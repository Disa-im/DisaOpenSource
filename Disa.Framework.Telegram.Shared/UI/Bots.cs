using Disa.Framework.Bots;
using Disa.Framework.Bubbles;
using ProtoBuf;
using SharpTelegram.Schema;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Disa.Framework.Telegram
{
    public partial class Telegram : IBots
    {
        public Task GetBotCallbackAnswer(BubbleGroup group, VisualBubble bubble, Bots.KeyboardButton button, Action<Bots.MessagesBotCallbackAnswer> answer)
        {
            return Task.Factory.StartNew(() =>
            {
                using (var client = new FullClientDisposable(this))
                {
                    // Note: Telegram also has KeyboardButtonGame which functions as a callback also
                    if (button is Bots.KeyboardButtonCallback)
                    {
                        var keyboardButtonCallback = button as Bots.KeyboardButtonCallback;

                        var args = new MessagesGetBotCallbackAnswerArgs
                        {
                            MsgId = uint.Parse(bubble.IdService),
                            Data = keyboardButtonCallback.Data,
                            Peer = GetInputPeer(group.Address, group.IsParty, group.IsExtendedParty)
                        };

                        SharpTelegram.Schema.MessagesBotCallbackAnswer telegramBotCallbackAnswer =
                            (SharpTelegram.Schema.MessagesBotCallbackAnswer)
                                TelegramUtils.RunSynchronously(
                                    client.Client.Methods.MessagesGetBotCallbackAnswerAsync(args));

                        var disaBotCallbackAnswer = new Bots.MessagesBotCallbackAnswer
                        {
                            Alert = telegramBotCallbackAnswer.Alert != null ? true : false,
                            Message = telegramBotCallbackAnswer.Message
                        };

                        answer(disaBotCallbackAnswer);
                    }
                }
            });
        }

        public Task SendBotInlineModeQuery(BubbleGroup group, Bots.UpdateBotInlineQuery query, Action<Bots.MessagesBotResults> results)
        {
            return Task.Factory.StartNew(() =>
            {
                using (var client = new FullClientDisposable(this))
                {

                    var telegramBotContact = query.Bot as TelegramBotContact;
                    var user = telegramBotContact.User;

                    var args = new MessagesGetInlineBotResultsArgs
                    {
                        Bot = TelegramUtils.CastUserToInputUser(user),
                        Peer = GetInputPeer(group.Address, group.IsParty, group.IsExtendedParty),
                        Query = query.Query,
                        Offset = query.Offset
                    };

                    SharpTelegram.Schema.MessagesBotResults telegramMessageBotResults =
                        (SharpTelegram.Schema.MessagesBotResults)
                            TelegramUtils.RunSynchronously(
                                client.Client.Methods.MessagesGetInlineBotResultsAsync(args));

                    var messageBotResults = new Bots.MessagesBotResults
                    {
                        Gallery = telegramMessageBotResults.Gallery != null ? true : false,
                        QueryId = telegramMessageBotResults.QueryId.ToString(),
                        NextOffset = telegramMessageBotResults.NextOffset,
                        Results = new List<Bots.BotInlineResultBase>()
                    };

                    if (telegramMessageBotResults.Results != null)
                    {
                        foreach (var telegramMessageBotResult in telegramMessageBotResults.Results)
                        {
                            Bots.BotInlineResultBase item = null;
                            if (telegramMessageBotResult is SharpTelegram.Schema.BotInlineResult)
                            {
                                var telegramItem = telegramMessageBotResult as SharpTelegram.Schema.BotInlineResult;
                                item = new Bots.BotInlineResult
                                {
                                    Id = telegramItem.Id,
                                    QueryId = telegramMessageBotResults.QueryId.ToString(),
                                    Type = telegramItem.Type,
                                    Title = telegramItem.Title,
                                    Description = telegramItem.Description,
                                    Url = telegramItem.Url,
                                    ThumbUrl = telegramItem.ThumbUrl,
                                    ContentUrl = telegramItem.ContentUrl,
                                    ContentType = telegramItem.ContentType,
                                    W = telegramItem.W,
                                    H = telegramItem.H,
                                    Duration = telegramItem.Duration,
                                    SendMessage = HandleInlineModeSendMessage(telegramItem.SendMessage, group)
                            };
                            }
                            else if (telegramMessageBotResult is SharpTelegram.Schema.BotInlineMediaResult)
                            {
                                var telegramItem = telegramMessageBotResult as SharpTelegram.Schema.BotInlineMediaResult;
                                item = new Bots.BotInlineMediaResult
                                {
                                    Id = telegramItem.Id,
                                    QueryId = telegramMessageBotResults.QueryId.ToString(),
                                    Type = telegramItem.Type,
                                    Title = telegramItem.Title,
                                    Description = telegramItem.Description,
                                    SendMessage = HandleInlineModeSendMessage(telegramItem.SendMessage, group)
                                };

                                var botInlineMediaResult = item as Bots.BotInlineMediaResult;
                                botInlineMediaResult.Photo = HandleInlineModePhoto(telegramItem.Photo);

                                botInlineMediaResult.Document = HandleFullDocument(telegramItem.Document);
                            }
                            else
                            {
                                continue;
                            }

                            messageBotResults.Results.Add(item);
                        }
                    }

                    results(messageBotResults);
                }
            });

        }

        public Task SendBotInlineModeSelection(Bots.BotInlineResultBase selection, Action<bool> success)
        {
            throw new NotSupportedException();
        }

        public Task GetFileLocationBytes(byte[] additionalData, Action<byte[]> bytes)
        {
            return Task.Factory.StartNew(() =>
            {
                if (additionalData != null)
                {
                    using (var memoryStream = new MemoryStream(additionalData))
                    {
                        var fileLocation = Serializer.Deserialize<SharpTelegram.Schema.FileLocation>(memoryStream);
                        bytes(FetchFileBytes(fileLocation));
                    }
                }
            });
        }

        // Helper method to parse the SendMessage field from SendBotInlineModeQuery
        private Bots.BotInlineMessage HandleInlineModeSendMessage(IBotInlineMessage sendMessage, BubbleGroup bubbleGroup)
        {
            Bots.BotInlineMessage botInlineMessageDisa = null;

            if (sendMessage is SharpTelegram.Schema.BotInlineMessageMediaAuto)
            {
                var sendMessageAuto = sendMessage as SharpTelegram.Schema.BotInlineMessageMediaAuto;

                botInlineMessageDisa = new Bots.BotInlineMessageMediaAuto
                {
                    Caption = sendMessageAuto.Caption,
                    KeyboardInlineMarkup = HandleReplyMarkup(sendMessageAuto.ReplyMarkup) as KeyboardInlineMarkup
                };
            }
            else if (sendMessage is SharpTelegram.Schema.BotInlineMessageMediaContact)
            {
                var sendMessageContact = sendMessage as SharpTelegram.Schema.BotInlineMessageMediaContact;

                botInlineMessageDisa = new Bots.BotInlineMessageMediaContact
                {
                    PhoneNumber = sendMessageContact.PhoneNumber,
                    FirstName = sendMessageContact.FirstName,
                    LastName = sendMessageContact.LastName,
                    KeyboardInlineMarkup = HandleReplyMarkup(sendMessageContact.ReplyMarkup) as KeyboardInlineMarkup
                };
            }
            else if (sendMessage is SharpTelegram.Schema.BotInlineMessageMediaGeo)
            {
                var sendMessageGeo = sendMessage as SharpTelegram.Schema.BotInlineMessageMediaGeo;

                botInlineMessageDisa = new Bots.BotInlineMessageMediaGeo()
                {
                    Geo = HandleGeoPoint(sendMessageGeo.Geo),
                    KeyboardInlineMarkup = HandleReplyMarkup(sendMessageGeo.ReplyMarkup) as KeyboardInlineMarkup
                };
            }
            else if (sendMessage is SharpTelegram.Schema.BotInlineMessageMediaVenue)
            {
                var sendMessageVenue = sendMessage as SharpTelegram.Schema.BotInlineMessageMediaVenue;

                botInlineMessageDisa = new Bots.BotInlineMessageMediaVenue
                {
                    Geo = HandleGeoPoint(sendMessageVenue.Geo),
                    Title = sendMessageVenue.Title,
                    Address = sendMessageVenue.Address,
                    Provider = sendMessageVenue.Provider,
                    VenueId = sendMessageVenue.VenueId,
                    KeyboardInlineMarkup = HandleReplyMarkup(sendMessageVenue.ReplyMarkup) as KeyboardInlineMarkup
                };
            }
            else if (sendMessage is SharpTelegram.Schema.BotInlineMessageText)
            {
                var sendMessageText = sendMessage as SharpTelegram.Schema.BotInlineMessageText;

                botInlineMessageDisa = new Bots.BotInlineMessageText
                {
                    NoWebpage = sendMessageText.NoWebpage == null ? false : true,
                    Message = sendMessageText.Message,
                    KeyboardInlineMarkup = HandleReplyMarkup(sendMessageText.ReplyMarkup) as KeyboardInlineMarkup,
                };

                if (sendMessageText.Entities != null)
                {
                    ((Bots.BotInlineMessageText)botInlineMessageDisa).BubbleMarkups = HandleEntities(
                        message: sendMessageText.Message,
                        entities: sendMessageText.Entities,
                        bubbleGroupAddress: bubbleGroup.Address,
                        extendedParty: bubbleGroup.IsExtendedParty,
                        optionalClient: null);
                }

            }

            return botInlineMessageDisa;
        }

        // Helper method to translate Telegram's IGeoPoint into Disa's
        //  GeoPointBase.
        private Bots.GeoPoint HandleGeoPoint(IGeoPoint geoPoint)
        {
            Bots.GeoPoint disaGeoPoint = null;
            if (geoPoint is SharpTelegram.Schema.GeoPointEmpty)
            {
                disaGeoPoint = new Bots.GeoPoint
                {
                    IsEmpty = true
                };
            }
            else if (geoPoint is SharpTelegram.Schema.GeoPoint)
            {
                var telegramGeoPoint = geoPoint as SharpTelegram.Schema.GeoPoint;
                disaGeoPoint = new Bots.GeoPoint
                {
                    Lat = telegramGeoPoint.Lat,
                    Long = telegramGeoPoint.Long
                };
            }

            return disaGeoPoint;
        }

        // Helper method to translate Telegram's IPhoto into 
        // Disa's Bots.Photo
        private Bots.Photo HandleInlineModePhoto(IPhoto photo)
        {
            var photoTelegram = photo as SharpTelegram.Schema.Photo;
            if (photoTelegram == null)
            {
                return null;
            }

            var photoDisa = new Bots.Photo
            {
                Id = photoTelegram.Id,
                AccessHash = photoTelegram.AccessHash,
                Date = photoTelegram.Date,
                Sizes = new List<Bots.PhotoSize>()
            };

            var fileLocation = GetPhotoFileLocation(photoTelegram);
            var fileSize = GetPhotoFileSize(photoTelegram);
            var fileInfo = new FileInformation
            {
                FileLocation = fileLocation,
                Size = fileSize,
                FileType = "image",
                Document = new SharpTelegram.Schema.Document()
            };
            using (var memoryStream = new MemoryStream())
            {
                Serializer.Serialize<FileInformation>(memoryStream, fileInfo);
                photoDisa.AdditionalData = memoryStream.ToArray();
            }

            foreach (var photoSizeTelegram in photoTelegram.Sizes)
            {
                var photoSizeDisa = HandlePhotoSize(photoSizeTelegram);
                if (photoSizeDisa != null)
                {
                    photoDisa.Sizes.Add(photoSizeDisa);
                }
            }

            return photoDisa;
        }

        private Bots.PhotoSize HandlePhotoSize(IPhotoSize photoSize)
        {
            var photoSizeEmptyTelegram = photoSize as SharpTelegram.Schema.PhotoSizeEmpty;
            var photoSizeTelegram = photoSize as SharpTelegram.Schema.PhotoSize;
            var photoCachedSizeTelegram = photoSize as SharpTelegram.Schema.PhotoCachedSize;

            if (photoSizeEmptyTelegram != null)
            {
                return null;
            }
            else if (photoSizeTelegram != null)
            {
                return new Bots.PhotoSize
                {
                    Type = photoSizeTelegram.Type,
                    Location = HandleFileLocation(photoSizeTelegram.Location),
                    W = photoSizeTelegram.W,
                    H = photoSizeTelegram.H,
                    Size = (int)photoSizeTelegram.Size
                };
            }
            else if (photoCachedSizeTelegram != null)
            {
                return new Bots.PhotoCachedSize
                {
                    Type = photoCachedSizeTelegram.Type,
                    Location = HandleFileLocation(photoCachedSizeTelegram.Location),
                    W = photoCachedSizeTelegram.W,
                    H = photoCachedSizeTelegram.H,
                    Bytes = photoCachedSizeTelegram.Bytes
                };
            }

            return null;
        }

        private Bots.FileLocation HandleFileLocation(IFileLocation fileLocation)
        {
            var fileLocationTelegram = fileLocation as SharpTelegram.Schema.FileLocation;
            if (fileLocationTelegram == null)
            {
                return null;
            }

            using (var memoryStream = new MemoryStream())
            {
                Serializer.Serialize<SharpTelegram.Schema.FileLocation>(memoryStream, fileLocationTelegram);

                return new Bots.FileLocation
                {
                    AdditionalData = memoryStream.ToArray()
                };
            }
        }

        // Helper function to fully convert a Telegram IDocument to a Disa Document
        private Bots.Document HandleFullDocument(IDocument document)
        {
            var documentTelegram = document as SharpTelegram.Schema.Document;

            var documentDisa = new Bots.Document
            {
                Id = documentTelegram.Id,
                AccessHash = documentTelegram.AccessHash,
                Date = documentTelegram.Date,
                MimeType = documentTelegram.MimeType,
                Size = documentTelegram.Size,
                Thumb = HandlePhotoSize(documentTelegram.Thumb),
                Attributes = new List<DocumentAttributeBase>()
            };

            var fileInfo = new FileInformation
            {
                FileType = "document",
                Document = document
            };
            using (var memoryStream = new MemoryStream())
            {
                Serializer.Serialize<FileInformation>(memoryStream, fileInfo);
                documentDisa.AdditionalData = memoryStream.ToArray();
            }

            foreach (var attribute in documentTelegram.Attributes)
            {
                var attributeImageSizeTelegram = attribute as SharpTelegram.Schema.DocumentAttributeImageSize;
                var attributeAnimatedTelegram = attribute as SharpTelegram.Schema.DocumentAttributeAnimated;
                var attributeStickerTelegram = attribute as SharpTelegram.Schema.DocumentAttributeSticker;
                var attributeVideoTelegram = attribute as SharpTelegram.Schema.DocumentAttributeVideo;
                var attributeAudioTelegram = attribute as SharpTelegram.Schema.DocumentAttributeAudio;
                var attributeFilenameTelegram = attribute as SharpTelegram.Schema.DocumentAttributeFilename;

                if (attributeImageSizeTelegram != null)
                {
                    documentDisa.Attributes.Add(new Bots.DocumentAttributeImageSize
                    {
                        W = attributeImageSizeTelegram.W,
                        H = attributeImageSizeTelegram.H
                    });

                }
                else if (attributeAnimatedTelegram != null)
                {
                    documentDisa.Attributes.Add(new Bots.DocumentAttributeAnimated());
                }
                else if (attributeStickerTelegram != null)
                {
                    documentDisa.Attributes.Add(new Bots.DocumentAttributeSticker
                    {
                        Alt = attributeStickerTelegram.Alt,
                        Stickerset = HandleInputStickerSet(attributeStickerTelegram.Stickerset)
                    });
                }
                else if (attributeVideoTelegram != null)
                {
                    documentDisa.Attributes.Add(new Bots.DocumentAttributeVideo
                    {
                        Duration = attributeVideoTelegram.Duration,
                        W = attributeVideoTelegram.W,
                        H = attributeVideoTelegram.H
                    });
                }
                else if (attributeAudioTelegram != null)
                {
                    documentDisa.Attributes.Add(new Bots.DocumentAttributeAudio
                    {
                        Voice = attributeAudioTelegram.Voice != null ? true : false,
                        Duration = attributeAudioTelegram.Duration,
                        Title = attributeAudioTelegram.Title,
                        Performer = attributeAudioTelegram.Performer,
                        Waveform = attributeAudioTelegram.Waveform
                    });
                }
                else if (attributeFilenameTelegram != null)
                {
                    documentDisa.Attributes.Add(new Bots.DocumentAttributeFilename
                    {
                        FileName = attributeFilenameTelegram.FileName
                    });
                }
            }

            return documentDisa;
        }

        // Helper function to convert a Telegram IInputStickerSet to a Disa InputStickerSet
        private Bots.InputStickerSetBase HandleInputStickerSet(IInputStickerSet inputStickerSet)
        {
            var inputStickerSetIDTelegram = inputStickerSet as SharpTelegram.Schema.InputStickerSetID;
            var inputStickerSetShortNameTelegram = inputStickerSet as SharpTelegram.Schema.InputStickerSetShortName;

            if (inputStickerSetIDTelegram != null)
            {
                return new Bots.InputStickerSetID
                {
                    Id = inputStickerSetIDTelegram.Id,
                    AccessHash = inputStickerSetIDTelegram.AccessHash
                };
            }
            else if (inputStickerSetShortNameTelegram != null)
            {
                return new Bots.InputStickerSetShortName
                {
                    ShortName = inputStickerSetShortNameTelegram.ShortName
                };
            }

            return null;
        }
    }
}
