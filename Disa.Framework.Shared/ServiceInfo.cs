using System;
using System.Linq;
using System.Reflection;


namespace Disa.Framework
{
    /// <summary>
    /// Describes the general capabilities offered by this <see cref="Service"/>.
    /// 
    /// After the <see cref="Service"/> is constructed, you can get access to the <see cref="ServiceInfo"/>
    /// instance associated with the <see cref="Service"/> via the <see cref="Service.Information"/> property.
    /// 
    /// IMPORTANT: If you support audio, files, gifs, stickers or videos you need to add the associated
    /// attribute.
    /// <see cref="AudioParameters"/>
    /// <see cref="FileParameters"/>
    /// <see cref="GifParameters"/>
    /// <see cref="StickerParameters"/>
    /// <see cref="VideoParameters"/>
    /// 
    /// After the <see cref="Service"/> is constructed, you can get access to these parameter instances via:
    /// <see cref="Service.AudioParameters"/>
    /// <see cref="Service.FileParameters"/>
    /// <see cref="Service.GifParameters"/>
    /// <see cref="Service.StickerParameters"/>
    /// <see cref="Service.VideoParameters"/>
    /// 
    /// If you do not specify one of these attribute parameters, a default instance of the parameter class will
    /// be instantiated and assigned to the property.
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public class ServiceInfo : Attribute
    {
        /// <summary>
        /// Specifies the order that the following methods should be called upon starting the <see cref="Service"/>:
        /// 
        /// <see cref="Service.Connect(WakeLock)"/>
        /// <see cref="Service.Authenticate(WakeLock)"/>
        /// </summary>
        public enum ProcedureType
        {
            /// <summary>
            /// Upon starting the <see cref="Service"/> first the <see cref="Service.Connect(WakeLock)"/> will be called
            /// then the <see cref="Service.Authenticate(WakeLock)"/> will be called.
            /// </summary>
            ConnectAuthenticate,

            /// <summary>
            /// Upon starting the <see cref="Service"/> first the <see cref="Service.Authenticate(WakeLock)"/> will be called
            /// then the <see cref="Service.Connect(WakeLock)"/> will be called.
            /// </summary>
            AuthenticateConnect
        };

        /// <summary>
        /// The name of the <see cref="Service"/>.
        /// 
        /// Can be retrieved later using <see cref="Service.Information.ServiceName"/>.
        /// </summary>
        public string ServiceName { get; private set; }

        /// <summary>
        /// An array of <see cref="Bubble"/> that represents the collection of <see cref="Bubble"/>s
        /// supported by this <see cref="Service"/>.
        /// </summary>
        public Type[] SupportedBubbles { get; private set; }

        /// <summary>
        /// A <see cref="DisaSettings"/> derived value class to hold settings specific to this <see cref="Service"/>.
        /// 
        /// The <see cref="Service.Initialize(DisaSettings)"/> will be called upon starting the service passing in the latest
        /// set of settings stored away for this <see cref="Service"/>.
        /// 
        /// You can use the <see cref="SettingsManager"/> Load and Save methods for maintaining your <see cref="Service"/> specific
        /// settings. 
        /// 
        /// Additionally, you can use <see cref="DisaMutableSettings"/> and <see cref="MutableSettingsManager"/> to save information
        /// you find yourself frequently saving (such as a timestamp you need to keep updated everytime the service is started).
        /// </summary>
        public Type Settings { get; private set; }

        /// <summary>
        /// If set to False, then the <see cref="Service"/>'s <see cref="Service.ProcessBubbles"/> will be called in a loop assuming
        /// the <see cref="Service"/> will respond from the method when it has <see cref="Bubble"/>s to process.
        /// If set to True, then the <see cref="Service"/> will publish receipt of <see cref="Bubble"/>s by calling
        /// <see cref="Service.EventBubble(Bubble)"/>.
        /// 
        /// Discussion:
        /// Some services require a dedicated thread to be infinitely polling against a keep-alive connection. 
        /// By setting event driven bubbles to false, the ProcessBubbles iterator block is called in an 
        /// infinite threaded loop while the service is running. Thus, the Framework completely manages this
        /// aspect of keeping the poller constantly alive. By settings event driven bubbles to true, 
        /// you are effectively telling the Framework: "I want to manage all the polling myself, and 
        /// invoke off the EventBubble method whenever I a new bubble comes in."
        /// </summary>
        public bool EventDrivenBubbles { get; private set; }

        /// <summary>
        /// If it we set this to true, then the Framework will ensure that the service is stopped if there 
        /// is no internet connection.
        /// </summary>
        public bool UsesInternet { get; private set; }

        /// <summary>
        /// If your <see cref="Service"/> can support giving feedback back to the client on the upload process of 
        /// media bubbles (images, videos, etc), you'll set this flag to true and then use the Transfer.Progress 
        /// callback in the associated media bubble you're uploading.
        /// </summary>
        public bool UsesMediaProgress { get; private set; }

        /// <summary>
        /// If your <see cref="Service"/> can support quoting (also known as replying) to a <see cref="VisualBubble"/>
        /// then set this to true. Otherwise, set this to false.
        /// </summary>
        public bool SendingQuotes { get; private set; }

        /// <summary>
        /// Set to to true if the <see cref="Service"/> supports battery savings mode, set to false if not.
        /// 
        /// Currently not used. You can safely set this to false.
        /// </summary>
        public bool SupportsBatterySavingsMode { get; private set; }

        /// <summary>
        /// The <see cref="ProcedureType"/> supported by this <see cref="Service"/>.
        /// 
        /// See the documentation on <see cref="ProcedureType"/> for a complete description.
        /// </summary>
        public ProcedureType Procedure { get; private set; }

        /// <summary>
        /// Delayed notifications will delay notification dispatches by 1 (one) second. 
        /// 
        /// Setting this to true and using NotificationManager.Remove allows you to have multiple 
        /// clients working together without notifications going off while chatting on another client.
        /// </summary>
        public bool DelayedNotifications { get; private set; }

        /// <summary>
        /// Constuct the <see cref="ServiceInfo"/> <see cref="Attribute"/> passing in a subset of
        /// parameters.
        /// 
        /// Does not support setting <see cref="SendingQuotes"/>.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="eventDrivenBubbles"></param>
        /// <param name="usesMediaProgress"></param>
        /// <param name="usesInternet"></param>
        /// <param name="supportsBatterySavingsMode"></param>
        /// <param name="delayedNotifications"></param>
        /// <param name="settings"></param>
        /// <param name="procedureType"></param>
        /// <param name="supportedBubbles"></param>
        public ServiceInfo(
            string serviceName, 
            bool eventDrivenBubbles, 
            bool usesMediaProgress,
            bool usesInternet, 
            bool supportsBatterySavingsMode, 
            bool delayedNotifications, 
            Type settings, 
            ProcedureType procedureType,
            params Type[] supportedBubbles)
        {
            ServiceName = serviceName;
            EventDrivenBubbles = eventDrivenBubbles;
            SupportedBubbles = supportedBubbles;
            Settings = settings;
            UsesInternet = usesInternet;
            UsesMediaProgress = usesMediaProgress;
            Procedure = procedureType;
            SupportsBatterySavingsMode = supportsBatterySavingsMode;
            DelayedNotifications = delayedNotifications;
        }

        /// <summary>
        /// Constuct the <see cref="ServiceInfo"/> <see cref="Attribute"/>.
        /// 
        /// Includes support for setting <see cref="SendingQuotes"/>.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="eventDrivenBubbles"></param>
        /// <param name="usesMediaProgress"></param>
        /// <param name="usesInternet"></param>
        /// <param name="supportsBatterySavingsMode"></param>
        /// <param name="delayedNotifications"></param>
        /// <param name="sendingQuotes"></param>
        /// <param name="settings"></param>
        /// <param name="procedureType"></param>
        /// <param name="supportedBubbles"></param>
        public ServiceInfo(
            string serviceName, 
            bool eventDrivenBubbles, 
            bool usesMediaProgress,
            bool usesInternet, 
            bool supportsBatterySavingsMode, 
            bool delayedNotifications, 
            bool sendingQuotes,
            Type settings, 
            ProcedureType procedureType, 
            params Type[] supportedBubbles)
        {
            ServiceName = serviceName;
            EventDrivenBubbles = eventDrivenBubbles;
            SupportedBubbles = supportedBubbles;
            Settings = settings;
            UsesInternet = usesInternet;
            UsesMediaProgress = usesMediaProgress;
            Procedure = procedureType;
            SendingQuotes = sendingQuotes;
            SupportsBatterySavingsMode = supportsBatterySavingsMode;
            DelayedNotifications = delayedNotifications;
        }

        internal void SetServiceName(string name)
        {
            ServiceName = name;
        }

        /// <summary>
        /// Given a <see cref="Bubble"/>, does the <see cref="Service"/> support that <see cref="Bubble"/> type.
        /// 
        /// True if yes, false if no.
        /// </summary>
        /// <param name="bubble"></param>
        /// <returns></returns>
        public bool DoesSupport(Type bubble)
        {
            if (SupportedBubbles == null)
                return false;

            return SupportedBubbles.FirstOrDefault(b => bubble == b) != null;
        }
    }

