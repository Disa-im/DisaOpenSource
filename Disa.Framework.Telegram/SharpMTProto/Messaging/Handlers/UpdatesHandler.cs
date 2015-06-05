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
        private readonly TLRig _tlRig;

        public UpdatesHandler(TLRig tlRig)
        {
            _tlRig = tlRig;
        }

        public EventHandler<List<object>> OnUpdate;
        public EventHandler OnUpdateTooLong;

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

            if (updateShort != null)
            {
                updateList.Add(updateShort.Update);
            }
            else if (updateShortMessage != null)
            {
                updateList.Add(updateShortMessage);
            }
            else if (updateShortChatMessage != null)
            {
                updateList.Add(updateShortChatMessage);
            }
            else if (updatesCombined != null)
            {
                updateList.AddRange(updatesCombined.Updates);
            }
            else if (updates != null)
            {
                updateList.AddRange(updates.UpdatesProperty);
            }

            //TODO: GZIP

            //TODO: remove
            //Console.WriteLine(ObjectDumper.Dump(body));

            RaiseOnUpdate(updateList);

            return TaskConstants.Completed;

        }

    }
}

