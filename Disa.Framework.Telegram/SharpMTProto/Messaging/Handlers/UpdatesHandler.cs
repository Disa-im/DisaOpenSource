using System.Threading.Tasks;
using Nito.AsyncEx;
using SharpMTProto.Schema;
using System;
using SharpTL;
using System.IO;
using System.IO.Compression;
using SharpTelegram.Schema.Layer18;
using System.Collections.Generic;
using Disa.Framework.Telegram;

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

            var updatesTooLong = body as UpdatesTooLong;
            var updateShortMessage = body as UpdateShortMessage;
            var updateShortChatMessage = body as UpdateShortChatMessage;
            var updateShort = body as UpdateShort;
            var updatesCombined = body as UpdatesCombined;
            var updates = body as Updates;

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
                state.Seq = updateShortMessage.Seq;
                updateList.Add(updateShortMessage);
            }
            else if (updateShortChatMessage != null)
            {
                state.Date = updateShortChatMessage.Date;
                state.Pts = updateShortChatMessage.Pts;
                state.Seq = updateShortChatMessage.Seq;
                updateList.Add(updateShortChatMessage);
            }
            else if (updatesCombined != null)
            {
                state.Date = updatesCombined.Date;
                state.Seq = updatesCombined.Seq;
                updateList.AddRange(updatesCombined.Updates);
            }
            else if (updates != null)
            {
                state.Date = updates.Date;
                state.Seq = updates.Seq;
                updateList.AddRange(updates.UpdatesProperty);
            }

            foreach (var update in updateList)
            {
                var newMessage = update as UpdateNewMessage;
                var readMessages = update as UpdateReadMessages;
                var deleteMessages = update as UpdateDeleteMessages;
                var restoreMessages = update as UpdateRestoreMessages;
                var encryptedMessages = update as UpdateNewEncryptedMessage;

                if (newMessage != null)
                {
                    state.Pts = newMessage.Pts;
                }
                else if (readMessages != null)
                {
                    state.Pts = readMessages.Pts;
                }
                else if (deleteMessages != null)
                {
                    state.Pts = deleteMessages.Pts;
                }
                else if (restoreMessages != null)
                {
                    state.Pts = restoreMessages.Pts;
                }
                else if (encryptedMessages != null)
                {
                    state.Qts = encryptedMessages.Qts;
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

