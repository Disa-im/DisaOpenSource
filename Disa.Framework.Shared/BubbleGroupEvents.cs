using System;
using System.Collections.Generic;
using Disa.Framework.Bubbles;

namespace Disa.Framework
{
    public static class BubbleGroupEvents
    {
        public static event BubbleFailed OnBubbleFailed;
        public static event BubbleInserted OnBubbleInserted;
        public static event NewAbstractBubble OnNewAbstractBubble;

        public delegate void BubbleFailed(VisualBubble bubble, BubbleGroup bubbleGroup);
        public delegate void BubbleInserted(VisualBubble bubble, BubbleGroup bubbleGroup);
        public delegate void NewAbstractBubble(AbstractBubble bubble, BubbleGroup bubbleGroup);

        private static Action<IEnumerable<BubbleGroup>> _refreshed; // bubble group updated, conversation list needs refresh
        private static Action<IEnumerable<BubbleGroup>> _bubblesUpdated; // bubble group bubbles updated, conversation bubbldes need to be updated
        private static Action<IEnumerable<BubbleGroup>> _informationUpdated; // bubble group last-seen or name updated, conversation info needs update
        private static Action<UnifiedBubbleGroup> _sendingServiceChanged;
        private static Action<BubbleGroup> _syncReset;

        public static void RegisterSyncReset(Action<BubbleGroup> syncReset)
        {
            _syncReset = syncReset;
        }

        internal static void RaiseSyncReset(BubbleGroup group)
        {
            if (_syncReset != null)
            {
                _syncReset(group);
            }
        }

        internal static void RaiseRefreshed(IEnumerable<BubbleGroup> group)
        {
            if (_refreshed != null)
                _refreshed(@group);
        }

        internal static void RaiseRefreshed(BubbleGroup group)
        {
            if (_refreshed != null)
                _refreshed(new [] { @group });
        }

        public static void RegisterRefreshed(Action<IEnumerable<BubbleGroup>> update)
        {
            _refreshed = update;
        }

        internal static void RaiseBubblesUpdated(IEnumerable<BubbleGroup> group)
        {
            if (_bubblesUpdated != null)
                _bubblesUpdated(@group);
        }

        internal static void RaiseBubblesUpdated(BubbleGroup group)
        {
            if (_bubblesUpdated != null)
                _bubblesUpdated(new [] { @group });
        }

        internal static void RaiseSendingServiceChange(UnifiedBubbleGroup bubbleGroup)
        {
            if (_sendingServiceChanged != null)
            {
                _sendingServiceChanged(bubbleGroup);
            }
        }

        public static void RegisterSendingServiceChanged(Action<UnifiedBubbleGroup> sendingServiceChanged)
        {
            _sendingServiceChanged = sendingServiceChanged;
        }

        public static void RegisterBubblesUpdated(Action<IEnumerable<BubbleGroup>> update)
        {
            _bubblesUpdated = update;
        }

        internal static void RaiseInformationUpdated(IEnumerable<BubbleGroup> group)
        {
            if (_informationUpdated != null)
                _informationUpdated(@group);
        }

        internal static void RaiseInformationUpdated(BubbleGroup group)
        {
            if (_informationUpdated != null)
                _informationUpdated(new [] { @group });
        }

        public static void RegisterInformationUpdated(Action<IEnumerable<BubbleGroup>> update)
        {
            _informationUpdated = update;
        }

        public static void RaiseBubbleInserted(VisualBubble bubble, BubbleGroup bubbleGroup)
        {
            if (OnBubbleInserted != null)
            {
                OnBubbleInserted(bubble, bubbleGroup);
            }
        }

        public static void RaiseBubbleFailed(VisualBubble bubble, BubbleGroup bubbleGroup)
        {
            if (OnBubbleFailed != null)
            {
                OnBubbleFailed(bubble, bubbleGroup);
            }
        }

        internal static void RaiseNewAbstractBubble(AbstractBubble bubble, BubbleGroup bubbleGroup)
        {
            if (OnNewAbstractBubble != null)
            {
                OnNewAbstractBubble(bubble, bubbleGroup);
            }
        }
    }
}