    /// <summary>
    /// Allows the <see cref="Service"/> to specify which <see cref="Bubble"/> types not to queue for a retry if sending
    /// failed but rather to fail them immediately.
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public class QueuedBubblesParameters : Attribute
    {
        /// <summary>
        /// The collection of <see cref="Bubble"/> <see cref="Type"/>s to fail immediately if sending failed.
        /// 
        /// That is,  they will not be queued up to retry sending.
        /// </summary>
        public Type[] BubblesNotToQueue { get; private set; }

        /// <summary>
        /// Upon <see cref="Service"/> startup, the collection of <see cref="Bubble"/> <see cref="Type"/>s 
        /// that are candidates to be queued up from a past session to fail immediately.
        /// 
        /// That is, they will not be queued up to retry sending.
        /// </summary>
        public Type[] SendingBubblesToFailOnServiceStart { get; private set; }

        /// <summary>
        /// Construct an instance of <see cref="QueuedBubblesParameters"/>.
        /// </summary>
        /// <param name="bubblesNotToQueue"></param>
        /// <param name="sendingBubblesToFailOnServiceStart"></param>
        public QueuedBubblesParameters(Type[] bubblesNotToQueue, Type[] sendingBubblesToFailOnServiceStart)
        {
            SendingBubblesToFailOnServiceStart = sendingBubblesToFailOnServiceStart;
            BubblesNotToQueue = bubblesNotToQueue;
        }
    }

