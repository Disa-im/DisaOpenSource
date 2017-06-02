using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Disa.Framework.Bubbles;

namespace Disa.Framework
{
    public static class ServiceManager
    {
        private class ServiceBinding
        {
            public Service Service { get; private set; }
            public ServiceFlags Flags { get; private set; }

            public ServiceBinding(Service service, ServiceFlags flags)
            {
                Service = service;
                Flags = flags;
            }
        }

        private class ServiceFlags
        {
            public bool Running { get; set; }
            public bool Starting { get; set; }
            public bool ManualSettingsNeeded { get; set; }
            public bool ConnectionFailed { get; set; }
            public bool AuthenticationFailed { get; set; }
            public bool DisconnectionFailed { get; set; }
            public bool DeauthenticationFailed { get; set; }
            public bool Aborted { get; set; }
            public bool AbortedSpecial { get; set; }
            public bool Expired { get; set; }
        }

        private static readonly object RegisteredLock = new object();

        private static List<Service> AllInternal { get; set; }

        private static List<ServiceBinding> ServicesBindings { get; set; }

        internal static void Initialize(List<Service> allServices)
        {
            AllInternal = allServices;
            RegisteredServicesDatabase.RegisterAllRegistered();
            Register(GetUnified());

            SendAnalyticsTotalCounts();
        }

        /// <summary>
        /// Helper function to consolidating sending analytic count events for
        /// plugin total count and plugin active count.
        /// </summary>
        public static void SendAnalyticsTotalCounts()
        {
            // The total number of services that are registered excluding unified
            var pluginTotalCount = RegisteredNoUnified.Count();
            Analytics.RaiseCountEvent(
                Analytics.EventAction.PluginTotalCount,
                Analytics.EventCategory.Plugins,
                Analytics.CustomDimensionIndex.PluginTotalCount,
                pluginTotalCount);

            // The total number of services that are registered excluding unified, 
            // minus the number of services that are paused
            var pluginActiveCount = pluginTotalCount - 
                RegisteredNoUnified.Where(x => GetFlags(x).Aborted == true).Count();
            Analytics.RaiseCountEvent(
                Analytics.EventAction.PluginActiveCount,
                Analytics.EventCategory.Plugins,
                Analytics.CustomDimensionIndex.PluginActiveCount,
                pluginActiveCount);
        }

        static ServiceManager()
        {
            ServicesBindings = new List<ServiceBinding>();
        }

        public static IEnumerable<Service> All
        {
            get
            {
                return AllInternal;
            }
        }

        public static IEnumerable<Service> AllNoUnified
        {
            get
            {
                return AllInternal.Where(x => !(x is UnifiedService));
            }
        }

        private static IEnumerable<Service> BindingQuery(Func<ServiceBinding, bool> predicate)
        {
            return from serviceBinding in ServicesBindings
                   where predicate(serviceBinding)
                   select serviceBinding.Service;
        }

        public static IEnumerable<Service> Registered
        {
            get { return (from serviceBinding in ServicesBindings select serviceBinding.Service); }
        }

        public static IEnumerable<Service> RegisteredNoUnified
        {
            get
            {
                return (from serviceBinding in ServicesBindings
                        where !(serviceBinding.Service is UnifiedService)
                        select serviceBinding.Service);
            }
        }

        public static IEnumerable<Service> Starting
        {
            get { return BindingQuery(binding => binding.Flags.Starting); }
        }

        public static IEnumerable<Service> Running
        {
            get { return BindingQuery(binding => binding.Flags.Running); }
        }

        public static IEnumerable<Service> RunningNoUnified
        {
            get { return Running.Where(x => !(x is UnifiedService)); }
        }

        public static IEnumerable<Service> ManualSettingsNeeded
        {
            get { return BindingQuery(binding => binding.Flags.ManualSettingsNeeded); }
        }

        public static IEnumerable<Service> ConnectionFailed
        {
            get { return BindingQuery(binding => binding.Flags.ConnectionFailed); }
        }

        public static IEnumerable<Service> AuthenticationFailed
        {
            get { return BindingQuery(binding => binding.Flags.AuthenticationFailed); }
        }

        public static IEnumerable<Service> DisconnectionFailed
        {
            get { return BindingQuery(binding => binding.Flags.DisconnectionFailed); }
        }

