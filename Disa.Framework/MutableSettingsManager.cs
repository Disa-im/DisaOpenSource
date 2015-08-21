using System.IO;
using System.Xml.Serialization;
using System;

namespace Disa.Framework
{
	public static class MutableSettingsManager
	{
		private static object _lock = new object();

		private static string GetPath(string name)
		{
			return Path.Combine(Platform.GetSettingsPath(), name + ".xml");
		}

        public static void Delete<T>() where T : DisaMutableSettings
        {
            Delete(typeof(T).Name);
        }

        public static void Save<T>(T settings) where T : DisaMutableSettings
        {
            Save(typeof(T).Name, settings);
        }

        public static T Load<T>() where T : DisaMutableSettings
        {
            return Load(typeof(T).Name, typeof(T)) as T;
        }

        private static void Delete(string name)
        {
            lock (_lock)
            {
                var path = GetPath(name);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

		private static DisaMutableSettings Load(string name, Type settings)
		{
			lock (_lock)
			{
				var path = GetPath(name);
                if (File.Exists(path))
				{
					try
					{
                        using (var sr = new StreamReader(path))
						{
							var serializer = new XmlSerializer(settings);
							return serializer.Deserialize(sr) as DisaMutableSettings;
						}
					}
					catch (Exception ex)
					{
						Utils.DebugPrint("Failed to load mutable settings (nuking) " + name + " " + ex);
                        File.Delete(path);
					}
				}
				return Activator.CreateInstance(settings) as DisaMutableSettings;
			}
		}

		private static bool Save(string name, DisaMutableSettings settings)
		{
			lock (_lock)
			{
				var path = GetPath(name);
				try
				{
                    MemoryStream sw2 = null;
                    using (var sw = new MemoryStream())
					{
						var serializer = new XmlSerializer(settings.GetType());
						serializer.Serialize(sw, settings);
                        sw2 = sw;
					}
                    if (sw2 != null)
                    {
                        File.WriteAllBytes(path, sw2.ToArray());
                    }
					return true;
				}
				catch (Exception ex)
				{
					Utils.DebugPrint("Failed to save mutable settings for " + settings.GetType().Name);
				}
				return false;
			}
		}
	}
}