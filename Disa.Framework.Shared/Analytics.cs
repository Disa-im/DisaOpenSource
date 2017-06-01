namespace Disa.Framework
{
    /// <summary>
    /// A centralized collection of official meta-data and other supporting elements for our Google Analytics implementation.
    /// 
    /// IMPORTANT: The actual submission of analytics events is done in the Disa Android client. Disa.Framework will actually publish
    /// analytics events to the Disa Android client via C# events (see below).
    /// </summary>
    public static class Analytics
    {
        /// <summary>
        /// This enum provides type-safe representations for Google Analytics Screen Names.
        /// 
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
        /// <param name="customDimensionIndex">The type safe representation of the Custom Dimension Index.</param>
        /// <param name="count">The count to be associated with this Google Analytics event.</param>
        internal static void RaiseCountEvent(
            EventAction eventAction,
            EventCategory eventCategory,
            CustomDimensionIndex customDimensionIndex,
            int count)
        {
            CountEvent?.Invoke(eventAction, eventCategory, customDimensionIndex, count);
        }

        /// <summary>
        /// Converts a type safe <see cref="ScreenName"/> enum into a defined string for Google Analytics.
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
                case ScreenName.SettingsServicesPersonalizeColor: { return "screen_settings_services_personlize_color"; }
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
        /// Converts a type safe <see cref="EventAction"/> into a defined string for Google Analytics.
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
                case EventAction.PluginRegistered: { return "plugin_registered"; }
                case EventAction.PluginUnregistered: { return "plugin_unregistered"; }
                case EventAction.PluginSetup: { return "plugin_setup"; }
                case EventAction.PluginUnsetup: { return "plugin_unsetup"; }
                case EventAction.PluginPaused: { return "plugin_paused"; }
                case EventAction.PluginReactivated: { return "plugin_reactivated"; }
                case EventAction.PluginPersonalized: { return "plugin_personalized"; }
                case EventAction.PluginTotalCount: { return "plugin_total_count"; }
                case EventAction.PluginActiveCount: { return "plugin_active_count"; }

                // Messaging
                case EventAction.MessageSent: { return "message_sent"; }
                case EventAction.MessageReceived: { return "message_received"; }
            }

            return string.Empty;
        }

        /// <summary>
        /// Converts a type safe <see cref="EventCategory"/> enum into a defined string for Google Analytics.
        /// </summary>
        /// <param name="eventCategory">The type safe enum we want to convert.</param>
        /// <returns>The defined string for Google Analytics.</returns>
        public static string GetEventCategory(EventCategory eventCategory)
        {
            switch (eventCategory)
            {
                case EventCategory.Plugins: { return "plugins"; }
                case EventCategory.Messaging: { return "messaging"; }
            }

            return string.Empty;
        }

        /// <summary>
        /// Converts a type safe <see cref="CustomDimensionIndex"/> into a defined int for Google Analytics.
        /// 
        /// Note:
        /// Although we could define the enum values to match the DimensionIndex, we are using this helper function
        /// to stay consistent with how we determine values for other Google Analytics elements.
        /// </summary>
        /// <param name="customDimensionIndex">The enum we want to convert.</param>
        /// <returns>The defined int for Google Analytics.</returns>
        public static int GetCustomDimensionIndex(CustomDimensionIndex customDimensionIndex)
        {
            switch (customDimensionIndex)
            {
                case CustomDimensionIndex.PluginName: { return 1; }
                case CustomDimensionIndex.PluginTotalCount: { return 2; }
                case CustomDimensionIndex.PluginActiveCount: { return 3; }
            }

            return -1;
        }
    }
}