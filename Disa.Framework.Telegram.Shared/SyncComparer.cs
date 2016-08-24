using Disa.Framework.Bubbles;

namespace Disa.Framework.Telegram
{
    public partial class Telegram : BubbleGroupSync.Comparer
    {
        public bool LoadBubblesComparer(VisualBubble localBubble, VisualBubble agentBubble)
        {
            if (localBubble.IdService == agentBubble.IdService)
            {
                return true;
            }
            return false;
        }
    }
}