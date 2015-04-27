using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Disa.Framework.Bubbles;

namespace Disa.Framework
{
    public static class BubbleGroupManager
    {
        public static List<Tuple<BubbleGroup, bool>> UpdateLastOnlineQueue { get; private set; }

        private static readonly object BubbleGroupsLock = new object();

        private static List<BubbleGroup> BubbleGroups
        {
            get;
            set;
        }

        internal static List<BubbleGroup> BubbleGroupsNonUnified
        {
            get
            {
                lock (BubbleGroupsLock)
                {
                    return BubbleGroups.Where(x => !(x is UnifiedBubbleGroup)).ToList();
                }
            }
        }

        internal static List<BubbleGroup> BubbleGroupsImmutable
        {
            get
            {
                lock (BubbleGroupsLock)
                {
                    return BubbleGroups.ToList();
                }
            }
        }

        public static List<BubbleGroup> DisplayImmutable
        {
            get
            {
                lock (BubbleGroupDatabase.OperationLock)
                {
                    return BubbleGroups.Where(x => !x.IsUnified).ToList();
                }
            }
        }

        public static List<BubbleGroup> DisplayNonUnifiedImmutable
        {
            get
            {
                return DisplayImmutable.Where(x => !(x is UnifiedBubbleGroup)).ToList();
            }
        }

        public static List<UnifiedBubbleGroup> UnifiedBubbleGroups
        {
            get
            {
                if (BubbleGroupFactory.UnifiedBubbleGroupsDatabase == null)
                    return new List<UnifiedBubbleGroup>();

                return BubbleGroupFactory.UnifiedBubbleGroupsDatabase.Select(x => x.Object).ToList();
            }
        }

        public static int BubbleGroupsCount
        {
            get { return BubbleGroups.Count; }
        }

        public static void BubbleGroupsAdd(BubbleGroup group)
        {
            lock (BubbleGroupsLock)
            {
                BubbleGroups.Add(group);
            }
        }

        public static void BubbleGroupsRemove(BubbleGroup group)
        {
            lock (BubbleGroupsLock)
            {
                BubbleGroups.Remove(group);
            }
        }

        static BubbleGroupManager()
        {
            BubbleGroups = new List<BubbleGroup>();
            UpdateLastOnlineQueue = new List<Tuple<BubbleGroup, bool>>();
        }

        public static bool Contains(BubbleGroup parent, BubbleGroup child)
        {
            if (parent == child)
                return true;
            if (child == null)
                return true;
            var unifiedGroup = parent as UnifiedBubbleGroup;
            return unifiedGroup != null && unifiedGroup.Groups.Any(innerGroup => innerGroup == child);
        }

        public static BubbleGroup FindInnerFast(VisualBubble visualBubble, BubbleGroup group)
        {
            if (visualBubble.BubbleGroupReference != null)
            {
                return visualBubble.BubbleGroupReference;
            }

            var unifiedGroup = @group as UnifiedBubbleGroup;

            if (unifiedGroup == null)
            {
                return @group;
            }
                
            BubbleGroup foundGroup = null;

            Parallel.ForEach(unifiedGroup.Groups, innerGroup =>
            {
                for (int i = innerGroup.Bubbles.Count - 1; i >= 0; i--)
                {
                    if (foundGroup != null)
                        return;

                    var bubble = innerGroup.Bubbles[i];

                    if (bubble.ID == visualBubble.ID)
                    {
                        foundGroup = innerGroup;
                        return;
                    }
                }
            });

            return foundGroup;
        }

        public static BubbleGroup FindInner(VisualBubble visualBubble, BubbleGroup group)
        {
            if (visualBubble.BubbleGroupReference != null)
            {
                return visualBubble.BubbleGroupReference;
            }

            var unifiedGroup = @group as UnifiedBubbleGroup;

            if (unifiedGroup == null)
            {
                return @group;
            }

            // try to find it.
            foreach (var innerGroup in unifiedGroup.Groups)
            {
                foreach (var bubble in innerGroup)
                {
                    if (bubble.ID != visualBubble.ID) continue;

                    return innerGroup;
                }
            }

            return null;
        }