    /// <summary>
    /// Allows a <see cref="Service"/> to specify limits on the sending and receipt of <see cref="FileBubble"/>s.
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public class FileParameters : Attribute
    {
        /// <summary>
        /// The value to specify if this <see cref="Service"/> has no size limit on the sending and
        /// reciept of <see cref="FileBubble"/>s.
        /// </summary>
        public const int NoSizeLimit = -1;

        /// <summary>
        /// Specify the size in bytes this <see cref="Service"/> imposes for sending and receipt of 
        /// <see cref="FileBubble"/>s.
        /// </summary>
        public long SizeLimit { get; set; }

        /// <summary>
        /// Construct an instance of <see cref="FileParameters"/> specifying the file size limit.
        /// </summary>
        /// <param name="sizeLimit"></param>
        public FileParameters(long sizeLimit)
        {
            SizeLimit = sizeLimit;
        }
    }

    /// <summary>
    /// Specifies that the <see cref="Service"/> has a plugin specific settings page that will be called
    /// upon initial setup of the plugin and available in the Settings | Service | Overflow menu | Settings
    /// menu option.
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public class PluginSettingsUI : Attribute
    {
        /// <summary>
        /// Specifies a class that implements <see cref="Disa.Framework.Mobile.IPluginPage"/>.   
        /// </summary>
        public Type Service { get; private set; }

        /// <summary>
        /// Construct an instance of the <see cref="PluginSettingsUI"/> specifying the <see cref="Type"/> of
        /// the class that implements <see cref="Disa.Framework.Mobile.IPluginPage"/>.
        /// </summary>
        /// <param name="service"></param>
        public PluginSettingsUI(Type service)
        {
            Service = service;
        }
    }

