using System;
using System.Linq;

namespace Disa.Framework
{
    public static class BubbleGroupUtils
    {
        public const string BubbleGroupPartyDelimeter = ",";

        public static string GeneratePartyTitle(string[] names)
        {
            var title = String.Empty;

            for (int i = 0; i < names.Length; i++)
            {
                title += names[i];
                if (i != names.Length - 1)
                {
                    title += BubbleGroupPartyDelimeter + " ";
                }
            }

            if (String.IsNullOrWhiteSpace(title))
            {
                return null;
            }

            return title;
        }

        public static string GeneratePartyTitle(Contact[] contacts)
        {
            var title = String.Empty;

            foreach (var contact in contacts)
            {
                if (contact != null)
                {
                    title += contact.FullName;
                    if (contact != contacts.Last())
                    {
                        title += BubbleGroupPartyDelimeter + " ";
                    }
                }
            }

            if (String.IsNullOrWhiteSpace(title))
            {
                return null;
            }

            return title;
        }

        public static void StitchPartyPhoto(BubbleGroup bubbleGroup, Action<DisaThumbnail> result)
        {
            Action<DisaParticipant[]> doIt = participants =>
            {
                if (participants == null || !participants.Any())
                {
                    result(null);
                    return;
                }

                DisaThumbnail firstImage = null;
                string firstName = null;
                var firstImageFetched = false;
                DisaThumbnail secondImage = null;
                string secondName = null;
                var secondImageFetched = false;
                DisaThumbnail thirdImage = null;
                string thirdName = null;
                var thirdImageFetched = false;

                Action pushIfAllFetched = () =>
                {
                    if (firstImageFetched && secondImageFetched && thirdImageFetched)
                    {
                        result(Platform.CreatePartyBitmap(bubbleGroup.Service, 
                            bubbleGroup.ID, firstImage, firstName, secondImage, secondName, thirdImage, thirdName));
                    }
                };

                var random = participants.ToList();
                if (random.Count > 0)
                {
                    firstName = random[0].Name;
                    try
                    {
                        bubbleGroup.Service.GetBubbleGroupPartyParticipantPhoto(random[0], result2 =>
                        {
                            if (result2 != null && result2.Failed)
                            {
                                result2 = null;
                            }

                            firstImageFetched = true;
                            firstImage = result2;
                            pushIfAllFetched();
                        });
                    }
                    catch
                    {
                        firstImageFetched = true;
                        pushIfAllFetched();
                    }
                }
                else
                {
                    firstImageFetched = true;
                    pushIfAllFetched();
                }
                if (random.Count > 1)
                {
                    secondName = random[1].Name;
                    try
                    {
                        bubbleGroup.Service.GetBubbleGroupPartyParticipantPhoto(random[1], result2 =>
                        {
                            if (result2 != null && result2.Failed)
                            {
                                result2 = null;
                            }

                            secondImageFetched = true;
                            secondImage = result2;
                            pushIfAllFetched();
                        });
                    }
                    catch
                    {
                        secondImageFetched = true;
                        pushIfAllFetched();
                    }
                }
                else
                {
                    secondImageFetched = true;
                    pushIfAllFetched();
                }
                if (random.Count > 2)
                {
                    thirdName = random[2].Name;
                    try
                    {
                        bubbleGroup.Service.GetBubbleGroupPartyParticipantPhoto(random[2], result2 =>
                        {
                            if (result2 != null && result2.Failed)
                            {
                                result2 = null;
                            }

                            thirdImageFetched = true;
                            thirdImage = result2;
                            pushIfAllFetched();
                        });
                    }
                    catch
                    {
                        thirdImageFetched = true;
                        pushIfAllFetched();
                    }
                }
                else
                {
                    thirdImageFetched = true;
                    pushIfAllFetched();
                }
            };

            try
            {
                if (bubbleGroup.IsParticipantsSetFromService)
                {
                    doIt(bubbleGroup.Participants.ToArray());
                }
                else
                {
                    bubbleGroup.Service.GetBubbleGroupPartyParticipants(bubbleGroup, participants =>
                    {
                        doIt(participants);
                    });
                }
            }
            catch
            {
                result(null);
            }
        }

        public static string GenerateComposeAddress(Contact.ID[] ids)
        {
            var address = String.Empty;
            foreach (var id in ids)
            {
                address += id.Id;
                if (id != ids.Last())
                {
                    address += ",";
                }
            }

            if (String.IsNullOrWhiteSpace(address))
            {
                return null;
            }
            return "compose:" + address;
        }
    }
}