using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Linq;
using Disa.Framework.Bubbles;
using System.Reflection;

namespace Disa.Framework
{
    public static class Utils
    {
        public static bool Logging { get; set; }

        static Utils()
        {
            Logging = true;
        }

        public static Task GcCollect()
        {
            return Task.Factory.StartNew(() =>
            {
                try
                {
                    GC.Collect();
#if __ANDROID__
                        
                    Java.Lang.Runtime.GetRuntime().Gc();
#endif

                }
                catch (Exception ex)
                {
                    DebugPrint("Failed to GC collect: " + ex);
                }
            });
        }

        public static int ColorRgbToColorArgbIfPossible(int rgb)
        {
            try
            {
                var rgbString = ColorIntToString(rgb);
                rgbString = rgbString.Remove(0, 1);
                if (rgbString.Length <= 6)
                {
                    rgbString = "FF" + rgbString;
                }
                rgbString = "#" + rgbString;
                #if __ANDROID__
                var argb = global::Android.Graphics.Color.ParseColor(rgbString).ToArgb();
                #else
                var argb = ColorStringToInt(rgbString);
                #endif
                return argb;
            }
            catch
            {
                return rgb;
            }
        }

        public static string ColorIntToString(int argb)
        {
            return "#" + argb.ToString("X4").PadLeft(6, '0');
        }

        public static int ColorStringToInt(string argb)
        {
            return Convert.ToInt32(argb.Remove(0, 1), 16);
        }

        public static bool UrlHasParams(string location)
        {
            return location.Contains("?");
        }

        public static string ConvertToUrlEscapingIllegalCharacters(String @string)
        {
            var uri = new Uri(@string);
            var ascii = uri.AbsoluteUri;
            return ascii;
        }

        public static bool IsUrl(string location)
        {
            var trimmed = location.Trim();
            return trimmed.StartsWith("https://") || trimmed.StartsWith("http://");
        }

        /// <summary>
        /// If logging has been turned on, submits a tagged log entry to both
        /// Console and log file.
        /// 
        /// Typically this is done by setting a const string with a tag string for a section of code
        /// you want to clearly identify in a log file.
        /// 
        /// Example: 
        /// private const string TAG = "[Backup]"
        /// .
        /// DebugPrint(TAG, "Starting backup")
        /// .
        /// 
        /// In log file this will produce:
        /// [Backup] Starting backup
        /// 
        /// </summary>
        /// <param name="tag">The tag you want to prefix the log entry with.</param>
        /// <param name="logEntry">The log entry.</param>
        public static void DebugPrint(string tag, string logEntry)
        {
            var taggedLogEntry = tag + " " + logEntry;

            DebugPrint(taggedLogEntry);
        }

        /// <summary>
        /// If logging has been turned on, submits the log entry to both
        /// Console and log file.
        /// </summary>
        /// <param name="logEntry">The log entry.</param>
        public static void DebugPrint(string logEntry)
        {
			if (Logging)
            {
                Console.WriteLine(logEntry);
                LogsManager.WriteLine(logEntry);
            }
        }

        /// <summary>
        /// If logging has been turned on, submits the log entry to just the Console.
        /// </summary>
        /// <param name="logEntry">The log entry.</param>
        public static void DebugPrintNoLog(string str)
        {
			if (Logging)
            {
                Console.WriteLine(str);
            }
        }

        public static Task Delay(double milliseconds)
        {
            return Task.Delay((int)milliseconds);
        }

        public static string RemoveDiacritics(string stIn)
        {
            string stFormD = stIn.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder();

            for (int ich = 0; ich < stFormD.Length; ich++)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(stFormD[ich]);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(stFormD[ich]);
                }
            }

            return (sb.ToString().Normalize(NormalizationForm.FormC));
        }

        public static T Clone<T>(T source)
        {
            if (!typeof(T).IsSerializable)
            {
                throw new ArgumentException("The type must be serializable.", "source");
            }

            // Don't serialize a null object, simply return the default for that object
            if (ReferenceEquals(source, null))
            {
                return default(T);
            }

            IFormatter formatter = new BinaryFormatter();
            Stream stream = new MemoryStream();
            using (stream)
            {
                formatter.Serialize(stream, source);
                stream.Seek(0, SeekOrigin.Begin);
                return (T)formatter.Deserialize(stream);
            }
        }

        public static IEnumerable<int> Factor(int number)
        {
            var factors = new List<int>();
            var max = (int)Math.Sqrt(number);

            for (var factor = 1; factor <= max; ++factor)
            {
                if (number % factor != 0) continue;

                factors.Add(factor);
                if (factor != number / factor)
                { 
                    factors.Add(number / factor);
                }
            }

            return factors;
        }

        public static bool Search(string haystack, string needle)
        {
            if (string.IsNullOrWhiteSpace(needle))
                return true;

            if (string.IsNullOrWhiteSpace(haystack))
            {
                return false;
            }

            var queryTrimmedAndDiacriticsRemoved = Utils.RemoveDiacritics(needle.Trim());

            var queryIsPartOfTitle = haystack.Split(' ').FirstOrDefault(x => 
                Utils.RemoveDiacritics(x.Trim())
                .StartsWith(queryTrimmedAndDiacriticsRemoved, 
                    StringComparison.CurrentCultureIgnoreCase)) != null;

            if (queryIsPartOfTitle)
                return true;

            var queryIsTitle = Utils.RemoveDiacritics(haystack.Trim())
                .IndexOf(queryTrimmedAndDiacriticsRemoved, StringComparison.CurrentCultureIgnoreCase) > -1;

            if (queryIsTitle)
                return true;

            return false;
        }

        public static IEnumerable<Type> GetAllTypes(Assembly assembly)
        {
            return GetAllTypes(new [] { assembly });
        }

        public static IEnumerable<Type> GetAllTypes(IEnumerable<Assembly> assemblies)
        {
            var types = new List<Type>();
            foreach (var assembly in assemblies)
            {
                Type[] assemblyTypes = null;
                try
                {
                    assemblyTypes = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    Utils.DebugPrint("Failed to load some types in the assembly " + assembly.FullName + ". They are printed below: ");
                    if (ex.LoaderExceptions != null)
                    {
                        foreach (var exception in ex.LoaderExceptions)
                        {
                            Utils.DebugPrint(exception.ToString());
                        }
                    }
                    assemblyTypes = ex.Types;
                }
                catch (Exception ex)
                {
                    Utils.DebugPrint("Failed to load assembly " + assembly.FullName + ": " + ex);
                    assemblyTypes = null;
                }
                if (assemblyTypes != null)
                {
                    foreach (var type in assemblyTypes)
                    {
                        if (type == null)
                        {
                            continue;
                        }
                        types.Add(type);
                    }
                }
            }
            return types;
        }
    }
}