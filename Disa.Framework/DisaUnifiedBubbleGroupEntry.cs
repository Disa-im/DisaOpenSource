using ProtoBuf;

namespace Disa.Framework
{

    [ProtoContract]
    public class DisaUnifiedBubbleGroupEntry
    {
        [ProtoMember(1)]
        public string[] GroupIds { get; set; }

        [ProtoMember(2)]
        public string PrimaryGroupId { get; set; }

        [ProtoMember(3)]
        public string SendingGroupId { get; set; }

        [ProtoMember(4)]
        public string Id { get; set; }

        public DisaUnifiedBubbleGroupEntry(string id, string[] groupIds, string primaryGroupId, string sendingGroupId)
        {
            Id = id;
            GroupIds = groupIds;
            PrimaryGroupId = primaryGroupId;
            SendingGroupId = sendingGroupId;
        }

        public DisaUnifiedBubbleGroupEntry()
        {

        }
    }
}