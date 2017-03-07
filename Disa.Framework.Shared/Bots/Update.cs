using System;
using System.Collections.Generic;
using System.Text;

namespace Disa.Framework.Bots
{
    public class UpdateBotInlineQuery
    {
        public int QueryId { get; set; }


        public int UserId { get; set; }

        public string Query { get; set; }

        // TODO
        // public IGeoPoint Geo { get; set; }


        public string Offset { get; set; }
    }

    public class UpdateBotInlineSend
    {
        public int UserId { get; set; }

        public string Query { get; set; }

        // TODO
        // public IGeoPoint Geo { get; set; }

        public string Id { get; set; }

        public InputBotInlineMessageID MsgId { get; set; }
    }

    public class UpdateBotCallbackQuery
    {
        public int QueryId { get; set; }

        public int UserId { get; set; }

        // TODO
        // public IPeer Peer { get; set; }

        public int MsgId { get; set; }

        public byte[] Data { get; set; }
    }

    public class UpdateInlineBotCallbackQuery
    {
        public int QueryId { get; set; }

        public int UserId { get; set; }

        public InputBotInlineMessageID MsgId { get; set; }

        public byte[] Data { get; set; }
    }
}
