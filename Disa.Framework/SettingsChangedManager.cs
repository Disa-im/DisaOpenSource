using System;
using System.IO;
using SQLite;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Disa.Framework
{
    public static class SettingsChangedManager
    {
        private static object _lock = new object();

        private static string Location
        {
            get
            {
                return Path.Combine(Platform.GetSettingsPath(), "ContactSync.db");
            }
        }

        private class Entry
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            public string ServiceName { get; set; }
            public bool NeedsContactSync { get; set; }
        }

        public static Task SetNeedContactSyncToAllServices()
        {
            return Task.Factory.StartNew(() =>
            {
                SetNeedsContactSync(ServiceManager.AllNoUnified.ToArray(), true);
            });
        }

        internal static void SetNeedsContactSync(Service[] services, bool value)
        {
            if (services == null)
                return;
            lock (_lock)
            {
                using (var db = new SqlDatabase<Entry>(Location))
                {
                    foreach (var service in services)
                    {
                        if (service == null)
                            continue;
                        bool updated = false;
                        foreach (var entry in db.Store.Where(x => x.ServiceName == 
                            service.Information.ServiceName))
                        {
                            Utils.DebugPrint("NeedsContactSync for service " 
                                + service.Information.ServiceName + " is being updated to " + value);
                            entry.NeedsContactSync = value;
                            db.Update(entry);
                            updated = true;
                            break;
                        }
                        if (!updated)
                        {
                            Utils.DebugPrint("NeedsContactSync for service " 
                                + service.Information.ServiceName + " is being set to " + value);
                            db.Add(new Entry
                            {
                                ServiceName = service.Information.ServiceName,
                                NeedsContactSync = value,
                            });
                        }
                    }
                }
            }
        }

        internal static void SetNeedsContactSync(Service service, bool value)
        {
            SetNeedsContactSync(new [] { service }, value);
        }

        internal static void SyncContactsIfNeeded(Service service)
        {
            var needSync = GetServicesThatNeedContactSync().FirstOrDefault(x => x == service) != null;
            if (!needSync)
            {
                Utils.DebugPrint("Service " + service.Information.ServiceName + " does not need a contact sync!");
                return;
            }
            Utils.DebugPrint("Syncing service contacts " + service.Information.ServiceName + " because it needs a contact sync...");
            try
            {
                PhoneBook.SyncService(service);
            }
            catch (Exception ex)
            {
                Utils.DebugPrint("Failed to sync contacts: " + ex);
                return;
            }
            Utils.DebugPrint("Successfully synced contacts for service " + service.Information.ServiceName);
            SetNeedsContactSync(service, false);
        }

        internal static IEnumerable<Service> GetServicesThatNeedContactSync()
        {
            lock (_lock)
            {
                using (var db = new SqlDatabase<Entry>(Location))
                {
                    foreach (var entry in db.Store)
                    {
                        if (entry.NeedsContactSync)
                        {
                            var service = ServiceManager.AllNoUnified.FirstOrDefault(x => 
                                x.Information.ServiceName == entry.ServiceName);
                            if (service != null)
                            {
                                yield return service;
                            }
                        }
                    }
                }
            }
        }
    }
}

