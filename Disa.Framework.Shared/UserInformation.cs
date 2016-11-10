namespace Disa.Framework
{
    public class UserInformation
    {
        public enum TypeSubtitle { Other, PhoneNumber }

        public string Status { get; set; }
        public string Title { get; set; }
        public TypeSubtitle SubtitleType { get; set; }
        public string Subtitle { get; set; }
        public long LastSeen { get; set; }
        public bool Presence { get; set; }
        public string UserHandle { get; set; }
    }
}

