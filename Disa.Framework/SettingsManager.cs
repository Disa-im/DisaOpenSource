using System;
using System.IO;
using System.Xml.Serialization;

namespace Disa.Framework
{
    public static class SettingsManager
    {
        private static readonly object Lock = new object();

        private static string GetPath(Service service)
        {
            return Path.Combine(Platform.GetSettingsPath(), service.Information.ServiceName + ".xml");
        }

        public static void Delete(Service service)
        {
            lock (Lock)
            {
                var path = GetPath(service);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        public static void Save(Service service, DisaSettings settings)
        {
            lock (Lock)
            {
                var path = GetPath(service);
                MemoryStream sw2;
                using (var sw = new MemoryStream())
                {
                    Save(sw, service.Information.Settings, settings);
                    sw2 = sw;
                }
                if (sw2 != null)
                {
                    File.WriteAllBytes(path, sw2.ToArray());
                }
            }
        }

        public static void Save(Stream fs, Type settingsType, DisaSettings settings)
        {
            try
            {
                var serializerObj = new XmlSerializer(settingsType);
                serializerObj.Serialize(fs, settings);
            }
            catch (Exception ex)
            {
                Utils.DebugPrint("Failed to save settings for " + settings.GetType().Name + ": " + ex.Message);
                throw;
            }
        }

        public static DisaSettings Load(string settingsPath, string fileName, Type settings)
        {
            lock (Lock)
            {
                try
                {
                    using (var sr = new StreamReader(Path.Combine(settingsPath, fileName)))
                    {
                        return Load(sr, settings);
                    }
                }
                catch (Exception ex)
                {
                    Utils.DebugPrint("Failed to load: " + ex);
                    return null;
                }
            }
        }

        public static DisaSettings Load(Service ds)
        {
            var path = GetPath(ds);

            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                using (var sr = new StreamReader(path))
                {
                    var loadedObj = Load(sr, ds.Information.Settings);

                    return loadedObj;
                }
            }
            catch (Exception ex)
            {
                Utils.DebugPrint("Failed (exception) to load settings for "
                + ds.Information.ServiceName + ". Nuking!");
                File.Delete(path);
                return null;
            }
        }

        public static DisaSettings Load(StreamReader fs, Type settings)
        {
            var serializer = new XmlSerializer(settings);
            return (DisaSettings)serializer.Deserialize(fs);
        }
    }
}