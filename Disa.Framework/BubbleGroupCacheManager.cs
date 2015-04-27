using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using ProtoBuf;

namespace Disa.Framework
{
    public class BubbleGroupCacheManager
    {
        private static readonly object BubbleGroupNamesLock = new object();

        private static string GetLocation()
        {
            var settingsPath = Platform.GetSettingsPath();
            var groupNamesPath = Path.Combine(settingsPath, "BubbleGroupCache.xml");
            return groupNamesPath;
        }

        public static void Save()
        {
            try
            {
                lock (BubbleGroupNamesLock)
                {
                    var location = GetLocation();
                    var sw = new Stopwatch();
                    sw.Start();
                    using (var xmlWriter = XmlWriter.Create((string) location))
                    {
                        xmlWriter.WriteStartDocument();
                        xmlWriter.WriteStartElement("BubbleGroupNames");
                        foreach (var group in BubbleGroupManager.BubbleGroupsImmutable)
                        {
                            var workingGroup = @group;

                            var unifiedGroup = workingGroup as UnifiedBubbleGroup;
                            if (unifiedGroup != null)
                            {
                                workingGroup = unifiedGroup.PrimaryGroup;
                            }

                            xmlWriter.WriteStartElement("BubbleGroupName");

                            if (workingGroup.Title != null)
                            {
                                xmlWriter.WriteAttributeString("Name", workingGroup.Title);
                            }

                            if (workingGroup.Participants.Any())
                            {
                                using (var ms = new MemoryStream())
                                {
                                    Serializer.Serialize(ms, workingGroup.Participants);
                                    xmlWriter.WriteAttributeString("Participants",
                                        Convert.ToBase64String(ms.ToArray()));
                                }
                            }

                            if (workingGroup.Photo != null)
                            {
                                using (var ms = new MemoryStream())
                                {
                                    Serializer.Serialize(ms, workingGroup.Photo);
                                    xmlWriter.WriteAttributeString("Photo",
                                        Convert.ToBase64String(ms.ToArray()));
                                }
                            }

                            xmlWriter.WriteAttributeString("Guid", @group.ID);

                            xmlWriter.WriteEndElement();
                        }
                        xmlWriter.WriteEndElement();
                        xmlWriter.WriteEndDocument();
                    }
                    sw.Stop();
                    Utils.DebugPrint("Saving bubble group names took " + sw.ElapsedMilliseconds + "ms.");
                }
            }
            catch (Exception ex)
            {
                Utils.DebugPrint("Failed to save bubble groups " + ex);
            }
        }

        internal static IEnumerable<BubbleGroupCache> Load()
        {
            var location = GetLocation();
            if (!File.Exists(location))
                yield break;

            lock (BubbleGroupNamesLock)
            {
                using (var xmlReader = XmlReader.Create(location))
                {
                    while (true)
                    {
                        bool read;
                        try
                        {
                            read = xmlReader.Read();
                        }
                        catch
                        {
                            Utils.DebugPrint(
                                "Failed to read bubble group names in. Something must've corrupt. Nuking file.");
                            File.Delete(location);
                            yield break;
                        }

                        if (!read)
                            break;

                        if (!xmlReader.IsStartElement()) continue;

                        switch (xmlReader.Name)
                        {
                            case "BubbleGroupName":

                                var name = xmlReader["Name"];
                                var guid = xmlReader["Guid"];

                                DisaThumbnail photo = null;
                                var encodedPhoto = xmlReader["Photo"];
                                if (encodedPhoto != null)
                                {
                                    using (var ms = new MemoryStream(Convert.FromBase64String(encodedPhoto)))
                                    {
                                        photo = Serializer.Deserialize<DisaThumbnail>(ms);
                                    }
                                }

                                var encodedParticipants = xmlReader["Participants"];
                                List<DisaParticipant> participants = null;
                                if (encodedParticipants != null)
                                {
                                    using (var ms = new MemoryStream(Convert.FromBase64String(encodedParticipants)))
                                    {
                                        participants = Serializer.Deserialize<List<DisaParticipant>>(ms);
                                    }
                                }

                                yield return new BubbleGroupCache(guid, name, photo, participants);

                                break;
                        }
                    }
                }
            }
        }
    }
}