        public static IEnumerable<Service> DeauthenticationFailed
        {
            get { return BindingQuery(binding => binding.Flags.DeauthenticationFailed); }
        }

        public static IEnumerable<Service> Expired
        {
            get { return BindingQuery(binding => binding.Flags.Expired); }
        }

        public static IEnumerable<Service> Aborted
        {
            get { return BindingQuery(binding => binding.Flags.Aborted); }
        }

        public static IEnumerable<Service> AbortedSpecial
        {
            get { return BindingQuery(binding => binding.Flags.AbortedSpecial); }
        }

        public static IEnumerable<Service> Get(BubbleGroup group)
        {
            var unifiedGroup = @group as UnifiedBubbleGroup;
            return unifiedGroup == null ? new [] { @group.Service } : unifiedGroup.Groups.Select(x => x.Service);
        }

        public static IEnumerable<Service> GetNonRegistered(IEnumerable<Service> services)
        {
            return services.Where(x => !IsRegistered(x));
        }

        public static IEnumerable<Service> GetRegistered(IEnumerable<Service> services)
        {
            return services.Where(IsRegistered);
        }

        public static bool Has(BubbleGroup group, Service service)
        {
            return Get(@group).FirstOrDefault(x => x == service) != null;
        }

        public static Service Get(Type serviceType)
        {
            return AllInternal.FirstOrDefault(service => service.GetType() == serviceType);
        }

        public static Service GetByName(string serviceName)
        {
            return AllInternal.FirstOrDefault(service => service.Information.ServiceName == serviceName);
        }

        public static T Get<T>(string guid)
        {
            var service = Registered.FirstOrDefault(s => s.Guid == guid);

            if (service == null)
                return default(T);

            if (service is T)
            {
                return (T) (object) service;
            }

            return default(T);
        }

        public static Service GetUnified()
        {
            return AllInternal.OfType<UnifiedService>().FirstOrDefault();
        }

        public static class RegisteredServicesDatabase
        {
            private static string GetRegisteredPath(string settingsPath)
            {
                return Path.Combine(settingsPath, "RegisteredServicesList.xml");
            }

            public static void RegisterAllRegistered()
            {
                foreach (var serviceName in FetchAllRegistered())
                {
                    try
                    {
                        var service = AllInternal.FirstOrDefault(s => serviceName == s.Information.ServiceName);
                        if (service == null)
                        {
                            Utils.DebugPrint("LoadAllRegistered: Weird. Service " + serviceName + " does not exist.");
                            continue;
                        }

                        Register(service);
                    }
                    catch (Exception ex)
                    {
                        Utils.DebugPrint(ex.Message);
                    }
                }
            }

            public static void SaveAllRegistered()
            {
                var path = GetRegisteredPath(Platform.GetSettingsPath());

                lock (RegisteredLock)
                {
                    using (var writer = XmlWriter.Create(path))
                    {
                        writer.WriteStartDocument();
                        writer.WriteStartElement("SelectedServices");
                        foreach (var ds in RegisteredNoUnified)
                        {
                            writer.WriteStartElement("Service");
                            writer.WriteString(ds.Information.ServiceName);
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();
                        writer.WriteEndDocument();
                    }
                }
            }

            private static IEnumerable<string> FetchAllRegistered()
            {
                return FetchAllRegistered(Platform.GetSettingsPath());
            }

            public static List<string> FetchAllRegistered(string settingsPath)
            {
                var registered = new List<string>();

                var path = GetRegisteredPath(settingsPath);

                if (!File.Exists(path))
                {
                    Utils.DebugPrint("Registered Services List doesn't exist!");
                    return registered;
                }

                try
                {
                    using (var reader = XmlReader.Create(path))
                    {
                        while (reader.Read())
                        {
                            if (!reader.IsStartElement() || reader.Name != "Service") continue;

                            reader.Read();
                            var service = reader.Value;

                            registered.Add(service);
                        }
                    }
                }
                catch
                {
                    Utils.DebugPrint("Registered services list failed to be loaded in. Nuking file.");
                    File.Delete(path);
                    registered.Clear();
                }

                return registered;
            }

            public static void AddToRegisteredAndSaveAllForImminentRestart(string settingsPath, string additionalService)
            {
                var registeredServices = FetchAllRegistered().Concat(new [] { additionalService }).Distinct().ToList();
                var path = GetRegisteredPath(settingsPath);

                lock (RegisteredLock)
                {
                    using (var writer = XmlWriter.Create(path))
                    {
                        writer.WriteStartDocument();
                        writer.WriteStartElement("SelectedServices");

                        foreach (var registeredService in registeredServices)
                        {
                            writer.WriteStartElement("Service");
                            writer.WriteString(registeredService);
                            writer.WriteEndElement();
                        }

                        writer.WriteEndElement();
                        writer.WriteEndDocument();
                    }
                }
            }
        }

