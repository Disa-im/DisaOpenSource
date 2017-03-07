namespace Disa.Framework.Bots
{
    public abstract class BotInlineResultBase
    {
        public string Id { get; set; }
        public long QueryId { get; set; }
        // TODO
        // public Document Document { get; set; }
        public DisaThumbnail Photo { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public BotInlineMessage SendMessage { get; set; }
        public string ContentUrl { get; set; }
    }

    public class BotInlineResult : BotInlineResultBase
    {
        public string Url { get; set; }
        public string ThumbUrl { get; set; }
        public string ContentType { get; set; }
        public int W { get; set; }
        public int H { get; set; }
        public int Duration { get; set; }
    }

    public class BotInlineMediaResult : BotInlineResultBase
    {
    }

}
