using System.Threading.Tasks;
using Nito.AsyncEx;
using SharpMTProto.Schema;
using System;
using SharpTL;
using System.IO;
using System.IO.Compression;
using SharpTelegram.Schema;
using System.Collections.Generic;
using Disa.Framework.Telegram;
using Disa.Framework;

namespace SharpMTProto.Messaging.Handlers
{
    public class UpdatesHandler : ResponseHandler<IUpdates>
    {
        public class State
        {
            public uint Date { get; set; }
            public uint Pts { get; set; }
            public uint Qts { get; set; }
            public uint Seq { get; set; }
        }

        private readonly TLRig _tlRig;

        public UpdatesHandler(TLRig tlRig)
        {
            _tlRig = tlRig;
        }

        public EventHandler<List<object>> OnUpdate;

        public EventHandler OnUpdateTooLong;

        public EventHandler<State> OnUpdateState;

        private void RaiseOnUpdateTooLong()
        {
            if (OnUpdateTooLong != null)
            {
                OnUpdateTooLong(this, null);
            }
        }

        private void RaiseOnUpdate(List<object> updates)
        {
            if (OnUpdate != null)
            {
                OnUpdate(this, updates);
            }
        }

        private void RaiseOnUpdateState(State state)
        {
            if (OnUpdateState != null)
            {
                OnUpdateState(this, state);
            }
        }

        protected override Task HandleInternalAsync(SharpMTProto.Schema.IMessage responseMessage)
        {
            var body = responseMessage.Body;

            Console.WriteLine("handle Internal async " + ObjectDumper.Dump(responseMessage));

            var updatesTooLong = body as UpdatesTooLong;
            var updateShortMessage = body as UpdateShortMessage;
            var updateShortChatMessage = body as UpdateShortChatMessage;
            var updateShort = body as UpdateShort;
            var updatesCombined = body as UpdatesCombined;
            var updates = body as Updates;
            var updateShortSentMessage = body as UpdateShortSentMessage;
            var updateContactLink = body as UpdateContactLink;
            

            if (updatesTooLong != null)
            {
                RaiseOnUpdateTooLong();
            }

            var updateList = new List<object>();
            var state = new State();

            if (updateShort != null)
            {
                state.Date = updateShort.Date;
                updateList.Add(updateShort.Update);
            }
            else if (updateShortMessage != null)
            {
                state.Date = updateShortMessage.Date;
                state.Pts = updateShortMessage.Pts;
                updateList.Add(updateShortMessage);
            }
            else if (updateShortChatMessage != null)
            {
                state.Date = updateShortChatMessage.Date;
                state.Pts = updateShortChatMessage.Pts;
                updateList.Add(updateShortChatMessage);
            }
            else if (updatesCombined != null)
            {
                state.Date = updatesCombined.Date;
                state.Seq = updatesCombined.Seq;
                updateList.AddRange(updatesCombined.Updates);
                updateList.AddRange(updatesCombined.Users);
                updateList.AddRange(updatesCombined.Chats);
            }
            else if (updates != null)
            {
                state.Date = updates.Date;
                state.Seq = updates.Seq;
                updateList.AddRange(updates.UpdatesProperty);
                updateList.AddRange(updates.Users);
                updateList.AddRange(updates.Chats);
            }
            else if (updateShortSentMessage != null)
            {
                state.Date = updateShortSentMessage.Date;
                state.Pts = updateShortSentMessage.Pts;
                updateList.Add(updateShortSentMessage);
            }

            foreach (var update in updateList)
            {
                var newMessage = update as UpdateNewMessage;
                var deleteMessages = update as UpdateDeleteMessages;
                var encryptedMessages = update as UpdateNewEncryptedMessage;
                var readHistoryInbox = update as UpdateReadHistoryInbox;
                var readHistoryOutBox = update as UpdateReadHistoryOutbox;
                var updateWebPage = update as UpdateWebPage;
                var updateReadMessagesContents = update as UpdateReadMessagesContents;
                var updateEditMessage = update as UpdateEditMessage;
                var updateChannelTooLong = update as UpdateChannelTooLong;
                var updateNewChannelMessage = update as UpdateNewChannelMessage;
                var updateDeleteChannelMessage = update as UpdateDeleteChannelMessages;
                var updateEditChannelMessage = update as UpdateEditChannelMessage;


                if (newMessage != null)
                {
                    state.Pts = newMessage.Pts;
                }
                else if (deleteMessages != null)
                {
                    state.Pts = deleteMessages.Pts;
                }
                else if (encryptedMessages != null)
                {
                    state.Qts = encryptedMessages.Qts;
                }
                else if (readHistoryInbox != null)
                {
                    state.Pts = readHistoryInbox.Pts;
                }
                else if (readHistoryOutBox != null)
                {
                    state.Pts = readHistoryOutBox.Pts;
                }
                else if (updateWebPage != null)
                {
                    state.Pts = updateWebPage.Pts;
                }
                else if (updateReadMessagesContents != null)
                {
                    state.Pts = updateReadMessagesContents.Pts;
                }
                else if (updateEditMessage != null)
                {
                    state.Pts = updateEditMessage.Pts;
                }
                else if (updateChannelTooLong != null)
                {
                    state.Pts = updateChannelTooLong.Pts;
                }
                else if (updateNewChannelMessage != null)
                {
                    state.Pts = updateNewChannelMessage.Pts;
                }
                else if (updateDeleteChannelMessage != null)
                {
                    state.Pts = updateDeleteChannelMessage.Pts;
                }
                else if (updateEditChannelMessage != null)
                {
                    state.Pts = updateEditChannelMessage.Pts;
                }

            }

            RaiseOnUpdateState(state);

            //TODO: GZIP

            //TODO: remove
            //Console.WriteLine(ObjectDumper.Dump(body));

            RaiseOnUpdate(updateList);

            return TaskConstants.Completed;

        }

    }
}