    /// <summary>
    /// Specifies the characteristics of the audio files supported by this <see cref="Service"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public class AudioParameters : Attribute
    {
        /// <summary>
        /// Used to specify the audio recording types supported by this <see cref="Service"/>.
        /// </summary>
        public enum RecordType
        {
            /// <summary>
            /// The m4a audio recording type.
            /// </summary>
            M4A,

            /// <summary>
            /// The 3gp audio recording type.
            /// </summary>
            _3GP
        };

        /// <summary>
        /// Used to specify that this <see cref="Service"/> does not have a size limit on the audio file.
        /// </summary>
        public const int NoSizeLimit = -1;

        /// <summary>
        /// Used to specify that this <see cref="Service"/> does not have a duration limit on the audio file.
        /// </summary>
        public const int NoDurationLimit = -1;

        /// <summary>
        /// The audio duration limit supported by this <see cref="Service"/>.
        /// 
        /// Specified in seconds.
        /// </summary>
        public int DurationLimit { get; set; }

        /// <summary>
        /// The audio file size limit supported by this <see cref="Service"/>.
        /// 
        /// Specified in bytes.
        /// </summary>
        public long SizeLimit { get; set; }

        /// <summary>
        /// The audio file extensions supported by this <see cref="Service"/>.
        /// </summary>
        public string[] SupportedExtensions { get; set; }

        /// <summary>
        /// The audio recording types supported by this <see cref="Service"/>.
        /// </summary>
        public RecordType AudioRecordType { get; set; }

        /// <summary>
        /// Construct an instance of <see cref="AudioParameters"/> specifying the characteristics it supports.
        /// </summary>
        /// <param name="recordType"></param>
        /// <param name="durationLimit"></param>
        /// <param name="sizeLimit"></param>
        /// <param name="supportedExtensions"></param>
        public AudioParameters(
            RecordType recordType, 
            int durationLimit, 
            int sizeLimit, 
            params string[] supportedExtensions)
        {
            DurationLimit = durationLimit;
            SizeLimit = sizeLimit;
            SupportedExtensions = supportedExtensions;
            AudioRecordType = recordType;
        }

        /// <summary>
        /// Given a string representing the file extension of the audio file, determine if this <see cref="Service"/>
        /// supports the extension.
        /// 
        /// True if the <see cref="Service"/> supports the extension, false if not.
        /// </summary>
        /// <param name="extension"></param>
        /// <returns></returns>
        public bool DoesSupportExtension(string extension)
        {
            return SupportedExtensions.FirstOrDefault(x => x.ToLower() == extension.ToLower()) != null;
        }
    }

    /// <summary>
    /// Specifies the characteristics of the video files supported by this <see cref="Service"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public class VideoParameters : Attribute
    {
        /// <summary>
        /// Used to specify the video recording types supported by this <see cref="Service"/>.
        /// </summary>
        public enum RecordType
        {
            /// <summary>
            /// The mp4 video recording type.
            /// </summary>
            Mp4,

            /// <summary>
            /// The 3gp video recording type.
            /// </summary>
            _3Gp
        };

        /// <summary>
        /// Used to specify that this <see cref="Service"/> does not have a size limit on the video file.
        /// </summary>
        public const int NoSizeLimit = -1;

        /// <summary>
        /// Used to specify that this <see cref="Service"/> does not have a duration limit on the video file.
        /// </summary>
        public const int NoDurationLimit = -1;

        /// <summary>
        /// The video recording types supported by this <see cref="Service"/>.
        /// </summary>
        public RecordType VideoRecordType { get; set; }

        /// <summary>
        /// The video file extensions supported by this <see cref="Service"/>.
        /// </summary>
        public string[] SupportedExtensions { get; set; }

        /// <summary>
        /// The video duration limit supported by this <see cref="Service"/>.
        /// 
        /// Specified in seconds.
        /// </summary>
        public int DurationLimit { get; set; }

        /// <summary>
        /// The video file size limit supported by this <see cref="Service"/>.
        /// 
        /// Specified in bytes.
        /// </summary>
        public long SizeLimit { get; set; }

        /// <summary>
        /// Construct an instance of the <see cref="VideoParameters"/> specifying the characteristics it supports.
        /// </summary>
        /// <param name="videoRecordType"></param>
        /// <param name="durationLimit"></param>
        /// <param name="sizeLimit"></param>
        /// <param name="supportedExtensions"></param>
        public VideoParameters(
            RecordType videoRecordType, 
            int durationLimit,
            long sizeLimit, 
            params string[] supportedExtensions)
        {
            VideoRecordType = videoRecordType;
            DurationLimit = durationLimit;
            SizeLimit = sizeLimit;
            SupportedExtensions = supportedExtensions;
        }

        /// <summary>
        /// Given a string representing the file extension of the audio file, determine if this <see cref="Service"/>
        /// supports the extension.
        /// 
        /// True if the <see cref="Service"/> supports the extension, false if not.
        /// </summary>
        /// <param name="extension"></param>
        /// <returns></returns>
        public bool DoesSupportExtension(string extension)
        {
            return SupportedExtensions.FirstOrDefault(x => x.ToLower() == extension.ToLower()) != null;
        }
    }

