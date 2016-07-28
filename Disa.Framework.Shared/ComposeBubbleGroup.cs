using Disa.Framework.Bubbles;

namespace Disa.Framework
{
    public class ComposeBubbleGroup : BubbleGroup
    {
        public Contact.ID[] Ids { get; private set; }

        public ComposeBubbleGroup(NewBubble newBubble, Contact.ID[] ids, string title)
            : base(newBubble)
        {
            Title = string.IsNullOrWhiteSpace(title) ? null : title;
            Photo = null;
            Ids = ids;
        }
    }
}