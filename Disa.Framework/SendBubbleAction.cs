using System;

namespace Disa.Framework
{
    public class SendBubbleAction
    {
        public enum ActionType 
        {         
            Typing,
            Recording,
            Nothing 
        }

        public string Address { get; internal set; }
        public ActionType Type { get; internal set; }
    }
}