    /// <summary>
    /// Specifies the characteristics of the gif files supported by this <see cref="Service"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public class GifParameters : Attribute
    {
        /// <summary>
        /// Used to specify the gif recording types supported by this <see cref="Service"/>.
        /// </summary>
        public enum RecordType
        {
            /// <summary>
            /// The gif recording type.
            /// </summary>
            Gif
        };

        /// <summary>
        /// Used to specify that this <see cref="Service"/> does not have a size limit on the gif file.
        /// </summary>
        public const int NoSizeLimit = -1;

        /// <summary>
        /// The gif recording types supported by this <see cref="Service"/>.
        /// </summary>
        public RecordType GifRecordType { get; set; }

        /// <summary>
        /// The gif file extensions supported by this <see cref="Service"/>.
        /// </summary>
        public string[] SupportedExtensions { get; set; }

        /// <summary>
        /// The gif file size limit supported by this <see cref="Service"/>.
        /// 
        /// Specified in bytes.
        /// </summary>
        public long SizeLimit { get; set; }

        /// <summary>
        /// Construct an instance of the <see cref="GifParameters"/> specifying the characteristics it supports.
        /// </summary>
        /// <param name="gifRecordType"></param>
        /// <param name="durationLimit"></param>
        /// <param name="sizeLimit"></param>
        /// <param name="supportedExtensions"></param>
        public GifParameters(
            RecordType gifRecordType,
            long sizeLimit,
            params string[] supportedExtensions)
        {
            GifRecordType = gifRecordType;
            SizeLimit = sizeLimit;
            SupportedExtensions = supportedExtensions;
        }

        /// <summary>
        /// Given a string representing the file extension of the gif file, determine if this <see cref="Service"/>
        /// supports the extension.
        /// 
        /// True if the <see cref="Service"/> supports the extension, false if not.
        /// </summary>
        /// <param name="extension"></param>
        /// <returns></returns>
        public bool DoesSupportExtension(string extension)
        {
            return SupportedExtensions.FirstOrDefault(x => x.ToLower() == extension.ToLower()) != null;
        }
    }

    /// <summary>
    /// Specifies the characteristics of the sticker files supported by this <see cref="Service"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public class StickerParameters : Attribute
    {
        /// <summary>
        /// Used to specify the sticker recording types supported by this <see cref="Service"/>.
        /// </summary>
        public enum RecordType
        {
            /// <summary>
            /// The webp recording type.
            /// </summary>
            Webp
        };

        /// <summary>
        /// Used to specify that this <see cref="Service"/> does not have a size limit on the sticker file.
        /// </summary>
        public const int NoSizeLimit = -1;

        /// <summary>
        /// The sticker recording types supported by this <see cref="Service"/>.
        /// </summary>
        public RecordType StickerRecordType { get; set; }

        /// <summary>
        /// The sticker file extensions supported by this <see cref="Service"/>.
        /// </summary>
        public string[] SupportedExtensions { get; set; }

        /// <summary>
        /// The sticker file size limit supported by this <see cref="Service"/>.
        /// 
        /// Specified in bytes.
        /// </summary>
        public long SizeLimit { get; set; }

        /// <summary>
        /// Construct an instance of the <see cref="StickerParameters"/> specifying the characteristics it supports.
        /// </summary>
        /// <param name="stickerRecordType"></param>
        /// <param name="durationLimit"></param>
        /// <param name="sizeLimit"></param>
        /// <param name="supportedExtensions"></param>
        public StickerParameters(
            RecordType stickerRecordType,
            long sizeLimit,
            params string[] supportedExtensions)
        {
            StickerRecordType = stickerRecordType;
            SizeLimit = sizeLimit;
            SupportedExtensions = supportedExtensions;
        }

        /// <summary>
        /// Given a string representing the file extension of the gif file, determine if this <see cref="Service"/>
        /// supports the extension.
        /// 
        /// True if the <see cref="Service"/> supports the extension, false if not.
        /// </summary>
        /// <param name="extension"></param>
        /// <returns></returns>
        public bool DoesSupportExtension(string extension)
        {
            return SupportedExtensions.FirstOrDefault(x => x.ToLower() == extension.ToLower()) != null;
        }
    }

