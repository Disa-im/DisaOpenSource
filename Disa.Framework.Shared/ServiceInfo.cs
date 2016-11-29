using System;
using System.Linq;
using Disa.Framework.Bubbles;

namespace Disa.Framework
{
    [AttributeUsage(AttributeTargets.All)]
    public class ServiceInfo : Attribute
    {
        public enum ProcedureType { ConnectAuthenticate, AuthenticateConnect };

        public string ServiceName {get; private set;}
        public Type[] SupportedBubbles { get; private set; }
        public Type Settings { get; private set; }
        public bool EventDrivenBubbles { get; private set; }
        public bool UsesInternet { get; private set; }
        public bool UsesMediaProgress { get; private set; }
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
    public class PluginFramework : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class PluginFrameworkNOP : Attribute
    {
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

