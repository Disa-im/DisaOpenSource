using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disa.Framework.Bubbles;

namespace Disa.Framework
{
    public static class BubbleGroupUpdater
    {
        internal static async void UpdateGroupLegibleID(BubbleGroup bubbleGroup, Action finished = null)
        {
            var service = bubbleGroup.Service;

            if (!ServiceManager.IsRunning(service)) return;

            try
            {
                await service.GetBubbleGroupLegibleId(bubbleGroup, legibleID =>
                {
                    bubbleGroup.LegibleId = legibleID;
                    if (finished == null)
                    {
                        BubbleGroupEvents.RaiseRefreshed(bubbleGroup);
                    }
                    else
                    {
                        finished();
                    }
                });
            }
            catch (Exception ex)
            {
                Utils.DebugPrint("Error updating bubble group legible ID: " + service.Information.ServiceName + 
                    " - " + bubbleGroup.Address + ": " + ex);
                if (finished != null)
                {
                    finished();
                }
            }
        }

        internal static async void UpdateName(BubbleGroup bubbleGroup, Action finished = null)
        {
            var service = bubbleGroup.Service;

            if (!ServiceManager.IsRunning(service)) return;

            try
            {
                await service.GetBubbleGroupName(bubbleGroup, title =>
                {
                    bubbleGroup.IsTitleSetFromService = true;
                    if (string.IsNullOrWhiteSpace(title))
                    {
                        Utils.DebugPrint("Update name title is null (rejecting): " + service.Information.ServiceName + 
                            " - " + bubbleGroup.Address);
                    }
                    else
                    {
                        bubbleGroup.Title = title;
                    }
                    if (finished == null)
                    {
                        BubbleGroupEvents.RaiseRefreshed(bubbleGroup);
                        BubbleGroupEvents.RaiseInformationUpdated(bubbleGroup);
                    }
                    else
                    {
                        finished();
                    }
                });
            }
            catch (Exception ex)
            {
                Utils.DebugPrint("Error updating bubble group name: " + service.Information.ServiceName + 
                    " - " + bubbleGroup.Address + ": " + ex);
                if (finished != null)
                {
                    finished();
                }
            }
        }

        public static async void UpdatePhoto(BubbleGroup bubbleGroup, Action<DisaThumbnail> finished)
        {
            var service = bubbleGroup.Service;

            if (!ServiceManager.IsRunning(service))
                return;

            if (bubbleGroup.Service.Information.UsesInternet && !Platform.HasInternetConnection())
                return;

            try
            {
                await service.GetBubbleGroupPhoto(bubbleGroup, photo =>
                {
                    if (photo != null && photo.Failed)
                    {
                        if (finished != null)
                        {
                            finished(bubbleGroup.Photo);
                        }
                        return;
                    }

                    Action<DisaThumbnail> callbackFinished = thePhoto =>
                    {
                        bubbleGroup.Photo = thePhoto;
                        bubbleGroup.IsPhotoSetFromService = true;
                        if (finished != null)
                        {
                            finished(bubbleGroup.Photo);
                        }
                    };

                    if (photo == null && bubbleGroup.IsParty)
                    {
                        BubbleGroupUtils.StitchPartyPhoto(bubbleGroup, callbackFinished);
                    }
                    else
                    {
                        callbackFinished(photo);
                    }
                });
            }
            catch (Exception ex)
            {
                Utils.DebugPrint("Error updating bubble group photo: " + service.Information.ServiceName + 
                    " - " + bubbleGroup.Address + ": " + ex);
                if (finished != null)
                {
                    finished(null);
                }
            }
        }

        public static async void UpdateParticipantPhoto(Service service, DisaParticipant participant, Action<DisaThumbnail> finished)
        {
            if (!ServiceManager.IsRunning(service))
                return;

            if (service.Information.UsesInternet && !Platform.HasInternetConnection())
                return;

            var participantClosure = participant;
            try
            {
                await service.GetBubbleGroupPartyParticipantPhoto(participant, result =>
                {
                    if (result != null && result.Failed)
                    {
                        if (finished != null)
                        {
                            finished(participantClosure.Photo);
                        }
                        return;
                    }

                    participantClosure.IsPhotoSetFromService = true;
                    participantClosure.Photo = result;
                    finished(result);
                });
            }
            catch (Exception ex)
            {
                Utils.DebugPrint("Error updating bubble group participant photo: " + service.Information.ServiceName + 
                    " - " + participant.Address + ": " + ex);
                if (finished != null)
                {
                    finished(null);
                }
            }

            return;
        }

