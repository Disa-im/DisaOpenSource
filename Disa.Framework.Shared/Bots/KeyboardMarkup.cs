using ProtoBuf;
using System;
using System.Collections.Generic;

namespace Disa.Framework.Bots
{
    [Serializable]
    [ProtoContract]
    [ProtoInclude(101, typeof(KeyboardMarkupHide))]
    [ProtoInclude(102, typeof(KeyboardMarkupForceReply))]
    [ProtoInclude(103, typeof(KeyboardCustomMarkup))]
    [ProtoInclude(104, typeof(KeyboardInlineMarkup))]
    public abstract class KeyboardMarkup
    {
    }

    [Serializable]
    [ProtoContract]
    public class KeyboardMarkupHide : KeyboardMarkup
    {
        [ProtoMember(1)]
        public bool Selective { get; set; }

    }

    /// <summary>
    /// Upon receiving a message with this <see cref="KeyboardMarkup"/>, clients will display a reply interface 
    /// to the user (act as if the user has selected the bot‘s message and tapped ’Reply'). 
    /// 
    /// This can be extremely useful if you want to create user-friendly step-by-step interfaces 
    /// without having to sacrifice privacy mode.
    /// 
    /// Example: A poll bot for groups runs in privacy mode (only receives commands, replies to its messages and mentions). 
    /// There could be two ways to create a new poll:
    /// 1. Explain the user how to send a command with parameters(e.g. /newpoll question answer1 answer2). 
    /// May be appealing for hardcore users but lacks modern day polish.
    /// 2. Guide the user through a step-by-step process. ‘Please send me your question’, ‘Cool, now let’s add the first answer option‘,
    /// ’Great.Keep adding answer options, then send /done when you‘re ready’.
    /// 
    /// The last option is definitely more attractive.And if you use ForceReply in your bot‘s questions, 
    /// it will receive the user’s answers even if it only receives replies, commands and mentions — without 
    /// any extra work for the user.
    /// </summary>
    [Serializable]
    [ProtoContract]
    public class KeyboardMarkupForceReply : KeyboardMarkup
    {
        [ProtoMember(1)]
        public bool SingleUse { get; set; }

        /// <summary>
        /// Optional. Use this parameter if you want to force reply from specific users only. 
        /// Targets: 
        /// 1) users that are @mentioned in the text of the Message object; 
        /// 2) if the bot's message is a reply (has reply_to_message_id), sender of the original message.
        /// 
        /// Example: A user requests to change the bot‘s language, bot replies to the request with a keyboard
        /// to select the new language. Other users in the group don’t see the keyboard.
        /// </summary>
        [ProtoMember(2)]
        public bool Selective { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public class KeyboardCustomMarkup : KeyboardMarkup
    {
        [ProtoMember(1)]
        public bool Resize { get; set; }

        [ProtoMember(2)]
        public bool SingleUse { get; set; }

        [ProtoMember(3)]
        public bool Selective { get; set; }

        [ProtoMember(4)]
        public List<KeyboardButtonRow> Rows { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public class KeyboardInlineMarkup : KeyboardMarkup
    {
        [ProtoMember(1)]
        public List<KeyboardButtonRow> Rows { get; set; }
    }
}
