namespace Disa.Framework
{
    /// <summary>
    /// A centralized collection of official meta-data and other supporting elements for our App Center Analytics implementation.
    /// 
    /// IMPORTANT: The actual submission of analytics events is done in the Disa Android client. Disa.Framework will actually publish
    /// analytics events to the Disa Android client via C# events (see below).
    /// </summary>
    public static class Analytics
    {
        /// <summary>
        /// This enum provides type-safe representations for App Center Analytics Screen Names.
        /// 
        /// This note does not apply to App Center yet as App Center does not support publishing screen names with parameters.
        /// IMPORTANT: Some of these Screen Names require a <see cref="Service"/> to be assocated with them as well. 
        /// Comments for an enum representing a Screen Name requiring a <see cref="Service"/> to be associated 
        /// will indicate when this is necessary.
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
        /// Our defined parameter key for the category of an event when submitting a App Center Analytics event.
        /// </summary>
        public const string EVENT_CATEGORY_KEY = "event_category";

        /// <summary>
        /// Our defined parameter key for the plugin name associated with an event when submitting a App Center Analytics event.
        /// </summary>
        public const string EVENT_PLUGIN_NAME_KEY = "event_plugin_name";

        /// <summary>
        /// Our defined parameter key for the service name associated with an event when submitting a App Center Analytics event.
        /// </summary>
        public const string EVENT_SERVICE_NAME_KEY = "event_service_name";

        /// <summary>
        /// Our defined parameter key for a count associated with an event when submitting a App Center Analytics event.
        /// </summary>
        public const string EVENT_COUNT_KEY = "event_count";

        /// <summary>
        /// This enum provides type-safe representations for App Center Analytics Event Actions.
        /// </summary>
        public enum EventAction
        {
            PluginInstalled,
            PluginUninstalled,
            ServiceRegistered,
            ServiceUnregistered,
            ServiceSetup,
            ServiceUnsetup,
            ServicePaused,
            ServiceReactivated,
            ServicePersonalized,
            ConvoPersonalized,
            ServiceActiveCount,
            MessageSent,
            MessageReceived
        }

        /// <summary>
        /// This enum provides type-safe representations for App Center Analytics Event Categories.
        /// </summary>
        public enum EventCategory
        {
            Plugins,
            Services,
            Messaging
        }

        public delegate void PluginEventHandler(EventAction eventAction, EventCategory eventCategory, string pluginName);
        public delegate void ServiceEventHandler(EventAction eventAction, EventCategory eventCategory, Service service);
        public delegate void CountEventHandler(EventAction eventAction, EventCategory eventCategory, int count);

        /// <summary>
        /// This event allows Disa.Framework to publish <see cref="Service"/> related analytic events to the Disa Android client.
        /// </summary>
        public static event ServiceEventHandler ServiceEvent;

        /// <summary>
        /// This event allows Disa.Framework to publish count related analytic events (e.g., plugin_total_count) 
        /// to the Disa Android client.
        /// </summary>
        public static event CountEventHandler CountEvent;

        /// <summary>
        /// Publish a service related event to the Disa Android client.
        /// 
        /// IMPORTANT: Note that this is internal so that only the Disa.Framework assembly can call this function.
        /// </summary>
        /// <param name="eventAction">The type safe representation of the Event Action.</param>
        /// <param name="eventCategory">The type safe representation of the Event Category.</param>
        /// <param name="service">The <see cref="Service"/> to be associated with this Google Analytics event.</param>
        internal static void RaiseServiceEvent(
            EventAction eventAction,
            EventCategory eventCategory,
            Service service)
        {
            ServiceEvent?.Invoke(eventAction, eventCategory, service);
        }

        /// <summary>
        /// Publish a count related event (e.g., plugin_total_count) to the Disa Android client.
        /// 
        /// IMPORTANT: Note that this is internal so that only the Disa.Framework assembly can call this function.
        /// </summary>
        /// <param name="eventAction">The type safe representation of the Event Action.</param>
        /// <param name="eventCategory">The type safe representation of the Event Category.</param>
        /// <param name="count">The count to be associated with this Google Analytics event.</param>
        internal static void RaiseCountEvent(
            EventAction eventAction,
            EventCategory eventCategory,
            int count)
        {
            CountEvent?.Invoke(eventAction, eventCategory, count);
        }

