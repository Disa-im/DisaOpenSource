namespace Disa.Framework.Bots
{
    public abstract class BotInlineMessage
    {
    }

    public class BotInlineMessageMediaAuto : BotInlineMessage
    {
        public string Caption { get; set; }
        // TODO
        // public IReplyMarkup ReplyMarkup { get; set; }
    }

    public class BotInlineMessageMediaContact : BotInlineMessage
    {
        public string PhoneNumber { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        // TODO
        // public IReplyMarkup ReplyMarkup { get; set; }
    }

    public class BotInlineMessageMediaGeo : BotInlineMessage
    {
        // TODO
        // public IGeoPoint Geo { get; set; }
        // public IReplyMarkup ReplyMarkup { get; set; }
    }

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

    public class BotInlineMessageText : BotInlineMessage
    {
        // TODO: What is this?
        public bool NoWebpage { get; set; }

        public string Message { get; set; }
        // TODO
        // public List<IMessageEntity> Entities { get; set; }
        // public IReplyMarkup ReplyMarkup { get; set; }
    }

}
