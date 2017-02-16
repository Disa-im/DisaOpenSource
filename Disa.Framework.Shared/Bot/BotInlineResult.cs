namespace Disa.Framework.Bot
{
    public class BotInlineResult : BotInlineResultBase
    {
        public string Url { get; set; }
        public string ThumbUrl { get; set; }
        public string ContentType { get; set; }
        public int W { get; set; }
        public int H { get; set; }
        public int Duration { get; set; }
    }
}
