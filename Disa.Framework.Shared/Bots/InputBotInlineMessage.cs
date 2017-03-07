using System;
using System.Collections.Generic;
using System.Text;

namespace Disa.Framework.Bots
{
    public abstract class InputBotInlineMessage
    {
    }

    public class InputBotInlineMessageMediaAuto : InputBotInlineMessage
    {
        public string Caption { get; set; }

        public KeyboardMarkup KeyboardMarkup { get; set; }
    }

    public class InputBotInlineMessageText : InputBotInlineMessage
    {
        public bool NoWebPage { get; set; }

        public string Message { get; set; }

        public List<BubbleMarkup> BubbleMarkup { get; set; }
    }

    public class InputBotInlineMessageMediaGeo : InputBotInlineMessage
    {
        // TODO
        // public InputGeoPoint GeoPoint { get; set; }

        public KeyboardMarkup KeyboardMarkup { get; set; }
    }

    public class InputBotInlineMessageMediaVenue : InputBotInlineMessage
    {
        // TODO
        // public InputGeoPoint GeoPoint { get; set; }

        public string Title { get; set; }

        public string Address { get; set; }

        public string Provider { get; set; }

        public string VenueId { get; set; }

        public KeyboardMarkup KeyboardMarkup { get; set; }
    }

    public class InputBotInlineMessageMediaContact : InputBotInlineMessage
    {

        public string PhoneNumber { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public KeyboardMarkup KeyboardMarkup { get; set; }
    }
}
