namespace Disa.Framework.Bot
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
}
