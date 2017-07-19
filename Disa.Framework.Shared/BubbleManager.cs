using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Disa.Framework.Bubbles;
using Disa.Framework.Bots;

namespace Disa.Framework
{
    public static class BubbleManager
    {
        public static List<PresenceBubble> LastPresenceBubbles { get; private set; }

        static BubbleManager()
        {
            LastPresenceBubbles = new List<PresenceBubble>();
        }

        public static Task<bool> SendCompose(ComposeBubble composeBubble, ComposeBubbleGroup composeBubbleGroup)
        {
            return Send(composeBubble, composeBubbleGroup, false);
        }

        public static Task<bool> Send(Bubble b, bool resend = false)
        {
            return Send(b, null, resend);
        }

        private static Task<bool> Send(Bubble b, BubbleGroup group, bool resend)
        {
            return Task<bool>.Factory.StartNew(() =>
            {                   
                var vb = b as VisualBubble;
                if (vb != null)
                {
                    if (vb.Status == Bubble.BubbleStatus.Sent)
                    {
                        Utils.DebugPrint("Trying to send a bubble that is already sent! On " + vb.Service.Information.ServiceName);
                        return true;
                    }

                    Func<bool> restartServiceIfNeeded = () =>
                    {
                        if (!ServiceManager.IsRegistered(b.Service) ||
                            ServiceManager.IsRunning(b.Service) ||
                            ServiceManager.IsAborted(b.Service) )
                            return false;

                        Utils.DebugPrint(
                            "For fucks sakes. The scheduler isn't doing it's job properly, or " +
                            "you're sending a message to it at a weird time. Starting it up bra (" +
                            b.Service.Information.ServiceName + ").");
                        ServiceManager.AbortAndRestart(b.Service);
                        return true;
                    };

                    var visualBubbleServiceId = vb.Service as IVisualBubbleServiceId;
                    if (vb.IdService == null && vb.IdService2 == null && visualBubbleServiceId != null)
                    {
                        visualBubbleServiceId.AddVisualBubbleIdServices(vb);
                    }

                    try
                    {
                        @group = Group(vb, resend, true);
                    }
                    catch (Exception ex)
                    {
                        Utils.DebugPrint("Problem in Send:GroupBubble from service " + 
                                                 vb.Service.Information.ServiceName + ": " + ex.Message);
                        return false;
                    }

                    if (@group == null)
                    {
                        Utils.DebugPrint("Could not find a suitable group for bubble " + vb.ID 
                                                 + " on " + vb.Service.Information.ServiceName); 
                        return false;
                    }

                    var shouldQueue = vb.Service.QueuedBubblesParameters == null || 
                                      !vb.Service.QueuedBubblesParameters.BubblesNotToQueue.Contains(vb.GetType());
                    var skipMonitorExit = false;

                    try
                    {
                        if (shouldQueue && !resend && 
                            BubbleQueueManager.HasQueuedBubbles(vb.Service.Information.ServiceName, 
                                true, false))
                        {
                            skipMonitorExit = true;
                            BubbleQueueManager.JustQueue(group, vb);
                            restartServiceIfNeeded();
                            return false;
                        }

                        if (shouldQueue)
                        {
                            Monitor.Enter(vb.Service.SendBubbleLock);
                        }

                        using (var queued = new BubbleQueueManager.InsertBubble(group, vb, shouldQueue))
                        {
                            Action checkForQueued = () =>
                            {
                                if (!resend 
                                    && BubbleQueueManager.HasQueuedBubbles(vb.Service.Information.ServiceName, true, true))
                                {
                                    BubbleQueueManager.Send(new [] { vb.Service.Information.ServiceName });
                                }
                            };

                            try
                            {
                                FailBubbleIfPathDoesntExist(vb);
                                SendBubbleInternal(b);
                            }
                            catch (ServiceQueueBubbleException ex)
                            {
                                Utils.DebugPrint("Queuing visual bubble on service " + 
                                                         vb.Service.Information.ServiceName + ": " + ex.Message);

                                UpdateStatus(vb, Bubble.BubbleStatus.Waiting, @group);

                                if (!restartServiceIfNeeded())
                                {
                                    checkForQueued();
                                }

                                return false;
                            }
                            catch (Exception ex)
                            {
                                queued.CancelQueue();

                                Utils.DebugPrint("Visual bubble on " + 
                                                         vb.Service.Information.ServiceName + " failed to be sent: " +
                                                         ex);

                                UpdateStatus(vb, Bubble.BubbleStatus.Failed, @group);
                                BubbleGroupEvents.RaiseBubbleFailed(vb, @group);

                                if (!restartServiceIfNeeded())
                                {
                                    checkForQueued();
                                }
                                    
                                //FIXME: if the bubble fails to send, allow the queue manager to continue.
                                if (resend)
                                    return true;

                                return false;
                            }
                                
                            queued.CancelQueue();

                            lock (BubbleGroupManager.LastBubbleSentTimestamps)
                            {
                                BubbleGroupManager.LastBubbleSentTimestamps[group.ID] = Time.GetNowUnixTimestamp();
                            }

                            var status = Bubble.BubbleStatus.Sent;

                            if (vb.Status == Bubble.BubbleStatus.Delivered)
                            {
                                status = Bubble.BubbleStatus.Delivered;
                                Utils.DebugPrint(
                                    "************ Race condition. The server set the status to delivered before we could send it to sent. :')");
                            }

                            UpdateStatus(vb, status, @group);

                            checkForQueued();
                            return true;
                        }
                    }
                    finally
                    {
                        if (!skipMonitorExit && shouldQueue)
                        {
                            Monitor.Exit(vb.Service.SendBubbleLock);
                        }
                    }
                }
                else
                {
                    var composeBubble = b as ComposeBubble;

                    if (composeBubble != null)
                    {
                        var bubbleToSend = composeBubble.BubbleToSend;
                        var visualBubbleServiceId = bubbleToSend.Service as IVisualBubbleServiceId;
                        if (bubbleToSend.IdService == null && bubbleToSend.IdService2 == null && 
                            visualBubbleServiceId != null)
                        {
                            visualBubbleServiceId.AddVisualBubbleIdServices(bubbleToSend);
                        }
                        @group.InsertByTime(bubbleToSend);
                        try
                        {
                            BubbleGroupEvents.RaiseBubbleInserted(bubbleToSend, @group);
                        }
                        catch
                        {
                            // do nothing
                        }
                    }

                    try
                    {
                        SendBubbleInternal(b);
                    }
                    catch (ServiceBubbleGroupAddressException ex)
                    {
                        if (!String.IsNullOrWhiteSpace(ex.Address))
                        {
                            if (composeBubble != null)
                            {
                                composeBubble.BubbleToSend.Address = ex.Address;
                                composeBubble.BubbleToSend.Status = Bubble.BubbleStatus.Sent;

                                var actualGroup = Group(composeBubble.BubbleToSend, resend, true);

                                ServiceEvents.RaiseComposeFinished(
                                    @group as ComposeBubbleGroup, actualGroup);

                                return true;
                            }
                        }

                        composeBubble.BubbleToSend.Status = Bubble.BubbleStatus.Failed;
                        return false;
                    }
                    catch (Exception ex)
                    {
                        Utils.DebugPrint("Failed to send bubble on service " + b.Service.Information.ServiceName);

                        if (composeBubble != null)
                        {
                            composeBubble.BubbleToSend.Status = Bubble.BubbleStatus.Failed;
                        }

                        return false;
                    }

                    if (composeBubble != null)
                    {
                        composeBubble.BubbleToSend.Status = Bubble.BubbleStatus.Failed;
                        return false;
                    }

                    return true;
                }
            });
        }

