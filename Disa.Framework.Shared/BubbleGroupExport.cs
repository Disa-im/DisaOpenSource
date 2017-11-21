using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Disa.Framework.Bubbles;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xamarin;

namespace Disa.Framework
{
	public class BubbleGroupExport
	{
		private static readonly object _exportLock = new object();

		public class Options
		{
			internal static long ConvertToUnixTimestamp(Time time)
			{
				const int secondsInHour = 3600;
				const int secondsInDay = 86400;
				const int secondsInWeek = 604800;
				const int secondsInMonth = 2419200;
				const int secondsInYear = 29030400;
				var timestamp = Framework.Time.GetNowUnixTimestamp();
				switch (time)
				{
					case Time.PastHour:
						return timestamp - secondsInHour;
					case Time.Past6Hours:
						return timestamp - (secondsInHour * 6);
					case Time.Past12Hours:
						return timestamp - (secondsInHour * 12);
					case Time.PastDay:
						return timestamp - secondsInDay;
					case Time.Past4Days:
						return timestamp - (secondsInDay * 4);
					case Time.PastWeek:
						return timestamp - secondsInWeek;
					case Time.PastTwoWeeks:
						return timestamp - (secondsInWeek * 2);
					case Time.PastMonth:
						return timestamp - secondsInMonth;
					case Time.Past3Months:
						return timestamp - (secondsInMonth * 3);
					case Time.PastYear:
						return timestamp - secondsInYear;
					case Time.Past2Years:
						return timestamp - (secondsInYear * 2);
					default:
						return 0;
				}
			}

			public enum Time
			{
				PastHour, Past6Hours, Past12Hours, PastDay, Past4Days,
				PastWeek, PastTwoWeeks, PastMonth, Past3Months, Past6Months, PastYear, Past2Years, Everything
			};

			public Time ExportTime { get; set; }
			public bool ExportImages { get; set; }
			public bool ExportAudio { get; set; }
			public bool ExportVideos { get; set; }
			public bool ExportFiles { get; set; }
		}

		public class Result
		{
			public enum State { Success, Failure };

			public State Case { get; set; }
			public string OutputLocation { get; set; }
		}

		public static Task<Result> ExportAsync(BubbleGroup group, CancellationTokenSource cancellationToken, Options option)
		{
			return Task<Result>.Factory.StartNew(() =>
			{
				return Export(group, cancellationToken, option);
			});
		}

