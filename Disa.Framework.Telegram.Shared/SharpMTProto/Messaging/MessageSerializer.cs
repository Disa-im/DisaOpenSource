// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MessageSerializer.cs">
//   Copyright (c) 2013-2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using SharpMTProto.Schema;
using SharpTL;
using SharpTL.Serializers;

namespace SharpMTProto.Messaging
{
    /// <summary>
    ///     TL serializer for a <see cref="Message" /> class.
    /// </summary>
    /// <remarks>
    ///     message msg_id:long seqno:int bytes:int body:Object = Message;
    /// </remarks>
    public class MessageSerializer : TLSerializer<Message>
    {
        public const int DefaultConstructorNumber = 0x5BB8E511;
        private static readonly Type MessageType = typeof (Message);

        public MessageSerializer()
            : base(DefaultConstructorNumber)
        {
        }

        public override Type SupportedType
        {
            get { return MessageType; }
        }

        protected override Message ReadTypedBody(TLSerializationContext context)
        {
            TLStreamer streamer = context.Streamer;

            ulong msgId = streamer.ReadUInt64();
            uint seqNo = streamer.ReadUInt32();
            int bodyLength = streamer.ReadInt32();

            if (streamer.BytesTillEnd < bodyLength)
            {
                throw new TLSerializationException(String.Format("Body length ({0}) is greated than available to read bytes till end ({1}).", bodyLength,
                    streamer.BytesTillEnd));
            }

            object body = TLRig.Deserialize(context);

            return new Message(msgId, seqNo, body);
        }

        protected override void WriteTypedBody(Message message, TLSerializationContext context)
        {
            TLStreamer streamer = context.Streamer;

            streamer.WriteUInt64(message.MsgId);
            streamer.WriteUInt32(message.Seqno);

            // Skip 4 bytes for a body length.
            streamer.Position += 4;

            long bodyStartPosition = streamer.Position;
            TLRig.Serialize(message.Body, context, TLSerializationMode.Boxed);
            long bodyEndPosition = streamer.Position;

            long bodyLength = bodyEndPosition - bodyStartPosition;

            streamer.Position = bodyStartPosition - 4;

            // Write a body length.
            streamer.WriteInt32((int)bodyLength);

            streamer.Position = bodyEndPosition;
        }
    }
}
