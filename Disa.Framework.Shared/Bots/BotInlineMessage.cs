using System.Collections.Generic;

namespace Disa.Framework.Bots
{
    public abstract class BotInlineMessage
    {
        public KeyboardInlineMarkup KeyboardInlineMarkup { get; set; }
    }

    public class BotInlineMessageMediaAuto : BotInlineMessage
    {
        public string Caption { get; set; }
    }

    public class BotInlineMessageMediaContact : BotInlineMessage
    {
        public string PhoneNumber { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public class BotInlineMessageMediaGeo : BotInlineMessage
    {
        public GeoPointBase Geo { get; set; }
    }

    public class BotInlineMessageMediaVenue : BotInlineMessage
    {
        public GeoPointBase Geo { get; set; }
        public string Title { get; set; }
        public string Address { get; set; }
        public string Provider { get; set; }
        public string VenueId { get; set; }
    }

    public class BotInlineMessageText : BotInlineMessage
    {
        public bool NoWebpage { get; set; }
        public string Message { get; set; }
        public List<BubbleMarkup> BubbleMarkups { get; set; }
    }
}