        internal static BubbleGroup Find(Func<BubbleGroup, bool> predicate)
        {
            lock (BubbleGroupsLock)
            {
                return BubbleGroups.FirstOrDefault(predicate);
            }
        }

        public static BubbleGroup Find(Service service, string bubbleGroupId)
        {
            lock (BubbleGroupsLock)
            {
                return BubbleGroups.FirstOrDefault(x => x.Service == service && bubbleGroupId == x.ID);
            }
        }

        public static BubbleGroup FindWithAddress(Service service, string bubbleGroupAddress)
        {
            lock (BubbleGroupsLock)
            {
                return BubbleGroups.FirstOrDefault(x => x.Service == service && x.Service.BubbleGroupComparer(bubbleGroupAddress, x.Address));
            }
        }

        public static BubbleGroup Find(string bubbleGroupId)
        {
            lock (BubbleGroupsLock)
            {
                return BubbleGroups.FirstOrDefault(x => x.ID == bubbleGroupId);
            }
        }

        public static BubbleGroup Find(VisualBubble bubble)
        {
            lock (BubbleGroupsLock)
            {
                var unifiedGroups = BubbleGroups.OfType<UnifiedBubbleGroup>();
                foreach (var unifiedGroup in unifiedGroups)
                {
                    if (unifiedGroup.Any(unifiedGroupBubble => unifiedGroupBubble == bubble))
                    {
                        return unifiedGroup;
                    }
                }

                return
                BubbleGroups.FirstOrDefault(
                    group => @group.FirstOrDefault(secondBubble => secondBubble == bubble) != null);
            }
        }

        public static List<string> FindAllAddresses(Service service)
        {
            lock (BubbleGroupsLock)
            {
                return BubbleGroups.Where(group =>
                {
                    if (@group.Service != service)
                        return false;
                    return true;
                }).Select(x => x.Address).ToList();
            }
        }

        internal static List<BubbleGroup> FindAll(Func<BubbleGroup, bool> predicate)
        {
            lock (BubbleGroupsLock)
            {
                return BubbleGroups.Where(predicate).ToList();
            }
        }

        public static List<BubbleGroup> FindAll(Service service)
        {
            lock (BubbleGroupsLock)
            {
                return BubbleGroups.Where(group =>
                {
                    if (@group.Service != service)
                        return false;
                    return true;
                }).ToList();
            }
        }

        public static IEnumerable<BubbleGroup> FindAllInnerWithRegisteredService(BubbleGroup group)
        {
            var unifiedGroup = @group as UnifiedBubbleGroup;
            if (unifiedGroup == null)
            {
                return ServiceManager.IsRegistered(@group.Service) ? 
                    new [] { @group } : Enumerable.Empty<BubbleGroup>();
            }
            else
            {
                return unifiedGroup.Groups.Where(x => ServiceManager.IsRegistered(x.Service));
            }
        }

        public static BubbleGroup FindFirstInnerWithRegisteredService(UnifiedBubbleGroup unifiedGroup)
        {
            return unifiedGroup.Groups.FirstOrDefault(x => 
                ServiceManager.IsRegistered(x.Service));
        }

        public static BubbleGroup FindUnified(string primaryGroupId)
        {
            lock (BubbleGroupsLock)
            {
                return BubbleGroups.OfType<UnifiedBubbleGroup>().FirstOrDefault(x => x.PrimaryGroup.ID == primaryGroupId);
            }
        }

        public static List<BubbleGroup> GetInner(BubbleGroup group)
        {
            var unified = @group as UnifiedBubbleGroup;
            var innerGroups = new List<BubbleGroup>();
            if (unified != null)
            {
                innerGroups.AddRange(unified.Groups);
            }
            else
            {
                innerGroups.Add(@group);
            }
            return innerGroups;
        }

        public static IEnumerable<BubbleGroup> GetInnerMany(List<BubbleGroup> groups)
        {
            foreach (var group in groups)
            {
                foreach (var innerGroup in GetInner(@group))
                {
                    yield return innerGroup;
                }
            }
        }

