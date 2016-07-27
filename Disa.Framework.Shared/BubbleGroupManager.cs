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

        internal static Dictionary<string, long> LastBubbleSentTimestamps { get; private set; }

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
                lock (BubbleGroupsLock)
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
                lock (BubbleGroupsLock)
                {
                    return BubbleGroups.OfType<UnifiedBubbleGroup>().ToList();
                }
            }
        }

        public static int BubbleGroupsCount
        {
            get { return BubbleGroups.Count; }
        }

        public static void BubbleGroupsAdd(BubbleGroup group, bool initialLoad = false)
        {
            lock (BubbleGroupsLock)
            {
                BubbleGroups.Add(group);
                if (!initialLoad && !(group is UnifiedBubbleGroup))
                {
                    BubbleGroupIndex.Add(group);
                }
            }
        }

        public static void BubbleGroupsRemove(BubbleGroup group)
        {
            lock (BubbleGroupsLock)
            {
                BubbleGroups.Remove(group);
                if (!(group is UnifiedBubbleGroup))
                {
                    BubbleGroupIndex.Remove(group.ID);
                }
            }
        }

        static BubbleGroupManager()
        {
            BubbleGroups = new List<BubbleGroup>();
            UpdateLastOnlineQueue = new List<Tuple<BubbleGroup, bool>>();
            LastBubbleSentTimestamps = new Dictionary<string, long>();
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

        public static string GetParticipantNicknameOrName(BubbleGroup group, DisaParticipant participant)
        {
            var fetchNickname = false;
            var name = participant.Name;
            if (!string.IsNullOrWhiteSpace(participant.Name))
            {
                if (!PhoneBook.IsPossibleNumber(participant.Name))
                {
                    //fall-through
                }
                else
                {
                    fetchNickname = true;
                }
            }
            else
            {
                fetchNickname = true;
            }
            var prependParticipantName = false;
            if (fetchNickname)
            {
                var participantNicknames = group.ParticipantNicknames;
                if (participantNicknames != null)
                {
                    var participantNickname = participantNicknames.FirstOrDefault(x => 
                        group.Service.BubbleGroupComparer(participant.Address, x.Address));
                    if (participantNickname != null)
                    {
                        name = participantNickname.Nickname;   
                        if (participant.Unknown)
                        {
                            prependParticipantName = true;
                        }
                    }
                }
            }
            if (prependParticipantName && !string.IsNullOrWhiteSpace(participant.Name))
            {
                return name + " (" + participant.Name + ")";
            }
            return name;
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
                foreach (var bubble in innerGroup.Bubbles)
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

        public static List<BubbleGroup> FindWithParticipantAddress(Service service, string participantAddress)
        {
            lock (BubbleGroupsLock)
            {
                return BubbleGroups.Where(x => x.Service == service && x.IsParty &&
                    x.Participants.FirstOrDefault(b => service.BubbleGroupComparer(participantAddress, b.Address)) != null).ToList();
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
                    if (unifiedGroup.Bubbles.Any(unifiedGroupBubble => unifiedGroupBubble == bubble))
                    {
                        return unifiedGroup;
                    }
                }

                return
                BubbleGroups.FirstOrDefault(
                        group => @group.Bubbles.FirstOrDefault(secondBubble => secondBubble == bubble) != null);
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

        public static void PurgeQuotedTitlesCache(Service service)
        {
            var bubbleGroups = FindAll(service);
            foreach (var bubbleGroup in bubbleGroups)
            {
                if (bubbleGroup.QuotedTitles != null)
                {
                    bubbleGroup.QuotedTitles = null;
                }
            }
        }

        public static void PurgeQuotedTitlesCache(Service service, string bubbleGroupAddress)
        {
            var bubbleGroup = FindWithAddress(service, bubbleGroupAddress);
            if (bubbleGroup != null)
            {
                if (bubbleGroup.QuotedTitles != null)
                {
                    bubbleGroup.QuotedTitles = null;
                }
            }
        }

        public static void PurgeQuotedTitleCache(Service service, string bubbleGroupAddress, string quotedTitleAddress)
        {
            var bubbleGroup = FindWithAddress(service, bubbleGroupAddress);
            if (bubbleGroup != null)
            {
                if (bubbleGroup.QuotedTitles != null)
                {
                    var quotedTitles = bubbleGroup.QuotedTitles.ToList();
                    var quotedTitle = quotedTitles.FirstOrDefault(x =>
                                           bubbleGroup.Service.BubbleGroupComparer(x.Address, quotedTitleAddress));
                    if (quotedTitle != null)
                    {
                        quotedTitles.Remove(quotedTitle);
                        bubbleGroup.QuotedTitles = quotedTitles.ToArray();
                    }
                }
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

        public static SendBubbleAction[] GetSendBubbleActions(BubbleGroup group)
        {
            BubbleGroup sendBubbleGroup;

            var unifiedGroup = @group as UnifiedBubbleGroup;
            if (unifiedGroup != null)
            {
                sendBubbleGroup = unifiedGroup.Groups.FirstOrDefault(innerGroup =>
                {
                    return innerGroup.SendBubbleActions.FirstOrDefault(x => x.Type
                        != SendBubbleAction.ActionType.Nothing) != null;
                });
            }
            else
            {
                sendBubbleGroup = group;
            }

            Func<SendBubbleAction[]> generateNullReturn = () =>
            {
                return new SendBubbleAction[]
                {
                    new SendBubbleAction
                    {
                        Address = group.Address,
                        Type = SendBubbleAction.ActionType.Nothing
                    }
                };  
            };

            if (sendBubbleGroup == null)
            {
                return generateNullReturn();
            }

            SendBubbleAction[] rturn = null;

            if (sendBubbleGroup.IsParty)
            {
                rturn = sendBubbleGroup.SendBubbleActions.Where(x => 
                    x.Type != SendBubbleAction.ActionType.Nothing).ToArray();
            }
            else
            {
                var value = sendBubbleGroup.SendBubbleActions.FirstOrDefault(x => 
                    x.Type != SendBubbleAction.ActionType.Nothing);
                if (value != null)
                {
                    rturn = new SendBubbleAction[1];
                    rturn[0] = value;
                }
            }

            if (rturn == null || !rturn.Any())
            {
                return generateNullReturn();
            }

            return rturn;
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
                    Utils.DebugPrint(">>>>>>>>>>>>>> Processing all last online times...");
                    foreach (var updateLastOnlineTuple in 
                        UpdateLastOnlineQueue.Where(x => x.Item1.Service == service).ToList())
                    {
                        Utils.DebugPrint(">>>>>>>>>>>>>> Processing last online time (from stash) for service: " + service.Information.ServiceName);
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
                    Utils.DebugPrint(">>>>>>>>>>>>>> Stashing UpdateLastOnline for later.");
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
                Utils.DebugPrint(">>>>>>>>>>>>>> Calling GetBubbleGroupLastOnline.");
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
            Utils.DebugPrint(">>>>>>>>>>>>>> UpdateLastOnlineIfOffline called.");

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
                    SetUnread(innerGroup, unread, false, skipSendReadBubble, onlySetIfServiceRunning);
                }
            }

            var currentlyUnread = BubbleGroupSettingsManager.GetUnread(@group);

            if (!unread)
            {
                BubbleGroupSettingsManager.SetUnreadIndicatorGuid(group, group.LastBubbleSafe().ID, false);
            }

            if ((onlySetIfServiceRunning && ServiceManager.IsRunning(@group.Service)) || !onlySetIfServiceRunning)
            {
                BubbleGroupSettingsManager.SetUnread(@group, unread);
                if (unifiedGroup == null)
                {
                    if (!skipSendReadBubble)
                    {
                        if (@group.Service.Information.DoesSupport(typeof(ReadBubble)) && ServiceManager.IsRunning(@group.Service))
                        {
                            var readBubble = new ReadBubble(Time.GetNowUnixTimestamp(), Bubble.BubbleDirection.Outgoing,
                                                 @group.Service, @group.Address, null, Time.GetNowUnixTimestamp(), @group.IsParty, currentlyUnread);
                            if (@group.IsExtendedParty) 
                            {
                                readBubble.ExtendedParty = true;
                            }
                            BubbleManager.Send(readBubble);
                        }
                    }
                }
                if (updateUi)
                    BubbleGroupEvents.RaiseRefreshed(@group);
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
