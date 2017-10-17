using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Disa.Framework.Bubbles;
using ProtoBuf;
using System.Globalization;

namespace Disa.Framework
{
    public class BubbleGroup : IEnumerable<VisualBubble>
    {
        public const int BubblesCapSize = 100;

        public string ID { get; private set; }
        public string LegibleId { get; internal set; }
        public bool PartiallyLoaded { get; internal set; }
        public bool Lazy { get; set; }
        
        internal ThreadSafeList<VisualBubble> Bubbles { get; private set; }

        public bool IsTitleSetFromService { get; internal set; }
        public string Title { get; internal set; }
            
        public bool IsPhotoSetFromService { get; set; }
        public bool IsPhotoSetInitiallyFromCache { get; internal set; }
        public DisaThumbnail Photo { get; set; }

        public bool IsParticipantsSetFromService { get; internal set; }
        public ThreadSafeList<DisaParticipant> Participants = new ThreadSafeList<DisaParticipant>();
        public ThreadSafeList<string> FailedUnknownParticipants = new ThreadSafeList<string>();

        public bool NeedsSync { get; internal set; }

        public readonly ThreadSafeList<SendBubbleAction> SendBubbleActions = new ThreadSafeList<SendBubbleAction>();
        public long LastSeen { get; internal set; }

        public ThreadSafeList<Mention> Mentions = new ThreadSafeList<Mention>();

        public PresenceBubble.PresenceType PresenceType { get; internal set; }
        public PresenceBubble.PlatformType PresencePlatformType { get; internal set; }
        public bool Presence  
        {
            get 
            {
                return PresenceBubble.IsAvailable(PresenceType);
            } 
        }

        private Action<VisualBubble, BubbleGroup> _bubbleInserted;
        private Action<BubbleGroup> _synced;
        public UnifiedBubbleGroup Unified { get; private set; }
        public bool IsUnified
        {
            get
            {
                return Unified != null;
            }
        }

        public DisaReadTime[] ReadTimes 
        {
            get
            {
                return BubbleGroupSettingsManager.GetReadTimes(this);
            }
            internal set
            {
                BubbleGroupSettingsManager.SetReadTimes(this, value);
            }
        }

        public DisaQuotedTitle[] QuotedTitles
        {
            get
            {
                return BubbleGroupSettingsManager.GetQuotedTitles(this);
            }
            set
            {
                BubbleGroupSettingsManager.SetQuotedTitles(this, value);
            }
        }

        internal DisaParticipantNickname[] ParticipantNicknames
        {
            get
            {
                return BubbleGroupSettingsManager.GetParticipantNicknames(this);
            }
            set
            {
                BubbleGroupSettingsManager.SetParticipantNicknames(this, value);
            }
        }

        internal BubbleGroupSettings Settings { get; set; }

        private long _bubblesInsertedCount;

		public bool InputDisabled { get; set; }

        public void RegisterSynced(Action<BubbleGroup> updated)
        {
            _synced = updated;
        }

        public void DeregisterSynced()
        {
            _synced = null;
        }

        public void RegisterUnified(UnifiedBubbleGroup unified)
        {
            Unified = unified;
        }

        public void DeregisterUnified()
        {
            Unified = null;
        }

        public void RegisterBubbleInserted(Action<VisualBubble, BubbleGroup> updated)
        {
            _bubbleInserted = updated;
        }

        public void DeregisterBubbleInserted()
        {
            _bubbleInserted = null;
        }

        public void RaiseBubblesSynced()
        {
            if (_synced != null)
            {
                _synced(this);
            }
        }

        protected void RaiseBubbleInserted(VisualBubble bubble)
        {
            if (_bubbleInserted != null)
            {
                _bubbleInserted(bubble, this);
            }
        }

        private void RaiseUnifiedBubblesUpdatedIfUnified(VisualBubble bubble)
        {
            if (Unified != null)
            {
                Unified.OnBubbleUpdated(bubble, this);
            }
        }

        public bool IsParty
        {
            get { return Bubbles[0].Party; }
        }

        public bool IsExtendedParty
        {
            get { return Bubbles[0].ExtendedParty; }
        }

