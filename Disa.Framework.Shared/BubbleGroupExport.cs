using System;
using System.IO;
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
			var outputHtml = Path.Combine(outputLocation, "conversation.html");
			using (var fs = File.OpenWrite(outputHtml))
			{
				using (var sw = new StreamWriter(fs))
				{
					using (var writer = new JsonTextWriter(sw))
					{
						writer.WriteStartArray();
						foreach (var bubble in BubbleGroupDatabase.FetchBubbles(bubbleGroupLocation, null, count, skipDeleted))
						{
							if (bubble.Time > untilTime)
								break;
							CopyBubbleFilesToTargetIfNeeded(bubble,outputLocation);
							var jobject = JObject.FromObject(bubble);
							jobject.Add("ID", bubble.ID);
							jobject.Add("Type", bubble.GetType().Name);
							writer.WriteRawValue(jobject.ToString(Formatting.None));
						}
						writer.WriteEndArray();
					}
				}
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
				if (fileBubble.PathType == FileBubble.Type.File)
				{
					File.Copy(Path.Combine(Platform.GetFilesPath(), fileBubble.FileName), outputMediaLocation);
				}
			}

			if (audioBubble != null)
			{
				if (audioBubble.AudioType == AudioBubble.Type.File)
				{
					File.Copy(Path.Combine(Platform.GetAudioPath(), audioBubble.FileName), outputMediaLocation);
				}
			}

			if (videoBubble != null)
			{
				if (videoBubble.VideoType == VideoBubble.Type.File)
				{
					File.Copy(Path.Combine(Platform.GetVideosPath(), videoBubble.FileName), outputMediaLocation);
				}
			}

			if (imageBubble != null)
			{
				if (imageBubble.ImageType == ImageBubble.Type.File)
				{
					File.Copy(Path.Combine(Platform.GetPicturesPath(), videoBubble.FileName), outputMediaLocation);
				}
			}
		}
	}
}
