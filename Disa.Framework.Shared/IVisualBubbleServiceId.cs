using Disa.Framework.Bubbles;

namespace Disa.Framework
{
    public interface IVisualBubbleServiceId
    {
        void AddVisualBubbleIdServices(VisualBubble bubble);

        bool DisctinctIncomingVisualBubbleIdServices();
    }
}