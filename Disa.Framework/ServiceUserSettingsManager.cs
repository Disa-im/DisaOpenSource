using System;
using System.IO;
using System.Xml.Serialization;

namespace Disa.Framework
{
    public class ServiceUserSettingsManager
    {
        private static string GetBaseLocation()
        {
            var databasePath = Platform.GetDatabasePath();
            if (!Directory.Exists(databasePath))
            {
                Utils.DebugPrint("Creating database directory.");
                Directory.CreateDirectory(databasePath);
            }

            var bubbleGroupsSettingsBasePath = Path.Combine(databasePath, "serviceusersettings");
            if (!Directory.Exists(bubbleGroupsSettingsBasePath))
            {
                Utils.DebugPrint("Creating bubble service user settings base directory.");
                Directory.CreateDirectory(bubbleGroupsSettingsBasePath);
            }

            return bubbleGroupsSettingsBasePath;
        }

        private static string GetPath(Service service)
        {
            return Path.Combine(GetBaseLocation(), service.Information.ServiceName + ".xml");
        }

        public static void Save(Service service, DisaServiceUserSettings userSettings)
        {
            service.UserSettings = userSettings;
            var path = GetPath(service);
            using (var sw = new StreamWriter(path))
            {
                try
                {
                    var serializerObj = new XmlSerializer(userSettings.GetType());
                    serializerObj.Serialize(sw, userSettings);
                }
                catch (Exception ex)
                {
                    Utils.DebugPrint("Failed to save service user settings for " + userSettings.GetType().Name 
                        + ": " + ex.Message);
                }
            }
        }

        public static void LoadAll()
        {
            foreach (var service in ServiceManager.AllNoUnified)
            {
                var userSettings = Load(service);
                service.UserSettings = userSettings ?? DisaServiceUserSettings.Default;
            }
        }

        private static DisaServiceUserSettings Load(Service service)
        {
            var path = GetPath(service);
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                using (var sr = new StreamReader(path))
                {
                    var serializer = new XmlSerializer(typeof(DisaServiceUserSettings));
                    return (DisaServiceUserSettings) serializer.Deserialize(sr);
                }
            }
            catch (Exception ex)
            {
                Utils.DebugPrint("Failed to load service user settings for " + service.Information.ServiceName +
                    ". Corruption. Nuking... " + ex.Message);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }

            return null;
        }
    }
}