        internal static async void UpdatePartyParticipants(BubbleGroup bubbleGroup, Action finished = null)
        {
            var service = bubbleGroup.Service;

            if (!ServiceManager.IsRunning(service)) return;

            if (!bubbleGroup.IsParty)
            {
                if (finished != null)
                {
                    finished();
                }
                return;
            }

            try
            {
                await service.GetBubbleGroupPartyParticipants(bubbleGroup, participants =>
                {
                    // we need to propogate the old participant photos to the new participant list
                    var newParticipants = participants == null ? new List<DisaParticipant>() : participants.ToList();
                    foreach (var oldParticipant in bubbleGroup.Participants)
                    {
                        var newParticipant = newParticipants.FirstOrDefault(x => service.BubbleGroupComparer(x.Address, oldParticipant.Address));
                        if (newParticipant != null)
                        {
                            newParticipant.Photo = oldParticipant.Photo;
                            newParticipant.IsPhotoSetFromService = oldParticipant.IsPhotoSetFromService;
                        }
                    }
                    // move all unknown participants over
                    // if there are more than 100 unknown participants, we'll ignore and force unknown participants to be rebuilt
                    // to prevent an exceedingly large cache
                    if (bubbleGroup.Participants.Count(x => x.Unknown) < 100)
                    {
                        foreach (var unknownParticipant in bubbleGroup.Participants.Where(x => x.Unknown))
                        {
                            if (newParticipants.FirstOrDefault(x => service.BubbleGroupComparer(x.Address, unknownParticipant.Address)) == null)
                            {
                                newParticipants.Add(unknownParticipant);
                            }
                        }
                    }
                    bubbleGroup.Participants = new ThreadSafeList<DisaParticipant>(newParticipants);
                    bubbleGroup.IsParticipantsSetFromService = true;
                    if (finished == null)
                    {
                        BubbleGroupEvents.RaiseBubblesUpdated(bubbleGroup);
                    }
                    else
                    {
                        finished();
                    }
                });
            }
            catch (Exception ex)
            {
                Utils.DebugPrint("Error updating bubble group participants: " + service.Information.ServiceName + 
                    " - " + bubbleGroup.Address + ": " + ex);
                if (finished != null)
                {
                    finished();
                }
            }
        }

        public static void Update(Service service, bool optimized = true)
        {
            const int Interval = 5;
            var updateCounter = 0;
            var groupsDoneCounter = 0;
            var groups = BubbleGroupManager.FindAll(service);
            var updateCounterThreshold = groups.Count / Interval;

            foreach (var group in groups)
            {
                Action chainFinished = null;
                if (groups.Count >= Interval)
                {
                    chainFinished = () =>
                    {
                        Action<bool> doUpdate = singleton =>
                        {
                            BubbleGroupEvents.RaiseRefreshed(groups);
                            BubbleGroupEvents.RaiseBubblesUpdated(groups);
                            BubbleGroupEvents.RaiseInformationUpdated(groups);
                        };

                        groupsDoneCounter++;
                        if (groupsDoneCounter % Interval == 0)
                        {
                            updateCounter++;
                            doUpdate(false);
                        }
                            // do the remainder one by one ... 
                        else if (updateCounter >= updateCounterThreshold)
                        {
                            doUpdate(true);
                        }
                    };
                }

                Update(@group, optimized, chainFinished);
            }
        }

        internal static void Update(BubbleGroup group, bool optimized = true, Action optimizedChainFinished = null)
        {
            if (optimized)
            {
                UpdateName(@group, () =>
                {
                    UpdateGroupLegibleID(@group, () =>
                    {
                        UpdatePartyParticipants(@group, () =>
                        {
                            if (optimizedChainFinished != null)
                            {
                                optimizedChainFinished();
                            }
                            else
                            {
                                BubbleGroupEvents.RaiseRefreshed(@group);
                                BubbleGroupEvents.RaiseBubblesUpdated(@group);
                                BubbleGroupEvents.RaiseInformationUpdated(@group);
                            }
                        });
                    });
                });
            }
            else
            {
                UpdateName(@group);
                UpdateGroupLegibleID(@group);
                UpdatePartyParticipants(@group);
            }
        }