		public static Result Export(BubbleGroup group, CancellationTokenSource cancellationToken, Options options)
		{
			var untilTime = Options.ConvertToUnixTimestamp(options.ExportTime);
			var outputLocation = Path.Combine(Platform.GetSettingsPath(), "ConversationExport");
			var cache = BubbleGroupCacheManager.Load(group);
			if (cache == null)
			{
				Utils.DebugPrint("Could not relate the bubblegroup to a cache");
				return new Result { Case = Result.State.Failure };
			}
			//Null out what we dont't need to export
			cache = Utils.Clone(cache);
			cache.Photo = null;
			if (cache.Participants != null)
			{
				foreach (var participant in cache.Participants)
				{
					participant.Photo = null;
				}
			}
			var finalZip = MediaManager.GenerateFileLocation((cache.Name ?? "Unknown").Replace("/", "_") + ".zip");
			// Should never happen, as GenerateFileLocation ensures it's a new file. But just in case.
			if (File.Exists(finalZip))
			{
				File.Delete(finalZip);
			}
			try
			{
				Monitor.Enter(_exportLock);
				if (cancellationToken.Token.IsCancellationRequested)
				{
					cancellationToken.Token.ThrowIfCancellationRequested();
				}
				if (Directory.Exists(outputLocation))
				{
					Directory.Delete(outputLocation, true);
				}
				Directory.CreateDirectory(outputLocation);
				using (var ms = Platform.GetConversationExportAssetsArchiveStream())
				{
					ExtractZip(ms, outputLocation);
				}
				var bubblesJs = Path.Combine(outputLocation, "js", "bubbles.js");
				using (var fs = File.OpenWrite(bubblesJs))
				{
					using (var sw = new StreamWriter(fs))
					{
						using (var writer = new JsonTextWriter(sw))
						{
							sw.Write("angular.module(\"app\").service('exportedBubbles', function () {");
							sw.Write("\n");
							sw.Write("this.bubbles = ");
							writer.WriteStartArray();
							var cursor = new BubbleGroupFactory.Cursor(group, new BubbleGroupFactory.Cursor.Selection());
							while (true)
							{
								var bubbles = cursor.FetchNext();
								if (bubbles == null || !bubbles.Any())
								{
									goto End;
								}
								foreach (var bubble in bubbles)
								{
									if (cancellationToken.Token.IsCancellationRequested)
									{
										cancellationToken.Token.ThrowIfCancellationRequested();
									}
									if (bubble.Time < untilTime)
									{
										goto End;
									}
                                    if (!SupportsBubble(bubble, options))
                                    {
                                        continue;
                                    }
									CopyBubbleFilesToTargetIfNeeded(bubble, outputLocation, options);
									var jobject = JObject.FromObject(bubble);
									jobject.Add("ID", bubble.ID);
									jobject.Add("Type", bubble.GetType().Name);
									jobject.Add("Service", bubble.Service.Information.ServiceName);
									writer.WriteRawValue(jobject.ToString(Formatting.None));
								}
							}
						End:
							writer.WriteEndArray();
							sw.Write(";");
							sw.Write("\n");
							sw.Write("});");
						}
					}
				}
				var cacheJs = Path.Combine(outputLocation, "js", "cache.js");
				using (var fs = File.OpenWrite(cacheJs))
				{
					using (var sw = new StreamWriter(fs))
					{
						using (var writer = new JsonTextWriter(sw))
						{
							sw.Write("angular.module(\"app\").service('exportedBubblesCache', function () {");
							sw.Write("\n");
							sw.Write("this.cache = ");
							var jobject = JObject.FromObject(cache);
							writer.WriteRawValue(jobject.ToString(Formatting.None));
							sw.Write(";");
							sw.Write("\n");
							sw.Write("});");
						}
					}
				}
				using (var fs = File.OpenWrite(finalZip))
				{
					ArchiveZip(fs, outputLocation);
				}
				return new Result
				{
					Case = Result.State.Success,
					OutputLocation = finalZip,
				};
			}
			catch (Exception ex)
			{
				// Insights.Report(ex);
				Utils.DebugPrint("Failed to export conversation: " + ex);
			}
			finally
			{
				try
				{
					Directory.Delete(outputLocation, true);
				}
				catch
				{
					// fall-through
				}
				try
				{
					Monitor.Exit(_exportLock);
				}
				catch
				{
					 // fall-through
				}
			}
			return new Result
			{
				Case = Result.State.Failure
			};
		}

        private static bool SupportsBubble(VisualBubble bubble, Options options)
        {
            if (bubble is TextBubble)
            {
                return true;
            }
            if (bubble is ImageBubble && options.ExportImages)
            {
                return true;
            }
            if (bubble is VideoBubble && options.ExportVideos)
            {
                return true;
            }
            if (bubble is AudioBubble && options.ExportAudio)
            {
                return true;
            }
            if (bubble is FileBubble && options.ExportFiles)
            {
                return true;
            }
            return false;
        }

		// http://stackoverflow.com/questions/703281/getting-path-relative-to-the-current-working-directory
		private static string GetRelativePath(string filespec, string folder)
		{
			Uri pathUri = new Uri(filespec);
			// Folders must end in a slash
			if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
			{
				folder += Path.DirectorySeparatorChar;
			}
			Uri folderUri = new Uri(folder);
			return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
		}

		private static void ArchiveZip(Stream stream, string inputLocation)
		{
			using (var zipFile = ZipStorer.Create(stream, string.Empty))
			{
				foreach (var file in Directory.GetFiles(inputLocation, "*", SearchOption.AllDirectories))
				{
					try
					{
						var relativePath = GetRelativePath(file, inputLocation);
						zipFile.AddFile(ZipStorer.Compression.Store, file, relativePath, null);
					}
					catch (Exception ex)
					{
						Utils.DebugPrint("Failed to archive file: " + file);
					}
				}
			}
		}

