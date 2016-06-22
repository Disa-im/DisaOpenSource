using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Disa.Framework.Bubbles;
using SharpTelegram.Schema;

namespace Disa.Framework.Telegram
{
    public partial class Telegram : BubbleGroupSync.Agent
    {
        public Task<BubbleGroupSync.Result> Sync(BubbleGroup @group, string actionId)
        {
            return Task<BubbleGroupSync.Result>.Factory.StartNew(() =>
            {
                const int limit = 100;
                var actionIdLong = actionId == null ? 0 : long.Parse(actionId);

                if (actionIdLong == 0)
                {
                   var bubbles = LoadBubblesForBubbleGroup(group, Time.GetNowUnixTimestamp(), limit);
                    foreach (var x in bubbles)
                    {
                        Utils.DebugPrint("###### " + ObjectDumper.Dump(x));
                    }
                    if (bubbles.LastOrDefault() != null)
                    {
                        var lastActionId = bubbles.LastOrDefault().IdService;
                        return new BubbleGroupSync.Result(BubbleGroupSync.Result.Type.Purge,lastActionId,null,bubbles.ToArray());
                    }
                }
                return new BubbleGroupSync.Result();
            });
        }

        public Task<bool> DeleteBubble(BubbleGroup @group, VisualBubble bubble)
        {
            //just basic true, since we plan on not implementing this stuff
            return Task<bool>.Factory.StartNew(() => true);
        }

        public Task<bool> DeleteConversation(BubbleGroup @group)
        {
            return Task<bool>.Factory.StartNew(() => true);
        }

        public Task<List<VisualBubble>> LoadBubbles(BubbleGroup @group, long fromTime, int max = 100)
        {
            return Task<List<VisualBubble>>.Factory.StartNew(() =>
            {
                return LoadBubblesForBubbleGroup(group, fromTime, max);
            });
        }

        private List<VisualBubble> LoadBubblesForBubbleGroup(BubbleGroup @group, long fromTime, int max)
        {
            var response = GetMessageHistory(group.Address, group.IsParty, fromTime, max);
            var messages = response as MessagesMessages;
            var messagesSlice = response as MessagesMessagesSlice;
            if (messages != null)
            {
                _dialogs.AddChats(messages.Chats);
                _dialogs.AddUsers(messages.Users);
                DebugPrint("Messages are as follows " + ObjectDumper.Dump(messages.Messages));
                messages.Messages.Reverse();
                return ConvertMessageToBubbles(messages.Messages);

            }
            if (messagesSlice != null)
            {
                _dialogs.AddChats(messagesSlice.Chats);
                _dialogs.AddUsers(messagesSlice.Users);
                DebugPrint("Messages are as follows " + ObjectDumper.Dump(messagesSlice.Messages));
                messagesSlice.Messages.Reverse();
                return ConvertMessageToBubbles(messagesSlice.Messages);
            }
            return new List<VisualBubble>();
        }

        private IMessagesMessages GetMessageHistory(string address, bool isParty, long fromTime, int max)
        {
            using (var client = new FullClientDisposable(this))
            {
                var peer = GetInputPeer(address, isParty);
                var response =
                    TelegramUtils.RunSynchronously(
                        client.Client.Methods.MessagesGetHistoryAsync(new MessagesGetHistoryArgs
                        {
                            Peer = peer,
                            OffsetDate = (uint) fromTime,
                            Limit = (uint) max

                        }));
                return response;
            }
        }

        private List<VisualBubble> ConvertMessageToBubbles(List<IMessage> messages)
        {
            var bubbles = new List<VisualBubble>();
            foreach (var iMessage in messages)
            {
                var message = iMessage as Message;
                if(message==null)continue;
                var bubble = ProcessFullMessage(message, false);
                bubbles.Add(bubble);
            }
            return bubbles;
        }
    }
}