        private static void SendBubbleInternal(Bubble b)
        {
            if (b.Service == null)
            {
                throw new ServiceBubbleSendFailedException("This function cannot be called with a null service.");
            }

            if (!b.Service.Information.DoesSupport(b.GetType()))
            {
                throw new ServiceBubbleSendFailedException("The service " + b.Service.Information.ServiceName +
                                                           " does not support " + b.GetType());
            }

            Utils.DebugPrint("Sending " + b.GetType().Name + " on service " + b.Service.Information.ServiceName);
            b.Service.SendBubble(b);
        }

        public static void SendSubscribe(Service service, bool subscribe)
        {
            if (!service.Information.DoesSupport(typeof(SubscribeBubble)))
                return;

            Utils.DebugPrint((subscribe
                ? "Subscribing"
                : "Unsubscribing") + " to " + service.Information.ServiceName
                                     + " solo bubble groups.");

            foreach (var bubbleGroup in BubbleGroupManager.FindAll(g => !g.IsParty && g.Service == service))
            {
                SendSubscribe(bubbleGroup, subscribe);
            }
        }

        public static void SendSubscribe(BubbleGroup bubbleGroup, bool subscribe)
        {
            if (!bubbleGroup.Service.Information.DoesSupport(typeof(SubscribeBubble)))
                return;

            var address = bubbleGroup.Address;

            var subcribeBubble = new SubscribeBubble(Time.GetNowUnixTimestamp(),
                Bubble.BubbleDirection.Outgoing, address,
                false, bubbleGroup.Service, subscribe);

            Send(subcribeBubble);
        }

