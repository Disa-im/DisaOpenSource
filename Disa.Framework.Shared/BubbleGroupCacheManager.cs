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
        private static readonly object Lock = new object();

        private static string GetLocation()
        {
            var databasePath = Platform.GetDatabasePath();
            var cachePath = Path.Combine(databasePath, "BubbleGroupCache.db");
            return cachePath;
        }

        private static BubbleGroupCache Generate(BubbleGroup group, string guid)
        {
            var bubbleGroupCache = new BubbleGroupCache
            {
                Name = group.Title,
                Photo = group.Photo,
                Participants = group.Participants.ToList(),
                Guid = guid,
            };
            return bubbleGroupCache;
        }

        public static void Save()
        {
            lock (Lock)
            {
                var sw = new Stopwatch();
                sw.Start();

                var location = GetLocation();

                try
                {
                    var items = new List<BubbleGroupCache>();

                    foreach (var group in BubbleGroupManager.BubbleGroupsImmutable)
                    {
                        var workingGroup = group;
                        var unifiedGroup = workingGroup as UnifiedBubbleGroup;
                        if (unifiedGroup != null)
                        {
                            workingGroup = unifiedGroup.PrimaryGroup;
                        }
                        var groupGuid = group.ID;
                        var cache = Generate(workingGroup, groupGuid);
                        items.Add(cache);
                    }

                    using (var ms = new MemoryStream())
                    {
                        Serializer.Serialize(ms, items);
                        var bytes = ms.ToArray();
                        File.WriteAllBytes(location, bytes);
                    }
                }
                catch (Exception ex)
                {
                    Utils.DebugPrint("Failed to save bubble group cache: " + ex);
                    if (File.Exists(location))
                    {
                        File.Delete(location);
                    }
                }

                sw.Stop();
                Utils.DebugPrint("Saving bubble group cache took " + sw.ElapsedMilliseconds + "ms.");
            }
        }

        private static void Bind(BubbleGroup associatedGroup, BubbleGroupCache item)
        {
            associatedGroup.Title = item.Name;
            associatedGroup.Photo = item.Photo;
            associatedGroup.IsPhotoSetInitiallyFromCache = true;
            if (item.Participants != null)
            {
                associatedGroup.Participants = new ThreadSafeList<DisaParticipant>(item.Participants);
                foreach (var participant in associatedGroup.Participants)
                {
                    participant.IsPhotoSetInitiallyFromCache = true;
                }
            }
        }

		internal static BubbleGroupCache Load(BubbleGroup group)
		{
			return LoadInternal(group);
		}

		private static BubbleGroupCache LoadInternal(BubbleGroup group = null)
		{
			lock (Lock)
			{
				var sw = new Stopwatch();
				sw.Start();

				var location = GetLocation();

				try
				{
					if (File.Exists(location))
					{
						using (var fs = File.OpenRead(location))
						{
							var items = Serializer.Deserialize<List<BubbleGroupCache>>(fs);

							foreach (var item in items)
							{
								if (group != null)
								{
									if (item.Guid == group.ID)
									{
										return item;
									}
								}
								else
								{
									var associatedGroup = BubbleGroupManager.Find(item.Guid);

									if (associatedGroup == null)
									{
										continue;
									}

									var unifiedGroup = associatedGroup as UnifiedBubbleGroup;
									if (unifiedGroup != null)
									{
										associatedGroup = unifiedGroup.PrimaryGroup;
									}
									Bind(associatedGroup, item);
								}
							}
						}
					}
				}
				catch (Exception ex)
				{
					Utils.DebugPrint("Failed to load bubble group cache: " + ex);
					if (File.Exists(location))
					{
						File.Delete(location);
					}
				}

				sw.Stop();
				Utils.DebugPrint("Loading bubble group cache took " + sw.ElapsedMilliseconds + "ms.");
			}

			return null;
		}

        internal static void LoadAll()
        {
			LoadInternal();
        }
    }
}