        /// <summary>
        /// Converts a type safe <see cref="ScreenName"/> enum into a defined string for App Center Analytics.
        /// </summary>
        /// <param name="screenName">The type safe enum we want to convert.</param>
        /// <returns> The defined string for Google Analytics.</returns>
        public static string GetScreenName(ScreenName screenName)
        {
            switch (screenName)
            {
                // Conversation screens
                case ScreenName.Conversations: { return "screen_conversations"; }
                case ScreenName.Conversation: { return "screen_conversation"; }

                // Conversation options screens
                case ScreenName.PartyOptions: { return "screen_party_options"; }
                case ScreenName.PartyOptionsSettings: { return "screen_party_options_settings"; }
                case ScreenName.SoloOptions: { return "screen_solo_options"; }
                case ScreenName.ExportConversation: { return "screen_export_conversation"; }

                // Settings screens
                case ScreenName.SettingsServices: { return "screen_settings_services"; };
                case ScreenName.SettingsPluginManager: { return "screen_settings_plugin_manager"; };
                case ScreenName.SettingsServicesService: { return "screen_settings_services_service"; }
                case ScreenName.SettingsServicesPersonalize: { return "screen_settings_services_personalize"; }
                case ScreenName.SettingsNotifications: { return "screen_settings_notifications"; }
                case ScreenName.SettingsGeneral: { return "screen_settings_general"; }
                case ScreenName.SettingsConversations: { return "screen_settings_conversation"; }
                case ScreenName.SettingsInformation: { return "screen_settings_information"; }
                case ScreenName.SettingsBackupAndRestore: { return "screen_settings_backup_and_restore"; }

                // New Message screens
                case ScreenName.NewMessage: { return "screen_new_message"; }
                case ScreenName.NewChannel: { return "screen_new_channel"; }
            }

            return string.Empty;
        }

        /// <summary>
        /// Converts a type safe <see cref="EventAction"/> into a defined string for App Center Analytics.
        /// </summary>
        /// <param name="eventAction">The type safe enum we want to convert.</param>
        /// <returns>The defined string for Google Analytics.</returns>
        public static string GetEventAction(EventAction eventAction)
        {
            switch (eventAction)
            {
                // Plugins
                case EventAction.PluginInstalled: { return "plugin_installed"; }
                case EventAction.PluginUninstalled: { return "plugin_uninstalled"; }
                case EventAction.ServiceRegistered: { return "service_registered"; }
                case EventAction.ServiceUnregistered: { return "service_unregistered"; }
                case EventAction.ServiceSetup: { return "service_setup"; }
                case EventAction.ServiceUnsetup: { return "service_unsetup"; }
                case EventAction.ServicePaused: { return "service_paused"; }
                case EventAction.ServiceReactivated: { return "service_reactivated"; }
                case EventAction.ServicePersonalized: { return "service_personalized"; }
                case EventAction.ConvoPersonalized: { return "convo_personalized"; }
                case EventAction.ServiceActiveCount: { return "service_active_count"; }

                // Messaging
                case EventAction.MessageSent: { return "message_sent"; }
                case EventAction.MessageReceived: { return "message_received"; }
            }

            return string.Empty;
        }

        /// <summary>
        /// Converts a type safe <see cref="EventCategory"/> enum into a defined string for App Center Analytics.
        /// </summary>
        /// <param name="eventCategory">The type safe enum we want to convert.</param>
        /// <returns>The defined string for Google Analytics.</returns>
        public static string GetEventCategory(EventCategory eventCategory)
        {
            switch (eventCategory)
            {
                case EventCategory.Plugins: { return "plugins"; }
                case EventCategory.Services: { return "services"; }
                case EventCategory.Messaging: { return "messaging"; }
            }

            return string.Empty;
        }
    }
}