        public static bool IsRegistered(Service service)
        {
            return Registered.FirstOrDefault(s => s == service) != null;
        }

        public static bool IsStarting(Service service)
        {
            return Starting.FirstOrDefault(s => s == service) != null;
        }

        public static bool IsRunning(Service service)
        {
            return Running.FirstOrDefault(s => s == service) != null;
        }

        public static bool IsExpired(Service service)
        {
            return Expired.FirstOrDefault(s => s == service) != null;
        }

        public static bool IsAborted(Service service)
        {
            return Aborted.FirstOrDefault(s => s == service) != null;
        }

        public static bool IsAbortedSpecial(Service service)
        {
            return AbortedSpecial.FirstOrDefault(s => s == service) != null;
        }

        public static bool IsManualSettingsNeeded(Service service)
        {
            return ManualSettingsNeeded.FirstOrDefault(s => s == service) != null;
        }

        public static bool IsConnectionFailed(Service service)
        {
            return ConnectionFailed.FirstOrDefault(s => s == service) != null;
        }

        public static bool IsAuthenticationFailed(Service service)
        {
            return AuthenticationFailed.FirstOrDefault(s => s == service) != null;
        }

        public static bool IsDisconnectionFailed(Service service)
        {
            return DisconnectionFailed.FirstOrDefault(s => s == service) != null;
        }

        public static bool IsDeauthenticationFailed(Service service)
        {
            return DeauthenticationFailed.FirstOrDefault(s => s == service) != null;
        }