    [AttributeUsage(AttributeTargets.All)]
    public class PluginInfo : Attribute
    {
        public enum PluginType { Messaging, Media, }

        public PluginType Type { get; private set; }

        public PluginInfo(PluginType type)
        {
            Type = type;
        }
    }

    /// <summary>
    /// Used to flag an interface as belonging to the Disa Framework.
    /// 
    /// IMPORTANT: For internal use only. Do not use.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface)]
    public class DisaFrameworkAttribute : Attribute
    {
    }

    /// <summary>
    /// Used to flag an interface belonging to the Disa Framework as deprecated.
    /// 
    /// IMPORTANT: For internal use only. Do not use.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface)]
    public class DisaFrameworkDeprecated : Attribute
    {
    }

    /// <summary>
    /// Used to flag a method or property in the Disa Framwork as No-op.
    /// 
    /// IMPORTANT: For internal use only. Do not use.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public class DisaFrameworkNOP : Attribute
    {
    }

    /// <summary>
    /// Support class to:
    /// 1. Record a standard set of reference methods for the Disa Framework interfaces that might not be
    ///    implemented by a plugin implementation.
    /// 2. Methods to determine if a method is defined as missing (no implementation or marked with <see cref="DisaFrameworkNOP"/>)
    ///    from a plugin's interface implementation.
    ///    
    /// IMPORTANT: For internal use only. Do not use.
    /// </summary>
    public static class DisaFrameworkMethods
    {
        // INewMessage
        public static readonly string INewMessageFetchBubbleGroupAddressFromLink = "FetchBubbleGroupAddressFromLink";
        public static readonly string INewMessageSupportsShareLinks = "get_SupportsShareLinks";
        public static readonly string INewMessageSearchHint = "get_SearchHint";
        public static readonly string INewMessageGetContactsByUsername = "GetContactsByUsername";

        // IPartyOptions - Links
        public static readonly string IPartyOptionsHasPartyShareLink = "HasPartyShareLink";
        public static readonly string IPartyOptionsGetPartyShareLink = "GetPartyShareLink";
        public static readonly string IPartyOptionsCanGeneratePartyShareLink = "CanGeneratePartyShareLink";
        public static readonly string IPartyOptionsGeneratePartyShareLink = "GeneratePartyShareLink";
        public static readonly string IPartyOptionsCanSetPartyShareLink = "CanSetPartyShareLink";
        public static readonly string IPartyOptionsSetPartyShareLink = "SetPartyShareLink";
        public static readonly string IPartyOptionsGetPartyShareLinkMaxCharacters = "GetPartyShareLinkMaxCharacters";
        public static readonly string IPartyOptionsGetPartyShareLinkMinCharacters = "GetPartyShareLinkMinCharacters";

        // IPartyOptions - Desciption
        public static readonly string IPartyOptionsHasPartyDescription = "HasPartyDescription";
        public static readonly string IPartyOptionsGetPartyDescription = "GetPartyDescription";
        public static readonly string IPartyOptionsCanSetPartyDescription = "CanSetPartyDescription";
        public static readonly string IPartyOptionsSetPartyDescription = "SetPartyDescription";
        public static readonly string IPartyOptionsGetPartyDescriptionMaxCharacters = "GetPartyDescriptionMaxCharacters";
        public static readonly string IPartyOptionsGetPartyDescriptionMinCharacters = "GetPartyDescriptionMinCharacters";

