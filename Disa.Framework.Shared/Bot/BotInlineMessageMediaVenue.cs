namespace Disa.Framework.Bot
{
    public class BotInlineMessageMediaVenue : BotInlineMessage
    {
        // TODO
        // public IGeoPoint Geo { get; set; }
        public string Title { get; set; }
        public string Address { get; set; }
        public string Provider { get; set; }
        public string VenueId { get; set; }
        // TODO
        // public IReplyMarkup ReplyMarkup { get; set; }
    }
}