        internal static void OnBubbleReceived(Bubble b)
        {
            var visualBubble = b as VisualBubble;
            if (visualBubble != null)
            {
                try
                {
                    BubbleManager.Group(visualBubble);
                }
                catch (Exception ex)
                {
                    Utils.DebugPrint("Problem in OnBubbleReceived (VisualBubble) from service " +
                                     visualBubble.Service.Information.ServiceName + ": " + ex.Message);
                }

                if (visualBubble.Direction == Bubble.BubbleDirection.Incoming && 
                    IsRunning(visualBubble.Service) &&
                    BubbleQueueManager.HasQueuedBubbles(b.Service.Information.ServiceName, true, true))
                {
                    Utils.DebugPrint("Sending queued bubbles as we're getting some received.");
                    BubbleQueueManager.Send(new [] { b.Service.Information.ServiceName });
                }
            }
            else if (b is AbstractBubble)
            {
                var skipEvent = false;

                Utils.DebugPrint("We got an abstract bubble: " + b.GetType().Name + " Address: " + b.Address + " ParticipantAddress: " + b.ParticipantAddress);

                BubbleGroup group = null;

                var deliveredBubble = b as DeliveredBubble;
                if (deliveredBubble != null)
                {
                    @group = BubbleGroupManager.FindWithAddress(deliveredBubble.Service, deliveredBubble.Address);

                    if (@group != null)
                    {
                        BubbleGroupFactory.LoadFullyIfNeeded(@group);

                        var bubbles = @group.Bubbles;

                        for (var i = bubbles.Count - 1; i >= 0; i--)
                        {
                            var bubble = bubbles[i];

                            if (bubble.ID != deliveredBubble.VisualBubbleID) continue;

                            BubbleManager.UpdateStatus(bubble, Bubble.BubbleStatus.Delivered, @group);

                            if (@group.Service.Information.DoesSupport(typeof(DeliveredBubbleReceipt)))
                            {
                                BubbleManager.Send(new DeliveredBubbleReceipt(Time.GetNowUnixTimestamp(), Bubble.BubbleDirection.Outgoing, 
                                    @group.Service, bubble));
                            }

                            break;
                        }
                    }
                }

                var readBubble = b as ReadBubble;
                if (readBubble != null)
                {
                    @group = BubbleGroupManager.FindWithAddress(readBubble.Service, readBubble.Address);
                    if (@group != null)
                    {
                        BubbleGroupFactory.LoadFullyIfNeeded(@group);

                        if (@group.ReadTimes == null || !@group.IsParty)
                        {
                            @group.ReadTimes = new []
                            { 
                                new DisaReadTime
                                {
                                    ParticipantAddress = readBubble.ParticipantAddress,
                                    Time = readBubble.ReadTime
                                } 
                            };
                        }
                        else
                        {
                            var readTimes = @group.ReadTimes.ToList();

                            var duplicateReadTime = readTimes.FirstOrDefault(x => 
                                @group.Service.BubbleGroupComparer(x.ParticipantAddress, readBubble.ParticipantAddress));
                            if (duplicateReadTime != null)
                            {
                                readTimes.Remove(duplicateReadTime);
                            }

                            readTimes.Add(new DisaReadTime
                            {
                                ParticipantAddress = readBubble.ParticipantAddress,
                                Time = readBubble.ReadTime
                            });

                            @group.ReadTimes = readTimes.ToArray();
                        }
                    }  
                }

                var prescenceBubble = b as PresenceBubble;
                if (prescenceBubble != null)
                {
                    @group = BubbleGroupManager.Find(bubbleGroup =>
                        !bubbleGroup.IsParty &&
                        bubbleGroup.Service ==
                        prescenceBubble.Service &&
                        prescenceBubble.Service.BubbleGroupComparer(
                            bubbleGroup.Address,
                            prescenceBubble.Address));

                    if (@group != null)
                    {
                        if (group.Presence == prescenceBubble.Available)
                        {
                            skipEvent = true;
                        }
                        else
                        {
                            if (!prescenceBubble.Available)
                            {
                                @group.PresenceType = PresenceBubble.PresenceType.Unavailable;
                            }
                            else
                            {
                                @group.PresenceType = prescenceBubble.Presence;
                                @group.PresencePlatformType = prescenceBubble.Platform;
                            }

                            if (!prescenceBubble.Available)
                            {
                                group.SendBubbleActions.Clear();
                            }
                        }
                    }
                }

                var typingBubble = b as TypingBubble;
                if (typingBubble != null)
                {
                    @group = BubbleGroupManager.Find(bubbleGroup =>
                        bubbleGroup.Service ==
                        typingBubble.Service &&
                        typingBubble.Service.BubbleGroupComparer(
                            bubbleGroup.Address,
                            typingBubble.Address)); 

                    if (@group != null)
                    {
                        if (!group.IsParty)
                        {
                            group.SendBubbleActions.Clear();
                            if (group.Presence)
                            {
                                group.SendBubbleActions.Add(new SendBubbleAction
                                {
                                    Type = typingBubble.Typing ? (typingBubble.IsAudio ? 
                                        SendBubbleAction.ActionType.Recording : SendBubbleAction.ActionType.Typing) : 
                                        SendBubbleAction.ActionType.Nothing,
                                    Address = group.Address,
                                });
                            }
                        }
                        else
                        {
                            var sendBubbleAction = new SendBubbleAction
                            {
                                Address = typingBubble.ParticipantAddress,
                                Type = typingBubble.Typing ? (typingBubble.IsAudio ? 
                                    SendBubbleAction.ActionType.Recording : SendBubbleAction.ActionType.Typing) : 
                                    SendBubbleAction.ActionType.Nothing,
                            };
                            var skipAdd = false;
                            foreach (var item in group.SendBubbleActions)
                            {
                                if (group.Service.BubbleGroupComparer(item.Address, sendBubbleAction.Address))
                                {
                                    if (sendBubbleAction.Type == item.Type)
                                    {
                                       skipAdd = true;
                                    }
                                    else
                                    {
                                        group.SendBubbleActions.Remove(item);
                                    }
                                    break;
                                }
                            }
                            if (!skipAdd && sendBubbleAction.Type != SendBubbleAction.ActionType.Nothing)
                            {
                                group.SendBubbleActions.Add(sendBubbleAction);
                            }
                        }
                    }
                }

                try
                {
                    if (@group != null && !skipEvent)
                    {
                        BubbleGroupEvents.RaiseNewAbstractBubble(b as AbstractBubble, @group);
                    }
                }
                catch (Exception ex)
                {
                    Utils.DebugPrint("Problem in OnBubbleReceived (AbstractBubble) from service " +
                                     b.Service.Information.ServiceName + ": " + ex.Message);
                }
            }
        }

