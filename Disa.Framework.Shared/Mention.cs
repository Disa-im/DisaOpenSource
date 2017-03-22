using System;
using ProtoBuf;
using Disa.Framework.Bots;

namespace Disa.Framework
{
    [Serializable]
    [ProtoContract]
    public class Mention
    {
        /// <summary>
        /// Holds the token value used for usernames, hashtags or bot commands.
        /// </summary>
        [ProtoMember(1)]
        public MentionType Type { get; set; }

        /// <summary>
        /// For a <see cref="Mention"/> that applies to a particular <see cref="BubbleGroup"/>, thiw
        /// will hold the <see cref="BubbleGroup.ID"/>.
        /// </summary>
        [ProtoMember(2)]
        public string BubbleGroupId { get; set; }

        /// <summary>
        /// Usernames - holds username
        /// Hashtags - holds hashtag
        /// Bot Commands - holds command name
        /// </summary>
        [ProtoMember(3)]
        public string Value { get; set; }

        /// <summary>
        /// For a <see cref="MentionType.Username"/> or <see cref="MentionType.BotCommand"/> mention, holds 
        /// the <see cref="DisaParticipant.Name"/>.
        /// </summary>
        [ProtoMember(4)]
        public string Name { get; set; }

        /// <summary>
        /// For a <see cref="UsernameMention"/> or <see cref="MentionType.BotCommand"/>mention, holds 
        /// the <see cref="DisaParticipant.Address"/>.
        /// </summary>
        [ProtoMember(5)]
        public string Address { get; set; }

        /// <summary>
        /// For a <see cref="MentionType.BotCommand"/> mention, holds the <see cref="BotInfo"/> information 
        /// for the Bot such as commands.
        /// </summary>
        [ProtoMember(6)]
        public BotInfo BotInfo { get; set; }
    }
}
