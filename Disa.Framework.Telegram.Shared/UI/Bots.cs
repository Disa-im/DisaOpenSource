using Disa.Framework.Bots;
using Disa.Framework.Bubbles;
using SharpTelegram.Schema;
using System;
using System.Collections.Generic;
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
                        QueryId = telegramMessageBotResults.QueryId,
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
                                    QueryId = telegramMessageBotResults.QueryId,
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
                                };

                                HandleInlineModeSendMessage(telegramItem.SendMessage, item);
                            }
                            else if (telegramMessageBotResult is SharpTelegram.Schema.BotInlineMediaResult)
                            {
                                var telegramItem = telegramMessageBotResult as SharpTelegram.Schema.BotInlineMediaResult;
                                item = new Bots.BotInlineMediaResult
                                {
                                    Id = telegramItem.Id,
                                    QueryId = telegramMessageBotResults.QueryId,
                                    Type = telegramItem.Type,
                                    Title = telegramItem.Title,
                                    Description = telegramItem.Description
                                };

                                HandleInlineModeSendMessage(telegramItem.SendMessage, item);
                                HandleInlineModePhoto(telegramItem.Photo);
                                HandleInlineModeDocument(telegramItem.Document);
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

        public Task SendBotInlineModeSelection(Bots.UpdateBotInlineSend selection, Action<bool> success)
        {
            throw new NotImplementedException();
        }

        // Helper method to parse the SendMessage field from SendBotInlineModeQuery
        private void HandleInlineModeSendMessage(IBotInlineMessage sendMessage, BotInlineResultBase item)
        {
            if (sendMessage is SharpTelegram.Schema.BotInlineMessageMediaAuto)
            {
                var sendMessageAuto = sendMessage as SharpTelegram.Schema.BotInlineMessageMediaAuto;

                item.SendMessage = new Bots.BotInlineMessageMediaAuto
                {
                    Caption = sendMessageAuto.Caption,
                };
                HandleReplyMarkup(sendMessageAuto.ReplyMarkup, item.SendMessage);
            }
            else if (sendMessage is SharpTelegram.Schema.BotInlineMessageMediaContact)
            {
                var sendMessageContact = sendMessage as SharpTelegram.Schema.BotInlineMessageMediaContact;

                item.SendMessage = new Bots.BotInlineMessageMediaContact
                {
                    PhoneNumber = sendMessageContact.PhoneNumber,
                    FirstName = sendMessageContact.FirstName,
                    LastName = sendMessageContact.LastName,
                };
                HandleReplyMarkup(sendMessageContact.ReplyMarkup, item.SendMessage);
            }
            else if (sendMessage is SharpTelegram.Schema.BotInlineMessageMediaGeo)
            {
                var sendMessageGeo = sendMessage as SharpTelegram.Schema.BotInlineMessageMediaGeo;

                item.SendMessage = new Bots.BotInlineMessageMediaGeo()
                {
                    Geo = HandleGeoPoint(sendMessageGeo.Geo)
                };
                HandleReplyMarkup(sendMessageGeo.ReplyMarkup, item.SendMessage);
            }
            else if (sendMessage is SharpTelegram.Schema.BotInlineMessageMediaVenue)
            {
                var sendMessageVenue = sendMessage as SharpTelegram.Schema.BotInlineMessageMediaVenue;

                item.SendMessage = new Bots.BotInlineMessageMediaVenue
                {
                    Geo = HandleGeoPoint(sendMessageVenue.Geo),
                    Title = sendMessageVenue.Title,
                    Address = sendMessageVenue.Address,
                    Provider = sendMessageVenue.Provider,
                    VenueId = sendMessageVenue.VenueId
                };
                HandleReplyMarkup(sendMessageVenue.ReplyMarkup, item.SendMessage);
            }
            else if (sendMessage is SharpTelegram.Schema.BotInlineMessageText)
            {
                var sendMessageText = sendMessage as SharpTelegram.Schema.BotInlineMessageText;

                item.SendMessage = new Bots.BotInlineMessageText
                {
                    NoWebpage = sendMessageText.NoWebpage == null ? false : true,
                    Message = sendMessageText.Message,
                };
                HandleReplyMarkup(sendMessageText.ReplyMarkup, item.SendMessage);
            }
        }

        // Helper method to translate Telegram's IGeoPoint into Disa's
        //  GeoPointBase.
        private GeoPointBase HandleGeoPoint(IGeoPoint geoPoint)
        {
            GeoPointBase disaGeoPoint = null;
            if (geoPoint is SharpTelegram.Schema.GeoPointEmpty)
            {
                disaGeoPoint = new Bots.GeoPointEmpty();
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
    }
}
