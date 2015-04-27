using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Disa.Framework
{
    public class LogsManager
    {
        private static string GetDebugPath()
        {
            return !Platform.Ready
                       ? null
                       : Path.Combine(Platform.GetLogsPath(), "Debug.txt");
        }

		private static readonly object LogLock = new object();
        private static int _writeLineCounter;
        private static readonly List<string> LogCache = new List<string>();
        private const int PrintsBeforeWrite = 50;

        public static void WriteLine(string line)
        {
            lock (LogLock)
            {
                LogCache.Add(DateTime.Now.ToString("F") + " | " + line);

				if (LogCache.Count > PrintsBeforeWrite)
				{
                    var path = GetDebugPath();
                    if (path == null)
                        return;

					if (_writeLineCounter > 1000)
					{
						_writeLineCounter = 0;
						var debugFile = new FileInfo(path);
						if (debugFile.Length > 10485760) //10mb
						{
							Utils.DebugPrintNoLog("Huge log file. Killing.");
							debugFile.Delete();
						}
					}

					using (var fs = File.AppendText(path))
					{
						foreach (var log in LogCache)
						{
							fs.WriteLine(log);
						}
						LogCache.Clear();
					}
				}

                _writeLineCounter++;
            }
        }

        public static Task WriteOutAsync()
        {
            return Task.Factory.StartNew(WriteOut);
        }

        public static void WriteOut()
        {
			if (!Utils.Logging)
				return;

            var path = GetDebugPath();
            if (path == null)
                return;

            lock (LogLock)
            {
				using (var fs = File.AppendText(path))
				{
					foreach (var log in LogCache)
					{
						fs.WriteLine(DateTime.Now.ToString("F") + " | " + log);
					}
					LogCache.Clear();
				}
            }
        }
    }
}