        public static void SendPresence(Service service, bool available, bool justAddIfNoLastPresence = false)
        {
            if (!service.Information.DoesSupport(typeof(PresenceBubble)))
                return;

            var presenceBubble = new PresenceBubble(Time.GetNowUnixTimestamp(),
                Bubble.BubbleDirection.Outgoing, null,
                false, service, available);

            Utils.DebugPrint("Sending " + (presenceBubble.Available
                ? "available"
                : "unavailble") + " to " +
                                     presenceBubble.Service.Information.ServiceName);
            lock (LastPresenceBubbles)
            {
                Action a = () =>
                {
                    LastPresenceBubbles.RemoveAll(pb => pb.Service == presenceBubble.Service);
                    LastPresenceBubbles.Add(presenceBubble);
                };
                if (!justAddIfNoLastPresence)
                {
                    a();
                }
                else
                {
                    var hasPresence = LastPresenceBubbles.Any(x => x.Service == presenceBubble.Service);
                    if (hasPresence)
                    {
                        // do-nothing
                    }
                    else
                    {
                        a();
                    }
                }
            }

            if (!justAddIfNoLastPresence && ServiceManager.IsRunning(service))
                Send(presenceBubble);

            if (justAddIfNoLastPresence)
                return;
            
            if (available) 
                return;
            foreach (var group in BubbleGroupManager.FindAll(service))
            {
                @group.PresenceType = PresenceBubble.PresenceType.Unavailable;
            }
        }

        public static void SendLastPresence(Service service)
        {
            if (!service.Information.DoesSupport(typeof(PresenceBubble)))
                return;

            lock (LastPresenceBubbles)
            {
                var presenceBubble = LastPresenceBubbles.FirstOrDefault(pb => pb.Service == service);
                if (presenceBubble == null)
                {
                    return;
                }

                Utils.DebugPrint("Sending last presence for service " + service.Information.ServiceName + ". " +
                                         (presenceBubble.Available ? "Available." : "Unavailable."));
                Send(presenceBubble);

                if (presenceBubble.Available) 
                    return;
                foreach (var group in BubbleGroupManager.FindAll(presenceBubble.Service))
                {
                    @group.PresenceType = PresenceBubble.PresenceType.Unavailable;
                }
            }
        }

        private static void FailBubbleIfPathDoesntExist(VisualBubble bubble)
        {
            var path = GetMediaFilePathIfPossible(bubble);
            if (String.IsNullOrWhiteSpace(path))
                return;
            if (!File.Exists(path))
            {
                throw new ServiceBubbleSendFailedException("Visual bubble media file path doesn't exist.");
            }
        }

        public static bool Update(BubbleGroup group, VisualBubble[] bubbles, int bubbleDepth = 100)
        {
            return BubbleGroupDatabase.UpdateBubble(@group, bubbles, bubbleDepth);
        }

        public static bool Update(BubbleGroup group, VisualBubble visualBubble, int bubbleDepth = 100)
        {
            if (visualBubble.BubbleGroupReference != null)
            {
                BubbleGroupDatabase.UpdateBubble(visualBubble.BubbleGroupReference, visualBubble, bubbleDepth);
                return true;
            }

            var unifiedGroup = @group as UnifiedBubbleGroup;
            if (unifiedGroup == null)
            {
                BubbleGroupDatabase.UpdateBubble(@group, visualBubble, bubbleDepth);
                return true;
            }

            foreach (var innerGroup in unifiedGroup.Groups)
            {
                foreach (var bubble in innerGroup.Bubbles)
                {
                    if (bubble.ID != visualBubble.ID) continue;

                    BubbleGroupDatabase.UpdateBubble(innerGroup, visualBubble, bubbleDepth);
                    return true;
                }
            }

            return false;
        }