		private static void ExtractZip(Stream stream, string outputLocation)
		{
			using (var zipFile = ZipStorer.Open(stream, FileAccess.Read))
			{
				foreach (var file in zipFile.ReadCentralDir())
				{
					zipFile.ExtractFile(file, Path.Combine(outputLocation, file.FilenameInZip));
				}
			}
		}

		private static void CopyBubbleFilesToTargetIfNeeded(VisualBubble bubble, string outputLocation, Options options)
		{
			var outputMediaLocation = Path.Combine(outputLocation, "media");
			if (!Directory.Exists(outputMediaLocation))
			{
				Directory.CreateDirectory(outputMediaLocation);
			}

			if (options.ExportFiles)
			{
				var filesPath = Path.Combine(outputMediaLocation, "files");
				if (!Directory.Exists(filesPath))
				{
					Directory.CreateDirectory(filesPath);
				}
				var fileBubble = bubble as FileBubble;
				if (fileBubble != null)
				{
					var path = fileBubble.Path;
					if (fileBubble.PathType == FileBubble.Type.File && File.Exists(path))
					{
						var fileName = Path.GetFileName(path);
						var newPath = Path.Combine(filesPath, fileName);
						if (!File.Exists(newPath))
						{
							try
							{
								File.Copy(path, newPath);
							}
							catch (Exception ex)
							{
								Utils.DebugPrint("Failed to export file bubble file: " + ex);
							}
						}
						else
						{
							Utils.DebugPrint("File collision: " + newPath);
						}
					}
				}
			}

			if (options.ExportAudio)
			{
				var audioPath = Path.Combine(outputMediaLocation, "audio");
				if (!Directory.Exists(audioPath))
				{
					Directory.CreateDirectory(audioPath);
				}
				var audioBubble = bubble as AudioBubble;
				if (audioBubble != null)
				{
					var path = audioBubble.AudioPath;
					if (audioBubble.AudioType == AudioBubble.Type.File && File.Exists(path))
					{
						var fileName = Path.GetFileName(path);
						var newPath = Path.Combine(audioPath, fileName);
						if (!File.Exists(newPath))
						{
							try
							{
								File.Copy(path, newPath);
							}
							catch (Exception ex)
							{
								Utils.DebugPrint("Failed to export audio bubble file: " + ex);
							}
						}
						else
						{
							Utils.DebugPrint("File collision: " + newPath);
						}
					}
				}
			}

			if (options.ExportVideos)
			{
				var videoBubble = bubble as VideoBubble;
				if (videoBubble != null)
				{
					var videosPath = Path.Combine(outputMediaLocation, "videos");
					if (!Directory.Exists(videosPath))
					{
						Directory.CreateDirectory(videosPath);
					}
					var path = videoBubble.VideoPath;
					if (videoBubble.VideoType == VideoBubble.Type.File && File.Exists(path))
					{
						var fileName = Path.GetFileName(path);
						var newPath = Path.Combine(videosPath, fileName);
						if (!File.Exists(newPath))
						{
							try
							{
								File.Copy(path, newPath);
							}
							catch (Exception ex)
							{
								Utils.DebugPrint("Failed to export video bubble file: " + ex);
							}
						}
						else
						{
							Utils.DebugPrint("File collision: " + newPath);
						}
					}
				}
			}

			if (options.ExportImages)
			{
				var imagesPath = Path.Combine(outputMediaLocation, "images");
				if (!Directory.Exists(imagesPath))
				{
					Directory.CreateDirectory(imagesPath);
				}
				var imageBubble = bubble as ImageBubble;
				if (imageBubble != null)
				{
					var path = imageBubble.ImagePath;
					if (imageBubble.ImageType == ImageBubble.Type.File && File.Exists(path))
					{
						var fileName = Path.GetFileName(path);
						var newPath = Path.Combine(imagesPath, fileName);
						if (!File.Exists(newPath))
						{
							try
							{
								File.Copy(path, newPath);
							}
							catch (Exception ex)
							{
								Utils.DebugPrint("Failed to export image bubble file: " + ex);
							}
						}
						else
						{
							Utils.DebugPrint("File collision: " + newPath);
						}
					}
				}
			}
		}
	}
}
