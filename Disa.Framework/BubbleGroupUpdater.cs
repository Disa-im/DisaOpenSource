using System;
using System.Collections.Generic;
using System.Linq;

namespace Disa.Framework
{
    public static class BubbleGroupUpdater
    {
        internal static void UpdateGroupLegibleID(BubbleGroup bubbleGroup, Action finished = null)
        {
            var service = bubbleGroup.Service;

            if (!ServiceManager.IsRunning(service)) return;

            try
            {
                service.GetBubbleGroupLegibleId(bubbleGroup, legibleID =>
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
                Utils.DebugPrint("Error updating bubble group legible ID: " + service.Information.ServiceName + ": " +
                                         ex.Message);
                if (finished != null)
                {
                    finished();
                }
            }
        }

        internal static void UpdateName(BubbleGroup bubbleGroup, Action finished = null)
        {
            var service = bubbleGroup.Service;

            if (!ServiceManager.IsRunning(service)) return;

            try
            {
                service.GetBubbleGroupName(bubbleGroup, title =>
                {
                    bubbleGroup.IsTitleSetFromService = true;
                    bubbleGroup.Title = title;
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
                Utils.DebugPrint("Error updating bubble group name: " + service.Information.ServiceName + ": " +
                                         ex.Message);
                if (finished != null)
                {
                    finished();
                }
            }
        }

        public static bool UpdatePhoto(BubbleGroup bubbleGroup, Action<DisaThumbnail> finished)
        {
            var service = bubbleGroup.Service;

            if (!ServiceManager.IsRunning(service))
                return false;

            if (bubbleGroup.Service.Information.UsesInternet && !Platform.HasInternetConnection())
                return false;

            try
            {
                service.GetBubbleGroupPhoto(bubbleGroup, photo =>
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
                Utils.DebugPrint("Error updating bubble group photo: " + 
                                         service.Information.ServiceName + ": " + ex.Message);
                if (finished != null)
                {
                    finished(null);
                }
            }

            return true;
        }

        public static bool UpdateParticipantPhoto(Service service, DisaParticipant participant, Action<DisaThumbnail> finished)
        {
            if (!ServiceManager.IsRunning(service)) return false;

            if (service.Information.UsesInternet && !Platform.HasInternetConnection())
                return false;

            var participantClosure = participant;
            try
            {
                service.GetBubbleGroupPartyParticipantPhoto(participant, result =>
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
                Utils.DebugPrint("Error updating bubble group participant photo: " +
                                         service.Information.ServiceName + ": " + ex.Message);
                if (finished != null)
                {
                    finished(null);
                }
            }

            return true;
        }

        internal static void UpdatePartyParticipants(BubbleGroup bubbleGroup, Action finished = null)
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
                service.GetBubbleGroupPartyParticipants(bubbleGroup, participants =>
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
                    foreach (var unknownParticipant in bubbleGroup.Participants.Where(x => x.Unknown))
                    {
                        if (newParticipants.FirstOrDefault(x => service.BubbleGroupComparer(x.Address, unknownParticipant.Address)) == null)
                        {
                            newParticipants.Add(unknownParticipant);
                        }
                    }
                    bubbleGroup.Participants = newParticipants.ToSynchronizedCollection();
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
                Utils.DebugPrint("Error updating bubble group participants: " + service.Information.ServiceName + ": " +
                                         ex.Message);
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
            }
            catch (Exception ex)
            {
                Utils.DebugPrint("Error getting unknown bubble group participant: " + service.Information.ServiceName + ": " +
                                         ex.Message);
                return false;
            }

            return true;
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
    }
}