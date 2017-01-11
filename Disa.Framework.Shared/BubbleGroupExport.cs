using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Disa.Framework.Bubbles;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Disa.Framework
{
	public class BubbleGroupExport
	{
		/// <summary>
		/// Creates a rich html representation of the bubblegroup in html, with all the media referenced in the bubblegroup file.
		/// </summary>
		/// <param name="bubbleGroupLocation">Bubble group file location.</param>
		/// <param name="outputLocation">Output folder location , where everyhting will be created.</param>
		/// <param name="untilTime">Unix timestamp until which the conversations must be exported.</param>
		/// <param name="count">count of the number of messages that have to be exported, has the default of maxvalue, and has a  higher precendence than the timestamp.</param>
		/// <param name="skipDeleted">If set to <c>true</c> skip the deleted messages, defaults to true.</param>
		public static void ExportConversation(string bubbleGroupLocation, string outputLocation, long untilTime = long.MaxValue,
			 int count = int.MaxValue, bool skipDeleted = true)
		{
			outputLocation = Path.Combine(outputLocation, "Conversation");
			ExtractArchive(Platform.GetConversationExportAssetsArchiveStream(), outputLocation);
			var bubblesJs = Path.Combine(outputLocation, "js", "bubbles.js");
			if (File.Exists(bubblesJs))
				File.Delete(bubblesJs);
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
						foreach (var bubble in BubbleGroupDatabase.FetchBubbles(bubbleGroupLocation, null, count, skipDeleted))
						{
							if (bubble is PartyInformationBubble)
								continue;
							if (bubble.Time > untilTime)
								break;
							CopyBubbleFilesToTargetIfNeeded(bubble,outputLocation);
							var jobject = JObject.FromObject(bubble);
							jobject.Add("ID", bubble.ID);
							jobject.Add("Type", bubble.GetType().Name);
							writer.WriteRawValue(jobject.ToString(Formatting.None));
						}
						writer.WriteEndArray();
						sw.Write(";");
						sw.Write("\n");
						sw.Write("});");
					}
				}
			}
		}

		private static void ExtractArchive(Stream stream, string outputLocation)
		{
			var zipFile = ZipStorer.Open(stream, FileAccess.Read);
			foreach (var file in zipFile.ReadCentralDir())
			{
				zipFile.ExtractFile(file, Path.Combine(outputLocation, file.FilenameInZip));
			}
		}

		private static void CopyBubbleFilesToTargetIfNeeded(VisualBubble bubble, string outputLocation)
		{
			var fileBubble = bubble as FileBubble;
			var audioBubble = bubble as AudioBubble;
			var videoBubble = bubble as VideoBubble;
			var imageBubble = bubble as ImageBubble;

			string outputMediaLocation = Path.Combine(outputLocation, "media");
			Directory.CreateDirectory(outputMediaLocation);
			if (fileBubble != null)
			{
				if (fileBubble.PathType == FileBubble.Type.File && fileBubble.PathNative != null)
				{
					string fileName = Path.GetFileName(fileBubble.PathNative);
					string source = Path.Combine(Platform.GetFilesPath(), fileName);
					File.Copy(source, Path.Combine(outputMediaLocation, fileName),true);
				}
			}

			if (audioBubble != null)
			{
				if (audioBubble.AudioType == AudioBubble.Type.File && audioBubble.AudioPathNative != null) 
				{
					string fileName = Path.GetFileName(audioBubble.AudioPathNative);
					string source = Path.Combine(Platform.GetAudioPath(), fileName);
					File.Copy(source, Path.Combine(outputMediaLocation, fileName),true);
				}
			}

			if (videoBubble != null)
			{
				if (videoBubble.VideoType == VideoBubble.Type.File && videoBubble.VideoPathNative != null)
				{
					string fileName = Path.GetFileName(videoBubble.VideoPathNative);
					string source = Path.Combine(Platform.GetVideosPath(), fileName);
					File.Copy(source, Path.Combine(outputMediaLocation, fileName),true);
				}
			}

			if (imageBubble != null)
			{
				if (imageBubble.ImageType == ImageBubble.Type.File && imageBubble.ImagePathNative != null)
				{
					string fileName = Path.GetFileName(imageBubble.ImagePathNative);
					string source = Path.Combine(Platform.GetPicturesPath(), fileName);
					File.Copy(source, Path.Combine(outputMediaLocation, fileName), true);
				}
			}
		}
	}
}
