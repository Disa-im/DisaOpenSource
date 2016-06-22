using Disa.Framework.Bubbles;

namespace Disa.Framework.Telegram
{
    public partial class Telegram : BubbleGroupSync.Comparer
    {
        public bool LoadBubblesComparer(VisualBubble localBubble, VisualBubble agentBubble)
        {
            DebugPrint("###### Local bubble id service " + localBubble.IdService);
            DebugPrint("###### agent bubble id service " + agentBubble.IdService);
            if (localBubble.IdService == agentBubble.IdService)
            {
                return true;
            }
            return false;
        }
    }
}