        private static ServiceFlags GetFlags(Service service)
        {
            return (from servicesBindings in ServicesBindings
                where service == servicesBindings.Service
                select servicesBindings.Flags).FirstOrDefault();
        }

        public static void Register(Service service)
        {
            if (IsRegistered(service))
            {
                throw new ServiceSchedulerException("Service " + service.Information.ServiceName + " already registered!");
            }

            lock (ServicesBindings) ServicesBindings.Add(new ServiceBinding(service, new ServiceFlags()));
        }

        public static void Unregister(Service service)
        {
            if (!IsRegistered(service))
            {
                throw new ServiceSchedulerException("Service " + service.Information.ServiceName + "  is not registered!");
            }

            lock (ServicesBindings) ServicesBindings.Remove(ServicesBindings.FirstOrDefault(s => s.Service == service));
            ServiceEvents.RaiseServiceUnRegistered(service);
            SettingsChangedManager.SetNeedsContactSync(service, true);

            Analytics.RaiseServiceEvent(Analytics.EventAction.PluginUnregistered, Analytics.EventCategory.Plugins, service);
        }

        public static void StartUnified(UnifiedService unifiedService, WakeLock wakeLock)
        {
            StartInternal(unifiedService, wakeLock);
        }

        private static void StartInternal(Service registeredService, WakeLock wakeLock)
        {
            if (IsManualSettingsNeeded(registeredService))
            {
                throw new ServiceSchedulerException("Service " + registeredService.Information.ServiceName + " needs manual input for settings.");
            }

            if (!IsRegistered(registeredService))
            {
                throw new ServiceSchedulerException("Could not locate service "
                                                    + registeredService.Information.ServiceName + ". Are you sure you registered it?");
            }

            if (IsRunning(registeredService))
            {
                throw new ServiceSchedulerException(registeredService.Information.ServiceName
                                                    + " service is already running.");
            }

            ClearFailures(registeredService);

            ServiceEvents.RaiseServiceSettingsLoaded(registeredService);

            Action connect = () =>
            {

                try
                {
                    registeredService.Connect(wakeLock);
                }
                catch (NotImplementedException)
                {
                }
                catch (ServiceSpecialRestartException)
                {
                    throw;
                }
                catch (ServiceExpiredException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    GetFlags(registeredService).ConnectionFailed = true;
                    throw new Exception("Service " + registeredService.Information.ServiceName
                                        + " flat out failed to connect. Problem: " + ex);
                }
            };

            Action authenticate = () =>
            {
                try
                {
                    var authSuccess = registeredService.Authenticate(wakeLock);

                    if (!authSuccess)
                    {
                        throw new Exception("Failed authentication "
                                            + registeredService.Information.ServiceName + ".");
                    }
                }
                catch (NotImplementedException)
                {
                }
                catch (ServiceSpecialRestartException)
                {
                    throw;
                }
                catch (ServiceExpiredException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    GetFlags(registeredService).AuthenticationFailed = true;
                    throw new Exception("Service " + registeredService.Information.ServiceName
                                        + " flat out failed to authenticate. Problem: " + ex);
                }
            };

            if (registeredService.Information.Procedure 
                == ServiceInfo.ProcedureType.AuthenticateConnect)
            {
                authenticate();
                connect();
            }
            else if (registeredService.Information.Procedure 
                     == ServiceInfo.ProcedureType.ConnectAuthenticate)
            {
                connect();
                authenticate();
            }

            GetFlags(registeredService).Running = true;
            ServiceEvents.RaiseServiceStarted(registeredService);
        }

