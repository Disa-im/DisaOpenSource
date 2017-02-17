using System;
using ProtoBuf;

namespace Disa.Framework
{
    [Serializable]
    [ProtoContract]
    [ProtoInclude(101, typeof(BotCommand))]
    [ProtoInclude(102, typeof(Hashtag))]
    [ProtoInclude(103, typeof(Username))]
    public class Mentions
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
    }
}
