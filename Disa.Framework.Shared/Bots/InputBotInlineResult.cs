using System;
using System.Collections.Generic;
using System.Text;

namespace Disa.Framework.Bots
{
    public abstract class InputBotInlineResult
    {
    }

    public class InputBotInlineResultStandard : InputBotInlineResult
    {
        public string Id { get; set; }

        public string Type { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string Url { get; set; }

        public string ThumbUrl { get; set; }

        public string ContentUrl { get; set; }

        public string ContentType { get; set; }

        public int W { get; set; }

        public int H { get; set; }

        public int Duration { get; set;  }

        public InputBotInlineMessage SendMessage { get; set; }
    }

    public class InputBotInlineResultPhoto : InputBotInlineResult
    {
        public string Id { get; set; }

        public string Type { get; set; }

        // TODO
        // public IInputPhoto Photo { get; set; }

        public InputBotInlineMessage SendMessage { get; set; }
    }

    public class InputBotInlineResultDocument : InputBotInlineResult
    {
        public string Id { get; set; }

        public string Type { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        // TODO
        // public IInputDocument Document { get; set; }

        public InputBotInlineMessage SendMessage { get; set; }
    }

}

