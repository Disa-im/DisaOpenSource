using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Disa.Framework
{
	public class BubbleGroupExport
	{
		public static void OutputBubblesInJsonFormat(string bubbleGroupLocation, string jsonOutputLocation,
			 int count = int.MaxValue, bool skipDeleted = true)
		{

			using (var fs = File.OpenWrite(jsonOutputLocation))
			{
				using (var sw = new StreamWriter(fs))
				{
					using (var writer = new JsonTextWriter(sw))
					{
						sw.Write("var json_bubbles = '");
						writer.WriteStartArray();
						foreach (var bubble in BubbleGroupDatabase.FetchBubbles(bubbleGroupLocation, null, count, skipDeleted))
						{
							var jobject = JObject.FromObject(bubble);
							jobject.Add("ID", bubble.ID);
							jobject.Add("Type", bubble.GetType().Name);
							writer.WriteStartObject();
							writer.WritePropertyName("bubble");
							writer.WriteValue(jobject.ToString(Formatting.None));
							writer.WriteEndObject();
						}
						writer.WriteEndArray();
						sw.Write("';");
					}
				}
			}

		}
	}
}
