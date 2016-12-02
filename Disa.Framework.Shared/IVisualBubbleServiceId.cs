using Disa.Framework.Bubbles;

namespace Disa.Framework
{
    [DisaFramework]
    public interface IVisualBubbleServiceId
    {
        void AddVisualBubbleIdServices(VisualBubble bubble);

        bool DisctinctIncomingVisualBubbleIdServices();
    }
}