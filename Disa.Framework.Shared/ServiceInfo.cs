using System;
using System.Linq;
using Disa.Framework.Bubbles;
using System.Reflection;

namespace Disa.Framework
{
    [AttributeUsage(AttributeTargets.All)]
    public class ServiceInfo : Attribute
    {
        public enum ProcedureType { ConnectAuthenticate, AuthenticateConnect };

        public string ServiceName { get; private set; }
        public Type[] SupportedBubbles { get; private set; }
        public Type Settings { get; private set; }
        public bool EventDrivenBubbles { get; private set; }
        public bool UsesInternet { get; private set; }
        public bool UsesMediaProgress { get; private set; }
        public bool SendingQuotes { get; private set; }
        public bool SupportsBatterySavingsMode { get; private set; }
        public ProcedureType Procedure { get; private set; }
        public bool DelayedNotifications { get; private set; }

        public ServiceInfo(string serviceName, bool eventDrivenBubbles, bool usesMediaProgress,
            bool usesInternet, bool supportsBatterySavingsMode, bool delayedNotifications, Type settings, ProcedureType procedureType,
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

        public ServiceInfo(string serviceName, bool eventDrivenBubbles, bool usesMediaProgress,
            bool usesInternet, bool supportsBatterySavingsMode, bool delayedNotifications, bool sendingQuotes,
            Type settings, ProcedureType procedureType, params Type[] supportedBubbles)
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

        public bool DoesSupport(Type bubble)
        {
            if (SupportedBubbles == null)
                return false;

            return SupportedBubbles.FirstOrDefault(b => bubble == b) != null;
        }
    }

    [AttributeUsage(AttributeTargets.All)]
    public class QueuedBubblesParameters : Attribute
    {
        public Type[] BubblesNotToQueue { get; private set; }
        public Type[] SendingBubblesToFailOnServiceStart { get; private set; }

        public QueuedBubblesParameters(Type[] bubblesNotToQueue, Type[] sendingBubblesToFailOnServiceStart)
        {
            SendingBubblesToFailOnServiceStart = sendingBubblesToFailOnServiceStart;
            BubblesNotToQueue = bubblesNotToQueue;
        }
    }

    [AttributeUsage(AttributeTargets.All)]
    public class FileParameters : Attribute
    {
        public const int NoSizeLimit = -1;

        public long SizeLimit { get; set; }

        public FileParameters(long sizeLimit)
        {
            SizeLimit = sizeLimit;
        }
    }

    [AttributeUsage(AttributeTargets.All)]
    public class PluginSettingsUI : Attribute
    {
        public Type Service { get; private set; }

        public PluginSettingsUI(Type service)
        {
            Service = service;
        }
    }

    [AttributeUsage(AttributeTargets.Interface)]
    public class DisaFrameworkAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Interface)]
    public class DisaFrameworkDeprecated : Attribute
    {
    }

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
            catch(AmbiguousMatchException ex)
            {
                // The method we are inspecting is overloaded
                var methodInfos = plugin.GetType().GetMethods().Where(m => m.Name.Equals(method));
                foreach(var methodInfo in methodInfos)
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
        public static  bool MissingAny(object plugin, string[] methods)
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

    [AttributeUsage(AttributeTargets.All)]
    public class AudioParameters : Attribute
    {
        public enum RecordType
        {
            M4A,
            _3GP
        };
        
        public const int NoSizeLimit = -1;
        public const int NoDurationLimit = -1;

        public int DurationLimit { get; set; }
        public long SizeLimit { get; set; }
        public string[] SupportedExtensions { get; set; }
        public RecordType AudioRecordType { get; set; }

        public AudioParameters(RecordType recordType, int durationLimit, int sizeLimit, params string[] supportedExtensions)
        {
            DurationLimit = durationLimit;
            SizeLimit = sizeLimit;
            SupportedExtensions = supportedExtensions;
            AudioRecordType = recordType;
        }

        public bool DoesSupportExtension(string extension)
        {
            return SupportedExtensions.FirstOrDefault(x => x.ToLower() == extension.ToLower()) != null;
        }
    }

    [AttributeUsage(AttributeTargets.All)]
    public class VideoParameters : Attribute
    {
        public enum RecordType
        {
            Mp4,
            _3Gp
        };

        public const int NoSizeLimit = -1;
        public const int NoDurationLimit = -1;

        public RecordType VideoRecordType { get; set; }
        public string[] SupportedExtensions { get; set; }
        public int DurationLimit { get; set; }
        public long SizeLimit { get; set; }

        public VideoParameters(RecordType videoRecordType, int durationLimit, 
            long sizeLimit, params string[] supportedExtensions)
        {
            VideoRecordType = videoRecordType;
            DurationLimit = durationLimit;
            SizeLimit = sizeLimit;
            SupportedExtensions = supportedExtensions;
        }

        public bool DoesSupportExtension(string extension)
        {
            return SupportedExtensions.FirstOrDefault(x => x.ToLower() == extension.ToLower()) != null;
        }
    }
}