        public static bool UpdateUnknownPartyParticipant(BubbleGroup bubbleGroup, string participantAddress, Action onAdded = null)
        {
            var service = bubbleGroup.Service;

            if (!ServiceManager.IsRunning(service)) return false;

            if (string.IsNullOrWhiteSpace(participantAddress))
                return false;

            if (bubbleGroup.FailedUnknownParticipants.FirstOrDefault(x => 
                bubbleGroup.Service.BubbleGroupComparer(x, participantAddress)) != null)
                return false;

            try
            {
                service.GetBubbleGroupUnknownPartyParticipant(bubbleGroup, participantAddress, participant =>
                {
                    if (participant != null)
                    {
                        var contains = bubbleGroup.Participants.FirstOrDefault(x => 
                            bubbleGroup.Service.BubbleGroupComparer(x.Address, participantAddress)) != null;
                        if (!contains)
                        {
                            participant.Unknown = true;
                            bubbleGroup.Participants.Add(participant);
                        }
                    }
                    else
                    {
                        bubbleGroup.FailedUnknownParticipants.Add(participantAddress);
                    }
                    if (onAdded != null)
                    {
                        onAdded();
                    }
                    BubbleGroupEvents.RaiseRefreshed(bubbleGroup);
                    BubbleGroupEvents.RaiseBubblesUpdated(bubbleGroup);
                });
                return true;
            }
            catch (Exception ex)
            {
                Utils.DebugPrint("Error getting unknown bubble group participant: " + service.Information.ServiceName + 
                    " - " + bubbleGroup.Address + ": " + ex);
                return false;
            }
        }

        public static void Update(Service service, string bubbleGroupAddress)
        {
            Utils.DebugPrint("Service: " + service.Information.ServiceName + " has called UpdateBubbleGroup with id: " + bubbleGroupAddress);

            var selectedGroup =
                BubbleGroupManager.FindWithAddress(service, bubbleGroupAddress);

            if (selectedGroup == null) return;
            Utils.DebugPrint("Updating " + selectedGroup.ID + " with name " + (selectedGroup.Title != null
                ? selectedGroup.Title
                : "[unknown]"));
            Update(selectedGroup);
        }

        public static void UpdatePartyParticipants(Service service, 
            string bubbleGroupAddress, Action<bool> finished = null)
        {
            var bubbleGroup =
                BubbleGroupManager.FindWithAddress(service, bubbleGroupAddress);
            if (bubbleGroup == null)
            {
                finished(false);
            }
            UpdatePartyParticipants(bubbleGroup, () =>
            {
                try
                {
                    BubbleGroupEvents.RaiseBubblesUpdated(bubbleGroup);
                }
                catch
                {
                    // do nothing
                }
                if (finished != null)
                {
                    finished(true);
                }
            });
        }

        public static void UpdateParties(Service service, string participantAddress)
        {
            Utils.DebugPrint("Updating bubble group parties that contain participant address: " + participantAddress);

            foreach (var @group in 
                BubbleGroupManager.FindAll(@group => @group.IsParty && @group.Service == service && @group.Participants.Any()))
            {
                foreach (var participant in @group.Participants)
                {
                    //TODO: this needs to be made into a comparer eventually
                    //TODO: as it stands... a participant with address 16041234567 != 6041234567
                    if (participant.Address != participantAddress) continue;

                    Utils.DebugPrint("Updating " + @group.ID + " (party) with name " + (@group.Title != null
                        ? @group.Title
                        : "[unknown]"));

                    Update(@group);
                    break; //don't update the same group again
                }
            }
        }

        public static bool UpdateQuotedMessageTitle(VisualBubble bubble, Action<string> onTitleUpdated)
        {
            var service = bubble.Service;
            if (!ServiceManager.IsRunning(service))
            {
                return false;
            }
            if (string.IsNullOrWhiteSpace(bubble.QuotedAddress))
            {
                return false;
            }

            try
            {
                service.GetQuotedMessageTitle(bubble, result =>
                {
                    var bubbleGroup = BubbleGroupManager.FindWithAddress(bubble.Service, bubble.Address);
                    if (bubbleGroup != null)
                    {
                        var quotedTitles = bubbleGroup.QuotedTitles == null ? 
                                                    new List<DisaQuotedTitle>() : bubbleGroup.QuotedTitles.ToList();
                        var quotedTitle = quotedTitles.FirstOrDefault(x => 
                                                                   bubbleGroup.Service.BubbleGroupComparer(x.Address, bubble.QuotedAddress));
                        if (quotedTitle == null)
                        {
                            quotedTitles.Add(new DisaQuotedTitle
                            {
                                Address = bubble.QuotedAddress,
                                Title = result,
                            });
                        }
                        else
                        {
                            quotedTitle.Title = result;
                        }
                        bubbleGroup.QuotedTitles = quotedTitles.ToArray();
                    }
                    onTitleUpdated(result);
                });
                return true;
            }
            catch (Exception e)
            {
                Utils.DebugPrint("Error getting quoted message title " + e);
                return false;
            }
        }
    }
}
