namespace Disa.Framework.Bot
{
    public class BotInlineMessageMediaContact : BotInlineMessage
    {
        public string PhoneNumber { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        // TODO
        // public IReplyMarkup ReplyMarkup { get; set; }
    }
}