        private static void StopInternal(Service registeredService, bool clearFailures = true)
        {
            if (!IsRegistered(registeredService))
            {
                throw new ServiceSchedulerException("Could not locate service "
                                                    + registeredService.Information.ServiceName + ". Are you sure you registered it?");
            }

            if (clearFailures)
                ClearFailures(registeredService);

            Action deAuthenticate = () =>
            {
                try
                {
                    registeredService.Deauthenticate();
                }
                catch (NotImplementedException)
                {
                }
                catch (Exception ex)
                {
                    GetFlags(registeredService).DeauthenticationFailed = true;
                    Utils.DebugPrint(registeredService.Information.ServiceName +
                                     " service failed to deauthenticate. "
                                     + "This may lead to a memory leak. Problem: " + ex);
                }
            };

            Action disconnect = () =>
            {
                try
                {
                    registeredService.Disconnect();
                }
                catch (NotImplementedException)
                {
                }
                catch (Exception ex)
                {
                    GetFlags(registeredService).DisconnectionFailed = true;
                    Utils.DebugPrint(registeredService.Information.ServiceName +
                                     " service failed to disconnect. "
                                     + "This may lead to a memory leak. Problem: " + ex);
                }
            };

            if (registeredService.Information.Procedure 
                == ServiceInfo.ProcedureType.AuthenticateConnect)
            {
                deAuthenticate();
                disconnect();
            }
            else if (registeredService.Information.Procedure 
                     == ServiceInfo.ProcedureType.ConnectAuthenticate)
            {
                disconnect();
                deAuthenticate();
            }

            //there may be exceptions, but we have to remove the service.
            GetFlags(registeredService).Running = false;

            foreach (
                var group in
                    BubbleGroupManager.FindAll(
                        x => x.Service == registeredService && !(x is UnifiedBubbleGroup)))
            {
                @group.SendBubbleActions.Clear();
                if (!group.IsParty)
                {
                    @group.PresenceType = PresenceBubble.PresenceType.Unavailable;
                }
            }
        }

        private static void StartReceiveBubbles(Service registeredService)
        {
            if (!IsRunning(registeredService))
            {
                throw new ServiceSchedulerException(registeredService.Information.ServiceName
                                                    + " service is not running.");
            }

            if (!IsRegistered(registeredService))
            {
                throw new ServiceSchedulerException("Could not locate service "
                                                    + registeredService.Information.ServiceName + ". Are you sure you registered it?");
            }

            if (registeredService.Information.EventDrivenBubbles)
            {
                return;
            }

            while (IsRunning(registeredService))
            {
                try
                {
                    foreach (var b in registeredService.ProcessBubbles())
                    {
                        OnBubbleReceived(b);
                    }
                }
                catch (NotImplementedException) { }
                catch (ThreadAbortException)
                {
                    Utils.DebugPrint("Abort thread excepton in receiving bubbles on service (inner thread) " +
                                     registeredService.Information.ServiceName);
                    break;
                }
                catch (ServiceRestartException ex)
                {
                    RequestRestart(registeredService, ex);
                    break;
                }
                catch (ServiceWarningException ex)
                {
                    Utils.DebugPrint("Warning from service " + registeredService.Information.ServiceName +
                                     ". Reason: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Utils.DebugPrint("Unknown exception from service "
                                     + registeredService.Information.ServiceName + ". Reason: " + ex.Message + " " + ex.StackTrace);

                    RequestRestart(registeredService, ex);
                    break;
                }
            }
        }

        private static void RequestRestart(Service registeredService, Exception ex)
        {
            if (IsAborted(registeredService))
            {
                return;
            }

            Utils.DebugPrint("Telling the ServiceSchedulerManager that the service " +
                             registeredService.Information.ServiceName + " needs a restart. Reason: " +
                             ex.Message);
            OnServiceNeedsRestart(registeredService);
        }

        private static void ClearFailures(Service registeredService)
        {
            var flags = GetFlags(registeredService);

            flags.ConnectionFailed = false;
            flags.AuthenticationFailed = false;
            flags.DeauthenticationFailed = false;
            flags.DisconnectionFailed = false;
        }

        internal static void OnServiceNeedsRestart(Service service)
        {
            StopInternal(service);
            Start(service, true).Wait();
        }