        public static void UpdateStatus(VisualBubble[] bubbles, Bubble.BubbleStatus status, BubbleGroup group, 
            bool updateBubbleGroupBubbles = true)
        {
            foreach (var bubble in bubbles)
            {
                bubble.Status = status;
            }
            BubbleGroupDatabase.UpdateBubble(@group, bubbles);
            if (updateBubbleGroupBubbles)
                BubbleGroupEvents.RaiseBubblesUpdated(@group);
            BubbleGroupEvents.RaiseRefreshed(@group);
        }

        public static void UpdateStatus(VisualBubble b, Bubble.BubbleStatus status, BubbleGroup group, 
            bool updateBubbleGroupBubbles = true)
        {
            b.Status = status;
            BubbleGroupDatabase.UpdateBubble(@group, b);
            if (updateBubbleGroupBubbles)
                BubbleGroupEvents.RaiseBubblesUpdated(@group);
            BubbleGroupEvents.RaiseRefreshed(@group);
        }

        internal static void Replace(BubbleGroup group, IEnumerable<VisualBubble> bubbles)
        {
            var unifiedGroup = @group as UnifiedBubbleGroup;
            if (unifiedGroup != null)
            {
                foreach (var innerGroup in unifiedGroup.Groups)
                {
                    Replace(innerGroup, bubbles);
                }
            }

            for (int i = 0; i < @group.Bubbles.Count; i++)
            {
                var bubble = @group.Bubbles[i];
                var bubbleReplacement = bubbles.LastOrDefault(x => x.ID == bubble.ID);
                if (bubbleReplacement != null)
                {
                    @group.Bubbles.RemoveAt(i);
                    @group.Bubbles.Insert(i, bubbleReplacement);
                }
            }
        }

        public static bool UpdateStatus(Service service, string bubbleGroupAddress,
            string bubbleId, Bubble.BubbleStatus status)
        {
            return UpdateStatus(service, bubbleGroupAddress, bubbleId, status, null);
        }

        public static bool UpdateStatus(Service service, string bubbleGroupAddress,
            string bubbleId, Bubble.BubbleStatus status, Action<VisualBubble> additional)
        {
            var group = BubbleGroupManager.FindWithAddress(service, bubbleGroupAddress);
            if (group == null)
            {
                return false;
            }
            BubbleGroupFactory.LoadFullyIfNeeded(group);
            foreach (var bubble in @group.Bubbles)
            {
                if (bubble.ID == bubbleId)
                {
                    if (additional != null)
                    {
                        additional(bubble);
                    }
                    UpdateStatus(bubble, status, @group);
                    return true;
                }
            }
            return false;
        }

        private static bool IsBubbleDownloading(VisualBubble bubble)
        {
            var imageBubble = bubble as ImageBubble;
            if (imageBubble != null)
            {
                return imageBubble.Transfer != null;
            }
            var videoBubble = bubble as VideoBubble;
            if (videoBubble != null)
            {
                return videoBubble.Transfer != null;
            }
            var audioBubble = bubble as AudioBubble;
            if (audioBubble != null)
            {
                return audioBubble.Transfer != null;
            }
            var fileBubble = bubble as FileBubble;
            if (fileBubble != null)
            {
                return fileBubble.Transfer != null;
            }

            return false;
        }

        private static bool IsBubbleSending(VisualBubble bubble)
        {
            if (bubble is NewBubble)
            {
                return false;
            }
            if (bubble.Status == Bubble.BubbleStatus.Waiting 
                && bubble.Direction == Bubble.BubbleDirection.Outgoing)
            {
                return true;
            }
            return false;
        }

        internal static IEnumerable<VisualBubble> FetchAllSendingAndDownloading(BubbleGroup group)
        {
            foreach (var bubble in @group.Bubbles)
            {
                if (IsBubbleSending(bubble))
                {
                    yield return bubble;
                }
                else if (IsBubbleDownloading(bubble))
                {
                    yield return bubble;
                }
            }
        }

        internal static IEnumerable<VisualBubble> FetchAllSending(BubbleGroup group)
        {
            foreach (var bubble in @group.Bubbles)
            {
                if (IsBubbleSending(bubble))
                {
                    yield return bubble;
                }
            }
        }