        public static IEnumerable<BubbleGroup> SortByMostPopular(Service service, 
            bool avoidParties = true)
        {
            foreach (var mostPopularBubbleGroupLocation in 
                Directory.EnumerateFiles(BubbleGroupDatabase.GetServiceLocation(service.Information))
                    .OrderByDescending(x => new FileInfo(x).Length))
            {
                var groupHeader = Path.GetFileNameWithoutExtension(mostPopularBubbleGroupLocation);
                var groupDelimeter = groupHeader.IndexOf("^", StringComparison.Ordinal);
                var groupId = groupHeader.Substring(groupDelimeter + 1);
                lock (BubbleGroupsLock)
                {
                    foreach (var group in BubbleGroups.Where(x => x.Service == service))
                    {
                        if (@group.ID == groupId)
                        {
                            if (avoidParties && @group.IsParty)
                            {
                                break;
                            }

                            yield return @group;
                        }
                    }
                }
            }
        }

        public static BubbleGroup GetPrimary(BubbleGroup group)
        {
            var unifiedGroup = @group as UnifiedBubbleGroup;
            if (unifiedGroup == null)
                return @group;

            return unifiedGroup.PrimaryGroup;
        }

        public static BubbleGroup GetSending(BubbleGroup group)
        {
            var unifiedGroup = @group as UnifiedBubbleGroup;
            return unifiedGroup == null ? @group : unifiedGroup.SendingGroup;
        }

        public class TypingContainer
        {
            public bool Typing { get; private set; }
            public bool IsAudio { get; private set; }
            public bool Available { get; private set; }
            public DisaThumbnail Photo { get; private set; }

            public TypingContainer(bool typing, bool available, bool isAudio, DisaThumbnail photo)
            {
                Typing = typing;
                Available = available;
                IsAudio = isAudio;
                Photo = photo;
            }
        }

        public static TypingContainer GetTypingContainer(BubbleGroup group)
        {
            TypingContainer container;

            var unifiedGroup = @group as UnifiedBubbleGroup;
            if (unifiedGroup != null)
            {
                var typingGroup = unifiedGroup.Groups.FirstOrDefault(innerGroup => innerGroup.Typing);
                container = new TypingContainer (typingGroup != null, typingGroup != null && typingGroup.Presence,
                    typingGroup != null && typingGroup.TypingIsAudio, unifiedGroup.PrimaryGroup.Photo);
            }
            else
            {
                container = new TypingContainer(@group.Typing, @group.Presence, @group.TypingIsAudio, @group.Photo);
            }

            return container;
        }

        public static BubbleGroup GetUnifiedIfPossible(BubbleGroup group)
        {
            if (@group == null)
                return @group;

            var unifiedGroup = @group as UnifiedBubbleGroup;
            if (unifiedGroup != null)
                return @group;

            if (@group.IsUnified)
            {
                return @group.Unified;
            }

            return @group;
        }

        public static bool IsUnified(BubbleGroup group)
        {
            return @group as UnifiedBubbleGroup != null;
        }

        public static Task ProcessUpdateLastOnlineQueue(Service service)
        {
            return Task.Factory.StartNew(() =>
            {
                lock (UpdateLastOnlineQueue)
                {
                    foreach (var updateLastOnlineTuple in 
                        UpdateLastOnlineQueue.Where(x => x.Item1.Service == service).ToList())
                    {
                        UpdateLastOnline(updateLastOnlineTuple.Item1, updateLastOnlineTuple.Item2, true);
                        UpdateLastOnlineQueue.Remove(updateLastOnlineTuple);
                    }
                }
            });
        }

        internal static void UpdateLastOnline(BubbleGroup bubbleGroup, bool updateUi = true, bool fromProcessUpdateLastOnlineQueue = false)
        {
            var service = bubbleGroup.Service;

            if (!fromProcessUpdateLastOnlineQueue)
            {
                if (!ServiceManager.IsRunning(service) && !bubbleGroup.IsParty)
                {
                    lock (UpdateLastOnlineQueue)
                        UpdateLastOnlineQueue.Add(new Tuple<BubbleGroup, bool>(bubbleGroup, updateUi));
                    return;
                }
            }

            if (!ServiceManager.IsRunning(service) || bubbleGroup.IsParty) return;

            // reject the last online update if the Presence is currently available
            if (bubbleGroup.Presence)
            {
                return;
            }

            try
            {
                service.GetBubbleGroupLastOnline(bubbleGroup, time =>
                {
                    // reject the last online update if the Presence is currently available
                    if (bubbleGroup.Presence)
                    {
                        return;
                    }

                    bubbleGroup.PresenceType = PresenceBubble.PresenceType.Unavailable;
                    bubbleGroup.LastSeen = time;

                    if (updateUi)
                        BubbleGroupEvents.RaiseInformationUpdated(bubbleGroup);

                    //we constantly need to subscribe to a bubble group. doing it
                    //in last online method is the most effective.
                    BubbleManager.SendSubscribe(bubbleGroup, true);
                    //Presence(service, true);
                });
            }
            catch (Exception ex)
            {
                Utils.DebugPrint("Error updating bubble group last online: " + service.Information.ServiceName + ": " +
                                         ex.Message);
            }
        }

