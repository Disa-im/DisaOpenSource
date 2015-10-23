using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disa.Framework.Bubbles;

namespace Disa.Framework
{
    public class UnifiedBubbleGroup : BubbleGroup
    {
        public List<BubbleGroup> Groups { get; private set; }
        public BubbleGroup PrimaryGroup { get; private set; }
        internal BubbleGroup _sendingGroup;
        public BubbleGroup SendingGroup
        {
            get { return _sendingGroup; }
            set
            {
                _sendingGroup = value;
                BubbleGroupIndex.SetUnifiedSendingBubbleGroup(ID, value.ID);
            }
        }
        internal bool UnifiedGroupLoaded { get; set; }

        private readonly Service _unifiedService;

        public UnifiedBubbleGroup(List<BubbleGroup> groups, BubbleGroup primaryGroup,
            VisualBubble initialBubble, string id = null)
            : base(initialBubble, id, false)
        {
            _unifiedService = initialBubble.Service;
            Groups = groups;
            PrimaryGroup = primaryGroup;
            _sendingGroup = primaryGroup;
        }

        public void UnloadFullUnifiedLoad()
        {
            UnifiedGroupLoaded = false;
            var lastBubble = Bubbles.Last();
            Bubbles.Clear();
            Bubbles.Add(lastBubble);
        }

        protected internal void OnBubbleUpdated(VisualBubble bubble, BubbleGroup group)
        {
            if (Bubbles.Count > BubblesCapSize)
            {
                Bubbles.RemoveAt(0);
            }

            var addedToEnd = false;

            for (int i = Bubbles.Count - 1; i >= 0; i--)
            {
                var nBubble = Bubbles[i];
                if (nBubble.Time <= bubble.Time)
                {
                    // adding it to the end, we can do a simple contract
                    if (i == Bubbles.Count - 1)
                    {
                        addedToEnd = true;
                        Bubbles.Add(bubble);
                        if (bubble.Direction == Bubble.BubbleDirection.Incoming)
                        {
                            BubbleGroupSettingsManager.SetUnread(this, true);
                        }
                    }
                    // inserting, do a full contract
                    else
                    {
                        Bubbles.Insert(i + 1, bubble);
                    }
                    break;
                }

                // could not find a valid place to insert, then skip insertion.
                if (i == 0)
                {
                    return;
                }
            }

            if (SendingGroup != group && addedToEnd)
            {
                SendingGroup = group;
                BubbleGroupEvents.RaiseSendingServiceChange(this);
            }

            RaiseBubbleInserted(bubble);
        }

        public override Service Service
        {
            get { return _unifiedService; }
        }
    }
}