        public static string GetMediaFilePathIfPossible(VisualBubble bubble)
        {
            var imageBubble = bubble as ImageBubble;
            if (imageBubble != null)
            {
                return imageBubble.ImagePath;
            }

            var videoBubble = bubble as VideoBubble;
            if (videoBubble != null)
            {
                return videoBubble.VideoPath;
            }

            var audioBubble = bubble as AudioBubble;
            if (audioBubble != null)
            {
                return audioBubble.AudioPath;
            }

            var fileBubble = bubble as FileBubble;
            if (fileBubble != null)
            {
                return fileBubble.Path;
            }

            return null;
        }

        private static Regex _linkExtraction;
        private static void AddUrlMarkupIfNeeded(VisualBubble bubble)
        {
            try
            {
                var textBubble = bubble as TextBubble;
                if (textBubble != null && !textBubble.HasParsedMessageForUrls) 
                {
                    var markups = new List<BubbleMarkup>();
                    if (_linkExtraction == null) 
                    {
						_linkExtraction = new Regex(@"\b(?:https?://|www\.)\S+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
					}
                    var rawString = textBubble.Message;
                    foreach (Match m in _linkExtraction.Matches(rawString))
                    {
                        markups.Add(new BubbleMarkupUrl
                        {
                            Url = m.Value,
                        });
                    }
                    if (textBubble.BubbleMarkups == null)
                    {
                        textBubble.BubbleMarkups = new List<BubbleMarkup>();
                    }
                    textBubble.BubbleMarkups.AddRange(markups);
                    textBubble.HasParsedMessageForUrls = true;
                }
            }
            catch (Exception e)
            {
                Utils.DebugPrint("Failed to add UrlMarkup: " + e);
            }
        }

        internal static void AddUrlMarkupIfNeeded(VisualBubble[] bubbles)
        {
            if (bubbles == null)
                return;
            foreach (var bubble in bubbles)
            {
                AddUrlMarkupIfNeeded(bubble);
            }
        }

        internal static void AddUrlMarkupIfNeeded(List<VisualBubble> bubbles)
		{
			if (bubbles == null)
				return;
			foreach (var bubble in bubbles)
			{
				AddUrlMarkupIfNeeded(bubble);
			}
		}

        internal static BubbleGroup Group(VisualBubble vb, bool resend = false, bool insertAtBottom = false)
        {
            lock (BubbleGroupDatabase.OperationLock)
            {
                Utils.DebugPrint("Grouping an " + vb.Direction + " bubble on service " + vb.Service.Information.ServiceName);

                AddUrlMarkupIfNeeded(vb);

                var theGroup =
                    BubbleGroupManager.FindWithAddress(vb.Service, vb.Address);

                BubbleGroupFactory.LoadFullyIfNeeded(theGroup);

                var duplicate = false;
                var newGroup = false;
                if (theGroup == null)
                {
                    Utils.DebugPrint(vb.Service.Information.ServiceName + " unable to find suitable group. Creating a new one.");

                    theGroup = new BubbleGroup(vb, null, false);

                    newGroup = true;

                    Utils.DebugPrint("GUID of new group: " + theGroup.ID);

                    BubbleGroupSettingsManager.SetUnreadIndicatorGuid(theGroup, theGroup.LastBubbleSafe().ID, true);

                    vb.Service.NewBubbleGroupCreated(theGroup).ContinueWith(x =>
                    {
                        // force the UI to refetch the photo
                        theGroup.IsPhotoSetFromService = false;
                        SendSubscribe(theGroup, true);
                        BubbleGroupUpdater.Update(theGroup);
                    });

                    BubbleGroupManager.BubbleGroupsAdd(theGroup);
                }
                else
                {
                    if (resend)
                    {
                        if (vb.Status == Bubble.BubbleStatus.Failed)
                        {
                            UpdateStatus(vb, Bubble.BubbleStatus.Waiting, theGroup);
                        }
                        return theGroup;
                    }

                    var visualBubbleServiceId = vb.Service as IVisualBubbleServiceId;
                    if (visualBubbleServiceId != null && 
                        visualBubbleServiceId.DisctinctIncomingVisualBubbleIdServices())
                    {
                        if (vb.IdService != null)
                        {
                            duplicate = theGroup.Bubbles.FirstOrDefault(x => x.GetType() == vb.GetType() && x.IdService == vb.IdService) != null;
                        }
                        if (!duplicate && vb.IdService2 != null)
                        {
                            duplicate = theGroup.Bubbles.FirstOrDefault(x => x.GetType() == vb.GetType() && x.IdService2 == vb.IdService2) != null;
                        }
                    }

                    if (!duplicate)
                    {
                        Utils.DebugPrint(vb.Service.Information.ServiceName + " found a group. Adding.");

                        if (insertAtBottom)
                        {
                            var lastBubble = theGroup.LastBubbleSafe();
                            if (lastBubble.Time > vb.Time)
                            {
                                vb.Time = lastBubble.Time;
                            }
                        }

                        theGroup.InsertByTime(vb);
                    }
                    else
                    {
                        Utils.DebugPrint("Yuck. It's a duplicate bubble. No need to readd: " + vb.IdService + ", " + vb.IdService2);
                    }
                }

                try
                {
                    if (theGroup.IsParty && !string.IsNullOrWhiteSpace(vb.ParticipantAddressNickname))
                    {
                        var participantAddressNicknamesArray = theGroup.ParticipantNicknames;
                        if (participantAddressNicknamesArray == null)
                        {
                            participantAddressNicknamesArray = new DisaParticipantNickname[0];
                        }
                        var participantAddressNicknames = participantAddressNicknamesArray.ToList();
                        var changed = false;
                        var adding = true;
                        foreach (var participantAddressNickname in participantAddressNicknames)
                        {
                            if (theGroup.Service.BubbleGroupComparer(participantAddressNickname.Address, vb.ParticipantAddress))
                            {
                                if (participantAddressNickname.Nickname != vb.ParticipantAddressNickname)
                                {
                                    participantAddressNickname.Nickname = vb.ParticipantAddressNickname;
                                    changed = true;
                                }
                                adding = false;
                                break;
                            }
                        }
                        if (adding)
                        {
                            participantAddressNicknames.Add(new DisaParticipantNickname
                            {
                                Address = vb.ParticipantAddress,
                                Nickname = vb.ParticipantAddressNickname,
                            });
                        }
                        if (changed || adding)
                        {
                            theGroup.ParticipantNicknames = participantAddressNicknames.ToArray();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Utils.DebugPrint("Failed to insert/update participant nickname into cache: " + ex);   
                }

                if (!duplicate)
                {
                    Utils.DebugPrint("Inserting bubble into database group!");

                    try
                    {
                        if (newGroup)
                        {
                            BubbleGroupDatabase.AddBubble(theGroup, vb);
                        }
                        else
                        {
                            BubbleGroupDatabase.InsertBubbleByTime(theGroup, vb);
                        }
                    }
                    catch (Exception ex)
                    {
                        Utils.DebugPrint("Bubble failed to be inserting/added into the group " + theGroup.ID + ": " + ex);
                    }

                    try
                    {
                        BubbleGroupEvents.RaiseBubbleInserted(vb, theGroup);
                    }
                    catch (Exception ex)
                    {
                        Utils.DebugPrint(
                            "Error in notifying the interface that the bubble group has been updated (" +
                            vb.Service.Information.ServiceName + "): " + ex.Message);
                    }
                }

                return theGroup;
            }
        }

        public static List<VisualBubble> FindAll(Service service, string address)
        {
            var bubbleGroup = BubbleGroupManager.FindWithAddress(service, address);
            if (bubbleGroup == null)
            {
                return new List<VisualBubble>();
            }
            BubbleGroupFactory.LoadFullyIfNeeded(bubbleGroup);
            return bubbleGroup.Bubbles.ToList();
        }

        public static IEnumerable<VisualBubble> FindAllUnread(Service service, string address)
        {
            var bubbleGroup = BubbleGroupManager.FindWithAddress(service, address);
            if (bubbleGroup != null)
            {
                BubbleGroupFactory.LoadFullyIfNeeded(bubbleGroup);
                foreach (var bubble in bubbleGroup.Bubbles)
                {
                    if (bubble.Direction == Bubble.BubbleDirection.Incoming && 
                        bubble.Time >= BubbleGroupSettingsManager.GetLastUnreadSetTime(bubbleGroup))
                    {
                        yield return bubble;
                    }
                }
            }
        }
    }
}
