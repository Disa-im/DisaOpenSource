namespace Disa.Framework
{
    /// <summary>
    /// A centralized collection of supporting elements for our Google Analytics implementation.
    /// </summary>
    public static class Analytics
    {
        /// <summary>
        /// This enum provides type-safe representations for Google Analytics Screen Names.
        /// 
        /// IMPORTANT: Some of these Screen Names require a <see cref="Service"/> to be assocated with them as well. 
        /// Comments for an enum representing a Screen Name will indicate when this is necessary.
        /// </summary>
        public enum ScreenName
        {

            // Conversation screens
            Conversations,
            /// <summary>
            /// IMPORTANT: Requires associating a <see cref="Service"/> with this Screen Name
            /// when calling <see cref="Analytics.SendScreenName(ScreenName, Service)"/>.
            /// </summary>
            Conversation,

            // Conversation options screens
            /// <summary>
            /// IMPORTANT: Requires associating a <see cref="Service"/> with this Screen Name
            /// when calling <see cref="Analytics.SendScreenName(ScreenName, Service)"/>.
            /// </summary>
            PartyOptions,
            /// <summary>
            /// IMPORTANT: Requires associating a <see cref="Service"/> with this Screen Name
            /// when calling <see cref="Analytics.SendScreenName(ScreenName, Service)"/>.
            /// </summary>
            PartyOptionsSettings,
            /// <summary>
            /// IMPORTANT: Requires associating a <see cref="Service"/> with this Screen Name
            /// when calling <see cref="Analytics.SendScreenName(ScreenName, Service)"/>.
            /// </summary>
            SoloOptions,
            /// <summary>
            /// IMPORTANT: Requires associating a <see cref="Service"/> with this Screen Name
            /// when calling <see cref="Analytics.SendScreenName(ScreenName, Service)"/>.
            /// </summary>
            ExportConversation,

            // Settings screens
            SettingsServices,
            SettingsPluginManager,
            /// <summary>
            /// IMPORTANT: Requires associating a <see cref="Service"/> with this Screen Name
            /// when calling <see cref="Analytics.SendScreenName(ScreenName, Service)"/>.
            /// </summary>
            SettingsServicesService,
            /// <summary>
            /// IMPORTANT: Requires associating a <see cref="Service"/> with this Screen Name
            /// when calling <see cref="Analytics.SendScreenName(ScreenName, Service)"/>.
            /// </summary>
            SettingsServicesPersonalize,
            /// <summary>
            /// IMPORTANT: Requires associating a <see cref="Service"/> with this Screen Name
            /// when calling <see cref="Analytics.SendScreenName(ScreenName, Service)"/>.
            /// </summary>
            SettingsServicesPersonalizeColor,
            SettingsNotifications,
            SettingsGeneral,
            SettingsConversations,
            SettingsInformation,
            SettingsBackupAndRestore,

            // New Message screens
            /// <summary>
            /// IMPORTANT: Requires associating a <see cref="Service"/> with this Screen Name
            /// when calling <see cref="Analytics.SendScreenName(ScreenName, Service)"/>.
            /// </summary>
            NewMessage,
            /// <summary>
            /// IMPORTANT: Requires associating a <see cref="Service"/> with this Screen Name
            /// when calling <see cref="Analytics.SendScreenName(ScreenName, Service)"/>.
            /// </summary>
            NewChannel
        }

        /// <summary>
        /// This enum provides type-safe representations for Google Analytics Event Actions.
        /// </summary>
        public enum EventAction
        {
            PluginInstalled,
            PluginUninstalled,
            PluginRegistered,
            PluginUnregistered,
            PluginSetup,
            PluginUnsetup,
            PluginPaused,
            PluginReactivated,
            PluginPersonalized,
            PluginTotalCount,
            PluginActiveCount,
            MessageSent,
            MessageReceived
        }

        /// <summary>
        /// This enum provides type-safe representations for Google Analytics Event Categories.
        /// </summary>
        public enum EventCategory
        {
            Plugins,
            Messaging
        }

        /// <summary>
        /// This enum provides type-safe representations for Google Analytics Custom Dimension Indexes.
        /// </summary>
        public enum CustomDimensionIndex
        {
            PluginName,
            PluginTotalCount,
            PluginActiveCount
        }

        public delegate void ScreenNameHandler(ScreenName screenName);
        public delegate void ScreenNameWithBubbleGroupHandler(ScreenName screenName, BubbleGroup bubbleGroup);
        public delegate void ScreenNameWithServiceHandler(ScreenName screenName, Service service);
        public delegate void ServiceEventHandler(EventAction eventAction, EventCategory eventCategory, Service service);
        public delegate void CountEventHandler(EventAction eventAction, EventCategory eventCategory, CustomDimensionIndex customDimensionIndex, int count);

        public static event ServiceEventHandler ServiceEvent;
        public static event CountEventHandler CountEvent;

        internal static void RaiseServiceEvent(
            EventAction eventAction,
            EventCategory eventCategory,
            Service service)
        {
            ServiceEvent?.Invoke(eventAction, eventCategory, service);
        }

        internal static void RaiseCountEvent(
            EventAction eventAction,
            EventCategory eventCategory,
            CustomDimensionIndex customDimensionIndex,
            int count)
        {
            CountEvent?.Invoke(eventAction, eventCategory, customDimensionIndex, count);
        }
    }
}