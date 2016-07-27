using System;

namespace Disa.Framework
{
    public static class ServiceEvents
    {
        public static event EventHandler<Service> SettingsLoaded;
        public static event EventHandler<Service> ManualSettingsNeeded;
        public static event EventHandler<Service> UnRegistered;
        public static event EventHandler<Service> Expired;
        public static event EventHandler<Service> Started;
        public static event EventHandler<Service> ContactsUpdated;

        private static Action<ComposeBubbleGroup, BubbleGroup> _composeFinished;
        private static Action _privacyListUpdated;
        private static Action<Service> _contactsUpdated;

        internal static void RaiseComposeFinished(ComposeBubbleGroup composeGroup, 
            BubbleGroup actualGroup)
        {
            if (_composeFinished != null)
                _composeFinished(composeGroup, actualGroup);
        }

        public static void RegisterComposeFinished(Action<ComposeBubbleGroup, BubbleGroup> update)
        {
            _composeFinished = update;
        }

        public static void RaisePrivacyListUpdated()
        {
            if (_privacyListUpdated != null)
                _privacyListUpdated();
        }

        public static void RegisterPrivacyListUpdated(Action update)
        {
            _privacyListUpdated = update;
        }

        public static void RaiseServiceStarted(Service service)
        {
            if (Started == null)
                return;

            Started(null, service);
        }

        public static void RaiseServiceExpired(Service service)
        {
            if (Expired == null)
                return;

            Expired(null, service);
        }

        public static void RaiseServiceUnRegistered(Service service)
        {
            if (UnRegistered == null)
                return;

            UnRegistered(null, service);
        }

        public static void RaiseServiceManualSettingsNeeded(Service service)
        {
            if (ManualSettingsNeeded == null)
                return;

            ManualSettingsNeeded(null, service);
        }

        public static void RaiseServiceSettingsLoaded(Service service)
        {
            if (SettingsLoaded == null)
                return;

            SettingsLoaded(null, service);
        }

        internal static void RaiseContactsUpdated(Service service)
        {
            if (ContactsUpdated == null)
                return;

            ContactsUpdated(null, service);
        }
    }
}