        public static void UpdateLastOnlineIfOffline(BubbleGroup group, bool updateUi = true)
        {
            var unifiedGroup = @group as UnifiedBubbleGroup;
            if (unifiedGroup != null)
            {
                foreach (var innerGroup in unifiedGroup.Groups)
                {
                    UpdateLastOnlineIfOffline(innerGroup, updateUi);
                }

                return;
            }

            if (!@group.Presence)
            {
                UpdateLastOnline(@group);
            }
        }

        public static void RemoveUnread(BubbleGroup group, bool updateUi = true)
        {
            SetUnread(@group, false, updateUi);
        }

        private static void SetUnread(BubbleGroup group, bool unread, bool updateUi = true, bool skipSendReadBubble = false, bool onlySetIfServiceRunning = true)
        {
            var unifiedGroup = @group as UnifiedBubbleGroup;

            if (unifiedGroup != null)
            {
                foreach (var innerGroup in unifiedGroup.Groups)
                {
                    SetUnread(innerGroup, false, skipSendReadBubble);
                }
            }

            Action @do = () =>
            {
                var updatedSomething = BubbleGroupSettingsManager.GetUnread(@group);
                BubbleGroupSettingsManager.SetUnread(@group, unread);

                if (unifiedGroup == null)
                {
                    if (!skipSendReadBubble)
                    {
                        if (@group.Service.Information.DoesSupport(typeof(ReadBubble)) && ServiceManager.IsRunning(@group.Service))
                        {
                            BubbleManager.Send(new ReadBubble(Time.GetNowUnixTimestamp(), Bubble.BubbleDirection.Outgoing, 
                                @group.Service, @group.Address, null, Time.GetNowUnixTimestamp(), @group.IsParty, updatedSomething));
                        }
                    }
                }

                if (updateUi)
                    BubbleGroupEvents.RaiseRefreshed(@group);
            };

            if (onlySetIfServiceRunning && ServiceManager.IsRunning(@group.Service))
            {
                @do();
            }
            else if (!onlySetIfServiceRunning)
            {
                @do();
            }
        }

        public static void SetUnread(Service service, bool unread, string bubbleGroupAddress)
        {
            var bubbleGroup = FindWithAddress(service, bubbleGroupAddress);
            if (bubbleGroup == null)
                return;
            SetUnread(bubbleGroup, unread, true, true, false);

            var possibleUnifiedBubbleGroup = GetUnifiedIfPossible(bubbleGroup);
            var unifiedBubbleGroup = possibleUnifiedBubbleGroup as UnifiedBubbleGroup;
            if (unifiedBubbleGroup != null)
            {
                var unifiedUnread = unifiedBubbleGroup.Groups.FirstOrDefault(BubbleGroupSettingsManager.GetUnread) != null;
                SetUnread(unifiedBubbleGroup, unifiedUnread, true, true, false);
            }
        }

        public static void SetReadTimes(BubbleGroup group, DisaReadTime[] readTimes)
        {
            @group.ReadTimes = readTimes == null ? null : (!readTimes.Any() ? null : readTimes);
        }

        public static void UpdateLastUnreadSetTime(Service service, string address)
        {
            var bubbleGroup = FindWithAddress(service, address);
            if (bubbleGroup != null)
            {
                BubbleGroupSettingsManager.SetLastUnreadSetTime(bubbleGroup, Time.GetNowUnixTimestamp());
            }
        }
    }
}