        public void InsertByTime(VisualBubble b)
        {
            if (Bubbles.Count >= BubblesCapSize)
            {
                Bubbles.RemoveAt(0);
            }

            if (!(this is ComposeBubbleGroup))
            {
                var unreadIndicatorGuid = BubbleGroupSettingsManager.GetUnreadIndicatorGuid(this);
                for (int i = Bubbles.Count - 1; i >= 0; i--)
                {
                    var nBubble = Bubbles[i];

                    var unreadIndicatorIndex = -1;
                    if (unreadIndicatorGuid != null && unreadIndicatorGuid == nBubble.ID)
                    {
                        unreadIndicatorIndex = i;
                    }

                    // IMPORTANT: Our Time field is specified in seconds, however scenarios are appearing
                    //            (e.g., bots) where messages are sent in on the same second but still require
                    //            proper ordering. In this case, Services may set a flag specifying a fallback 
                    //            to the ID assigned by the Service (e.g. Telegram).
                    if ((nBubble.Time == b.Time) &&
                        (nBubble.IsServiceIdSequence && b.IsServiceIdSequence))
                    {
                        if (string.Compare(
                            strA: nBubble.IdService,
                            strB: b.IdService,
                            ignoreCase: false,
                            culture: CultureInfo.InvariantCulture) < 0)
                        {
                            //
                            // Incoming bubble must be placed AFTER current bubble we are evaluating
                            //

                            if (i == Bubbles.Count - 1)
                            {
                                Bubbles.Add(b);
                                if (b.Direction == Bubble.BubbleDirection.Incoming)
                                {
                                    BubbleGroupSettingsManager.SetUnread(this, true);
                                }
                            }
                            else
                            {
                                Bubbles.Insert(i + 1, b);
                                if (i >= unreadIndicatorIndex && b.Direction == Bubble.BubbleDirection.Outgoing)
                                {
                                    BubbleGroupSettingsManager.SetUnreadIndicatorGuid(this, b.ID, false);
                                }
                            }
                        }
                        else
                        {
                            //
                            // Incoming bubble must be placed BEFORE current bubble we are evaluating
                            //

                            Bubbles.Insert(i, b);
                            if (i >= unreadIndicatorIndex && b.Direction == Bubble.BubbleDirection.Outgoing)
                            {
                                BubbleGroupSettingsManager.SetUnreadIndicatorGuid(this, b.ID, false);
                            }
                        }
                        break;
                    }
                    // OK, simpler scenario, incoming bubble must be placed AFTER current bubble we are evaluating
                    else if (nBubble.Time <= b.Time)
                    {
                        // adding it to the end, we can do a simple contract
                        if (i == Bubbles.Count - 1)
                        {
                            Bubbles.Add(b);
                            if (b.Direction == Bubble.BubbleDirection.Incoming)
                            {
                                BubbleGroupSettingsManager.SetUnread(this, true);
                            }
                        }
                        // inserting, do a full contract
                        else
                        {
                            Bubbles.Insert(i + 1, b);
                        }
                        if (i >= unreadIndicatorIndex && b.Direction == Bubble.BubbleDirection.Outgoing)
                        {
                            BubbleGroupSettingsManager.SetUnreadIndicatorGuid(this, b.ID, false);
                        }
                        break;
                    }

                    // could not find a valid place to insert, then skip insertion.
                    if (i == 0)
                    {
                        return;
                    }
                }
            }

            if (Unified == null)
            {
                _bubblesInsertedCount++;
                if (_bubblesInsertedCount % 100 == 0)
                {
                    if (BubbleGroupSync.SupportsSyncAndIsRunning(this))
                    {
                        Action doSync = async () =>
                        {
                            using (Platform.AquireWakeLock("DisaSync"))
                            {
                                await Utils.Delay(1000);
                                await BubbleGroupSync.Sync(this, true);
                            }
                        };
                        doSync();
                    }
                }
            }

            RaiseBubbleInserted(b);
            RaiseUnifiedBubblesUpdatedIfUnified(b);
        }

        public void UnloadFullLoad()
        {
            PartiallyLoaded = true;
            var lastBubble = Bubbles.Last();
            Bubbles.Clear();
            Bubbles.Add(lastBubble);
        }

        public int CountSafe()
        {
            return Bubbles.Count;
        }

        public VisualBubble this[int key]
        {
            get
            {
                return Bubbles[key];
            }
        }

        public BubbleGroup(VisualBubble initialBubble, string id = null, bool partiallyLoaded = false)
        {
            PartiallyLoaded = partiallyLoaded;
            Bubbles = new ThreadSafeList<VisualBubble> { initialBubble };
            Setup(id);
        }

        public BubbleGroup(List<VisualBubble> initialBubbles, string id = null)
        {
            PartiallyLoaded = false;
            Bubbles = new ThreadSafeList<VisualBubble>(initialBubbles);
            Setup(id);
        }
        
        public BubbleGroup(List<VisualBubble> initialBubbles, bool lazy, string id = null)
        {
            Lazy = lazy;
            PartiallyLoaded = lazy;
            Bubbles = new ThreadSafeList<VisualBubble>(initialBubbles);
            Setup(id);
        }

        private void Setup(string id)
        {
            if (id == null)
            {
                ID = Guid.NewGuid().ToString();
            }
            else
            {
                ID = id;
            }

            var firstBubble = Bubbles.First();

            NeedsSync = firstBubble.Service is BubbleGroupSync.Agent;
        }

        public virtual Service Service
        {
            get { return Bubbles[0].Service; }
        }

        public string Address
        {
            get { return Bubbles[0].Address; }
        }

        public VisualBubble LastBubbleSafe()
        {
            return Bubbles[CountSafe() - 1];
        }

        public void RemoveBubble(VisualBubble bubble)
        {
            Bubbles.Remove(bubble);
        }

        public int CountExcludingNewBubbles()
        {
            return Bubbles.CountEx(x => !(x is NewBubble));
        }

        public IEnumerator<VisualBubble> GetEnumerator()
        {
            return Bubbles.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