        // IPartyOptions - Blocked Participants
        public static readonly string IPartyOptionsCanViewPartyBlockedParticipants = "CanViewPartyBlockedParticipants";

        // IPartyOptions - Demote Participants
        public static readonly string IPartyOptionsCanDemotePartyParticpantsFromLeader = "CanDemotePartyParticpantsFromLeader";
        public static readonly string IPartyOptionsDemotePartyParticipantsFromLeader = "DemotePartyParticipantsFromLeader";

        // IPartyOptions - Approve New Members
        public static readonly string IPartyOptionsHasApproveNewMembers = "HasApproveNewMembers";
        public static readonly string IPartyOptionsCanApproveNewMembers = "CanApproveNewMembers";
        public static readonly string IPartyOptionsGetApproveNewMembers = "GetApproveNewMembers";
        public static readonly string IPartyOptionsApproveNewMembers = "ApproveNewMembers";
        public static readonly string IPartyOptionsApproveNewMembersCount = "ApproveNewMembersCount";

        // IPartyOptionsSettings
        public static readonly string IPartyOptionsSettingsCanSignMessages = "CanSignMessages";
        public static readonly string IPartyOptionsSettingsGetSignMessages = "GetSignMessages";
        public static readonly string IPartyOptionsSettingsSignMessages = "SignMessages";

        // IPartyParticipantRequests
        public static readonly string IPartyParticipantRequestsGetPartyParticipantRequests = "GetPartyParticipantRequests";
        public static readonly string IPartyParticipantRequestsPartyParticipantRequestAction = "PartyParticipantRequestAction";

        // IUserInformtaion
        public static readonly string IUserInformationIsUserBotStopped = "IsUserBotStopped";
        public static readonly string IUserInformationEnableuserBot = "EnableUserBot";
        public static readonly string IUserInformationIsUserBot = "IsUserBot";

        // IVisualBubbleServiceId
        public static readonly string IVisualBubbleServiceIdCheckType = "CheckType";
        public static readonly string IVisualBubbleServiceIdVisualBubbleIdComparer = "VisualBubbleIdComparer";

        /// <summary>
        /// Is the plugin missing a valid method implementation for the method specified?
        /// 
        /// IMPORTANT: If a method is overloaded, it will test the validity of all overloaded methods
        ///            and if any are missing it will return True.
        /// </summary>
        /// <param name="plugin">The plugin we want to inspect for a valid method implementation.</param>
        /// <param name="method">The name of the method.</param>
        /// <returns>True if the method is defined as missing (no implementation or marked with <see cref="DisaFrameworkNOP"/>),
        /// False otherwise.</returns>
        public static bool Missing(object plugin, string method)
        {
            try
            {
                var methodInfo = plugin.GetType().GetMethod(method);
                if (methodInfo == null ||
                    methodInfo.IsDefined(typeof(DisaFrameworkNOP), false))
                {
                    return true;
                }

                return false;
            }
            catch (AmbiguousMatchException ex)
            {
                // The method we are inspecting is overloaded
                var methodInfos = plugin.GetType().GetMethods().Where(m => m.Name.Equals(method));
                foreach (var methodInfo in methodInfos)
                {
                    if (methodInfo.IsDefined(typeof(DisaFrameworkNOP), false))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Does the plugin have a valid method implementations for the methods specified? 
        /// 
        /// IMPORTANT: If a method is overloaded, it will test the validity of all overloaded methods
        ///            and if any are missing it will return True.
        /// </summary>
        /// <param name="plugin">The plugin we want to inspect for a valid method implementations.</param>
        /// <param name="methods">The names of the method.</param>
        /// <returns>True if at least one of the methods is defined as missing (no implementation or marked with <see cref="DisaFrameworkNOP"/>),
        /// False otherwise.</returns>
        public static bool MissingAny(object plugin, string[] methods)
        {
            foreach (var method in methods)
            {
                if (Missing(plugin, method))
                {
                    return true;
                }
            }

            return false;
        }

    }

}

