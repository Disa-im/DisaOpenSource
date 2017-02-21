using System;
using ProtoBuf;

namespace Disa.Framework
{
    [Serializable]
    [ProtoContract]
    [ProtoInclude(101, typeof(BotCommandMention))]
    [ProtoInclude(102, typeof(HashtagMention))]
    [ProtoInclude(103, typeof(UsernameMention))]
    public class Mention
    {
        /// <summary>
        /// Holds the token value used for usernames, hashtags or bot commands.
        /// </summary>
        [ProtoMember(1)]
        public string Token { get; set; }

        /// <summary>
        /// For usernames and bot commands, this will be the group id the
        /// username or bot command belongs to.
        /// 
        /// For hashtags, this will be <see cref="string.Empty"/> as hashtags
        /// apply across all groups.
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
        /// For a <see cref="UsernameMention"/> mention, holds the <see cref="DisaParticipant.Name"/> for this username. 
        /// </summary>
        [ProtoMember(4)]
        public string Name { get; set; }

        /// <summary>
        /// For a <see cref="UsernameMention"/> mention, holds the <see cref="DisaParticipant.Address"/> for this username. 
        /// </summary>
        [ProtoMember(5)]
        public string Address { get; set; }

        /// <summary>
        /// For a <see cref="BotCommandMention"/> mention, holds the Bot username.
        /// </summary>
        [ProtoMember(6)]
        public string BotUsername { get; set; }

    }
}