        public static Task Start(Service service, bool smartStart = false, int smartStartSeconds = 10)
        {
            return Task.Factory.StartNew(() =>
            {
                using (var wakeLock = Platform.AquireWakeLock("DisaStart"))
                {
                    if (IsRunning(service))
                    {
                        Utils.DebugPrint(
                            "The service is already running. Preventing possible deadlock. SmartStart? " +
                            smartStart);
                        return;
                    }

                    if (IsStarting(service))
                    {
                        Utils.DebugPrint(
                            "The service is being started. Preventing possible deadlock. SmartStart? " +
                            smartStart);
                        return;
                    }

                    Action epilogue = () => { GetFlags(service).Starting = false; };

                    lock (service)
                    {
                        GetFlags(service).Aborted = false;
                        GetFlags(service).AbortedSpecial = false;
                        GetFlags(service).Starting = true;
                        GetFlags(service).ManualSettingsNeeded = false;

                        Utils.DebugPrint("Loading settings for service " + service.Information.ServiceName);
                        try
                        {
                            var settings = SettingsManager.Load(service);
                            if (settings == null)
                            {
                                Utils.DebugPrint("Failed to load saved settings for "
                                                 + service.Information.ServiceName +
                                                 ". Will try to initialize with no settings...");
                                if (!service.InitializeDefault())
                                {
                                    Utils.DebugPrint(
                                        "Service doesn't allow initializing without settings. Needs manual input.");
                                    GetFlags(service).ManualSettingsNeeded = true;
                                    ServiceEvents.RaiseServiceManualSettingsNeeded(service);
                                }
                                else
                                {
                                    Utils.DebugPrint("Service initialized under no settings.");
                                }
                            }
                            else
                            {
                                Utils.DebugPrint("Loading saved settings! Initializing...");
                                if (service.Initialize(settings))
                                {
                                    Utils.DebugPrint("Successfully initialized service!");
                                }
                                else
                                {
                                    Utils.DebugPrint("Failed to initialize service. Needs manual input.");
                                    GetFlags(service).ManualSettingsNeeded = true;
                                    ServiceEvents.RaiseServiceManualSettingsNeeded(service);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Utils.DebugPrint("Failed: " + ex);
                            epilogue();
                            return;
                        }

                        Utils.DebugPrint("Starting service " + service.Information.ServiceName);

                        try
                        {
                            if (service.Information.UsesInternet 
                                && !Platform.HasInternetConnection())
                            {
                                throw new Exception("No internet connection. Cannot connect service: "
                                                    + service.Information.ServiceName);
                            }

                            if (service.Information.UsesInternet && 
                                !Platform.ShouldAttemptInternetConnection())
                            {
                                throw new Exception("We shouldn't attempt to connect service: "
                                    + service.Information.ServiceName);
                            }

                            StartInternal(service, wakeLock);
                        }
                        catch (ServiceSchedulerException ex)
                        {
                            Utils.DebugPrint("Problem in scheduler: " + ex.Message);
                            epilogue();
                            return;
                        }
                        catch (ServiceSpecialRestartException ex)
                        {
                            Utils.DebugPrint("Service " + service.Information.ServiceName +
                                             " is asking to be restarted on connect/authenticate. This should be called sparingly, Disa can easily " +
                                "break under these circumstances. Reason: " + ex + ". Restarting...");
                            StopInternal(service);
                            epilogue();
                            Start(service, smartStart, smartStartSeconds);
                            return;
                        }
                        catch (ServiceExpiredException ex)
                        {
                            Utils.DebugPrint("The service " + service.Information.ServiceName +
                                             " has expired: " + ex);
                            GetFlags(service).Aborted = true;
                            GetFlags(service).Expired = true;
                            ServiceEvents.RaiseServiceExpired(service);
                            StopInternal(service);
                            epilogue();
                            return;
                        }
                        catch (Exception ex)
                        {
                            if (smartStart)
                            {
                                StopInternal(service, false);

                                if (smartStartSeconds > 600)
                                {
                                    Utils.DebugPrint("Service " + service.Information.ServiceName +
                                                     " needs to wait over 10minutes to be restarted." +
                                                     " Killing SmartStart. The service will not be restarted. Reason: " + ex);
                                    epilogue();
                                    return;
                                }

                                Utils.DebugPrint("Service " + service.Information.ServiceName +
                                                 " failed to be started. SmartStart enabled. "
                                                 + "Service being scheduled to be re-started in T-" +
                                                 smartStartSeconds + " seconds! Reason: " + ex);

                                var hasSmartStart = new object();
                                service.HasSmartStart = hasSmartStart;

                                Platform.ScheduleAction(smartStartSeconds,
                                    new WakeLockBalancer.ActionObject(() =>
                                    {
                                        if (IsAborted(service))
                                        {
                                            Utils.DebugPrint(
                                                "Service " +
                                                service.Information
                                                    .ServiceName +
                                                " tried to be started, but it deemed killed.");
                                            return;
                                        }

                                        if (service.HasSmartStart !=
                                            hasSmartStart)
                                        {
                                            Utils.DebugPrint(
                                                "This smart start has been invalidated. There " +
                                                "seems to be another one on the block.");
                                            return;
                                        }

                                        Utils.DebugPrint(
                                            "Smart start is firing the service " +
                                            service.Information
                                                .ServiceName +
                                            " up again!");

                                        StopInternal(service);
                                        Start(service, true, smartStartSeconds*2);
                                    }, WakeLockBalancer.ActionObject.ExecuteType.TaskWithWakeLock));

                                epilogue();
                                return;
                            }

                            Utils.DebugPrint("Failed to start service " + service.Information.ServiceName +
                                             " (No SmartStart) : " + ex);
                            StopInternal(service, false);
                            epilogue();
                            return;
                        }

                        BubbleManager.SendSubscribe(service, true);
                        BubbleManager.SendLastPresence(service);

                        service.ReceivingBubblesThread = new Thread(() =>
                        {
                            try
                            {
                                StartReceiveBubbles(service);
                            }
                            catch (ThreadAbortException)
                            {
                                Utils.DebugPrint(
                                    "Abort thread excepton in receiving bubbles on service (outer thread) " +
                                    service.Information.ServiceName);
                            }
                            catch (Exception ex)
                            {
                                Utils.DebugPrint(">>>>>>>>> " + ex.Message + " " + ex.StackTrace);
                            }
                            Utils.DebugPrint("Receiving bubbles for service " +
                                             service.Information.ServiceName + " has come to an end.");
                        });
                        service.ReceivingBubblesThread.Start();

                        GetFlags(service).Starting = false;

                        BubbleQueueManager.SetNotQueuedToFailures(service);

                        Utils.Delay(1000).ContinueWith(x =>
                        {
                            BubbleGroupSync.ResetSyncsIfHasAgent(service);
                            BubbleGroupUpdater.Update(service);
                            BubbleQueueManager.Send(new[] {service.Information.ServiceName});
                            BubbleGroupManager.ProcessUpdateLastOnlineQueue(service);
                            SettingsChangedManager.SyncContactsIfNeeded(service);
                        });
                    }
                }
            });
        }

        public static Task AbortAndRestart(Service service)
        {
            return Task.Factory.StartNew(() =>
            {
                Abort(service).Wait();
                Start(service, true).Wait();
            });
        }

        public static Task AbortSpecial(Service service)
        {
            GetFlags(service).AbortedSpecial = true;
            return Abort(service);
        }

        public static Task AbortNoMercy(Service service)
        {
            return Task.Factory.StartNew(() =>
            {
                using (Platform.AquireWakeLock("DisaAbortNoMercy"))
                {
                    lock (service)
                    {
                        GetFlags(service).Aborted = true;

                        try
                        {
                            StopInternal(service);
                        }
                        catch (Exception ex)
                        {
                            Utils.DebugPrint(ex.Message);
                        }
                    }
                }
            });
        }

        public static Task Abort(Service service)
        {
            return Task.Factory.StartNew(() =>
            {
                using (Platform.AquireWakeLock("DisaAbort"))
                {
                    if (!IsRunning(service))
                    {
                        Utils.DebugPrint(
                            "The service is already stopped. Something external must of started it.");
                        return;
                    }

                    lock (service)
                    {
                        GetFlags(service).Aborted = true;

                        try
                        {
                            StopInternal(service);
                            Analytics.RaiseServiceEvent(Analytics.EventAction.PluginPaused, Analytics.EventCategory.Plugins, service);
                        }
                        catch (Exception ex)
                        {
                            Utils.DebugPrint(ex.Message);
                        }
                    }
                }
            });
        }

        public static void Restart(Service service)
        {
            OnServiceNeedsRestart(service);
        }
    }
}