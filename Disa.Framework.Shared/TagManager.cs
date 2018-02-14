using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using ProtoBuf;
using SQLite;

namespace Disa.Framework
{
    [ProtoContract]
    public static class TagManager
    {
        [ProtoContract]
        private class ConversationTagIds : ISerializableType<ConversationTagIds>
        {
            [ProtoMember(1)]
            [PrimaryKey]
            public string Id { get; set; }
            public byte[] FullyQualifiedTagIdsProtoBytes
            { get; set; }

            [Ignore]
            public HashSet<string> FullyQualifiedTagIds
            {
                get;
                set;
            }

            public override int GetHashCode()
            {
                return Id.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                var conversation = obj as ConversationTagIds;
                if (conversation == null)
                {
                    return false;
                }
                return Id.Equals(conversation.Id);
            }

            public ConversationTagIds SerializeProperties()
            {
                FullyQualifiedTagIdsProtoBytes = Utils.ToProtoBytes(FullyQualifiedTagIds);
                return this;
            }

            public ConversationTagIds DeserializeProperties()
            {
                FullyQualifiedTagIds = Utils.FromProtoBytesToObject<HashSet<string>>(FullyQualifiedTagIdsProtoBytes);
                return this;
            }
        }

        private class TagConversationIds : ISerializableType<TagConversationIds>
        {
            [PrimaryKey]
            public string FullyQualifiedId { get; set; }
            public byte[] BubbleGroupAddressesProtoBytes
            { get; set; }

            [Ignore]
            public HashSet<string> BubbleGroupAddresses
            {
                get;
                set;
            }

            public override int GetHashCode()
            {
                return FullyQualifiedId.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                var conversation = obj as TagConversationIds;
                if (conversation == null)
                {
                    return false;
                }
                return FullyQualifiedId.Equals(conversation.FullyQualifiedId);
            }

            public TagConversationIds SerializeProperties()
            {
                BubbleGroupAddressesProtoBytes = Utils.ToProtoBytes(BubbleGroupAddresses);
                return this;
            }

            public TagConversationIds DeserializeProperties()
            {
                BubbleGroupAddresses = Utils.FromProtoBytesToObject<HashSet<string>>(BubbleGroupAddressesProtoBytes);
                return this;
            }
        }

        [ProtoMember(1)]
        private static readonly string rootName = string.Empty;
        [ProtoMember(2)]
        private static HashSet<Tag> tags = new HashSet<Tag>();
        [ProtoMember(3)]
        private static DirectedAcyclicGraph<Tag> tree = new DirectedAcyclicGraph<Tag>(new Tag()
        {
            Id = rootName,
            Name = rootName,
            FullyQualifiedId = rootName,
        });
        private static Dictionary<Service, Node<Tag>> serviceRoots =
            new Dictionary<Service, Node<Tag>>();
        
        // string: internal path of a tag
        // (internal path of a tag ("email/Label_31/Label_34/Label_49") => "email/Label_49")
        // useful for providing quick tag object lookup for services
        [ProtoMember(4)]
        private static Dictionary<string, Node<Tag>> fullyQualifiedIdDictionary =
            new Dictionary<string, Node<Tag>>();

        // service root
        [ProtoMember(5)]
        private static Dictionary<string, Node<Tag>> serviceRootNodeDictionary =
            new Dictionary<string, Node<Tag>>();

        private static DatabaseManager databaseManager;
        private static AsyncTableQuery<ConversationTagIds> conversationTagIdsTable;
        private static AsyncTableQuery<TagConversationIds> tagConversationIdsTable;

        internal static Node<Tag> Root => tree.Root;

        public delegate void OnTagsCreatedRaiser(IEnumerable<Tag> tag);
        public static event OnTagsCreatedRaiser OnTagsCreated;

        public delegate void OnTagsDeletedRaiser(IEnumerable<Tag> tags);
        public static event OnTagsDeletedRaiser OnTagsDeleted;

        public static IList<Tag> Tags => tags.ToList();
        
        public static void Initialize()
        {
            var databasePath = Platform.GetDatabasePath();
            var conversationDatabasePath = Path.Combine(databasePath, @"ConversationTags.db");
            databaseManager = new DatabaseManager(conversationDatabasePath);

            conversationTagIdsTable = databaseManager.SetupTableObject<ConversationTagIds>();
            tagConversationIdsTable = databaseManager.SetupTableObject<TagConversationIds>();

            var treeDatabasePath = Path.Combine(databasePath, @"ConversationTree.protobytes");
            if (File.Exists(treeDatabasePath))
            {
                lock (tree)
                {
                    tree = Utils.FromProtoBytesToObject<DirectedAcyclicGraph<Tag>>(File.ReadAllBytes(treeDatabasePath));

                    //Initialize serviceRootNodeDictionary
                    foreach (var child in tree.Root.Children)
                    {
                        serviceRootNodeDictionary[child.Data.FullyQualifiedId] = child;
                    }

                    var nodes = new List<Node<Tag>>()
                    {
                        tree.Root,
                    };
                    while (nodes.Count > 0)
                    {
                        var node = nodes[0];
                        nodes.RemoveAt(0);
                        // Setup values, which are stored in a sqlite database
                        Expression<Func<TagConversationIds, bool>> filter = e =>
                            e.FullyQualifiedId.Equals(node.Data.FullyQualifiedId);
                        var tagConversationIds = databaseManager.FindRow<TagConversationIds>(filter);
                        if (tagConversationIds == null)
                        {
                            Utils.DebugPrint($"WUT");
                        }
                        else
                        {
                            node.Data.BubbleGroupAddresses = tagConversationIds.BubbleGroupAddresses;
                        }

                        fullyQualifiedIdDictionary[node.Data.FullyQualifiedId] = node;
                        tags.Add(node.Data);
                        // Setup Parents
                        foreach (var child in node.Children)
                        {
                            child.Parent = node;
                        }

                        nodes.AddRange(node.Children);
                    }
                }
            }

            RegisterServices();

            ServiceEvents.Registered += (sender, service) => 
            {
                RegisterService(service);
            };
            ServiceEvents.UnRegistered += (sender, service) => 
            {
                UnregisterService(service);                
            };
        }

        private static void RegisterService(Service service)
        {
            if(service == null)
            {
                Utils.DebugPrint($"Service is null");
                return;
            }
            
            if (!serviceRootNodeDictionary.ContainsKey(service.Information.ServiceName))
            {
                var tag = CreateNewService(service);
                OnTagsCreated?.Invoke(new List<Tag> { tag });
            }
            else
            {
                var node = serviceRootNodeDictionary[service.Information.ServiceName];
                
                foreach (var tag in node.EnumerateAllDescendantsAndSelfData())
                {
                    tag.Service = service;
                }
                serviceRoots[service] = node;
            }
        }

        internal static void RegisterServices()
        {
            var services = ServiceManager.RegisteredNoUnified.ToList();
            foreach (var service in services)
            {
                RegisterService(service);
            }
        }

        private static void UnregisterService(Service service)
        {
            if(service == null)
            {
                Utils.DebugPrint($"Service is null");
                return;
            }
            
            if (!serviceRootNodeDictionary.ContainsKey(service.Information.ServiceName))
            {
                Utils.DebugPrint($"{service.Information.ServiceName} is not in TagManager");
            }

            serviceRoots.Remove(service);
            var node = serviceRootNodeDictionary[service.Information.ServiceName];
            var serviceTag = node.Data;
            Delete(serviceTag);
        }
        
        private static Tag CreateNewService(Service service)
        {
            var tag = new Tag()
            {
                Id = service.Information.ServiceName,
                FullyQualifiedId = $"{service.Information.ServiceName}",
                Name = service.Information.ServiceName,
                Service = service,
                Parent = tree.Root.Data,
            };

            var serviceRoot = new Node<Tag>(tag, tree.Root) { Name = tag.Name };
            serviceRoots[service] = serviceRoot;
            serviceRootNodeDictionary[service.Information.ServiceName] = serviceRoot;
            fullyQualifiedIdDictionary[tag.FullyQualifiedId] = serviceRoot;
            lock (tree)
            {
                tree.Root.AddChild(serviceRoot);
            }
            tags.Add(tag);

            var tagConversationIds = new TagConversationIds()
            {
                FullyQualifiedId = tag.FullyQualifiedId,
                BubbleGroupAddresses = new HashSet<string>()
            };
            databaseManager.InsertRow(tagConversationIds);

            return tag;
        }

        public static Tag GetServiceRootTag(Service service)
        {
            return serviceRoots.GetValueOrDefault(service)?.Data;
        }

        public static Tag GetTagById(Service service, string id)
        {
            var fullId = $"{service.Information.ServiceName}|{id}";
            return fullyQualifiedIdDictionary.GetValueOrDefault(fullId)?.Data;
        }

        private static Tag CreateTag(Tag tag)
        {
            if (tag.Parent == null || tag.Parent == tag)
            {
                throw new ArgumentException(nameof(Tag.Parent));
            }

            var fullQualifiedId = $"{tag.Service.Information.ServiceName}|{tag.Id}";
            tag.FullyQualifiedId = fullQualifiedId;

            if (tags.Contains(tag))
            {
                return null;
            }
            
            Node<Tag> parentNode;
            if (!fullyQualifiedIdDictionary.TryGetValue(tag.Parent.FullyQualifiedId, out parentNode))
            {
                Utils.DebugPrint($"Node for respective {tag.Parent.FullyQualifiedId} is not found");
                throw new ArgumentException($"{nameof(Tag.Parent)}");
            }
            
            Node<Tag> childNode = null;
            lock (tree)
            {
                childNode = new Node<Tag>(tag, parentNode) { Name = tag.Name };
                parentNode.AddChild(childNode);
            }
            fullyQualifiedIdDictionary[tag.FullyQualifiedId] = childNode;
            tags.Add(tag);
            
            return tag;
        }

        // TODO: Extract Create and CreateService to common method
        public static Tag Create(Tag tag)
        {
            tag = CreateTag(tag);
            
            var tagConversationIds = new TagConversationIds()
            {
                FullyQualifiedId = tag.FullyQualifiedId,
                BubbleGroupAddresses = new HashSet<string>()
            };
            databaseManager.InsertRow(tagConversationIds);

            Persist();

            // Fire event to notify UI that new tag has been created
            OnTagsCreated?.Invoke(new List<Tag> { tag });

            return tag;
        }

        // TODO: Extract Create and CreateService to common method
        public static List<Tag> Create(IEnumerable<Tag> tags)
        {
            var tagList = tags.Select(CreateTag).ToList();

            var tagConversationIds = tagList.Select(tag => new TagConversationIds()
            {
                FullyQualifiedId = tag.FullyQualifiedId,
                BubbleGroupAddresses = new HashSet<string>()
            }).ToList();
            databaseManager.InsertRows(tagConversationIds);
            
            Persist();

            // Fire event to notify UI that new tag has been created
            OnTagsCreated?.Invoke(tagList);

            return tagList;
        }

        // Returns the tags and all the child tags that have been deleted
        private static List<Tag> DeleteTag(Tag tag)
        {
            if (tag == null)
            {
                return new List<Tag>();
            }

            Node<Tag> node;
            if (!fullyQualifiedIdDictionary.TryGetValue(tag.FullyQualifiedId, out node))
            {
                Utils.DebugPrint($"Node for respective {tag.FullyQualifiedId} is not found");
                return new List<Tag>();
            }

            HashSet<Tag> selfAndDescendantsTags = null;
            lock (tree)
            {
                selfAndDescendantsTags = node.EnumerateAllDescendantsAndSelfData();
            }

            foreach (var childrenTag in selfAndDescendantsTags)
            {
                fullyQualifiedIdDictionary.Remove(childrenTag.FullyQualifiedId);
                tags.Remove(childrenTag);
            }

            lock(tree)
            {
                node.Parent.RemoveChild(node);
            }
            
            var tagIds = selfAndDescendantsTags.Select(t => t.FullyQualifiedId).ToHashSet();
            Expression<Func<TagConversationIds, bool>> filter = 
                (tagConversationId) => tagIds.Contains(tagConversationId.FullyQualifiedId);
            databaseManager.DeleteRow(filter);

            return selfAndDescendantsTags.ToList();
        }

        public static void Delete(Tag tag)
        {
            var deletedTags = DeleteTag(tag);
            Persist();
            // Fire event to notify UI that new tag has been deleted
            OnTagsDeleted?.Invoke(deletedTags);
        }

        public static void Delete(IEnumerable<Tag> tags)
        {
            if (tags == null)
            {
                return;
            }

            var deletedTags = tags.SelectMany(DeleteTag).ToList();
            Persist();

            // Fire event to notify UI that new tag has been deleted
            OnTagsDeleted?.Invoke(deletedTags);
        }

        public static bool Exists(Service service, Tag tag)
        {
            tag.FullyQualifiedId = $"{service.Information.ServiceName}|{tag.Id}";
            return tags.Contains(tag);
        }

        public static void UpdateTags(Service service, 
                                      string bubbleGroupAddress, 
                                      IEnumerable<Tag> tagsToAdd = null, 
                                      IEnumerable<Tag> tagsToRemove = null)
        {
            if (tagsToAdd != null)
            {
                Add(service, bubbleGroupAddress, tagsToAdd);
            }
            if (tagsToRemove != null)
            {
                Remove(service, bubbleGroupAddress, tagsToRemove);
            }
        }

        public static void Add(Service service, string bubbleGroupAddress, IEnumerable<Tag> tags)
        {
            if (tags == null)
            {
                Console.WriteLine($"WUT");
            }

            var tagsList = tags.ToList();
            foreach (var tag in tagsList)
            {
                Node<Tag> node;
                if (!fullyQualifiedIdDictionary.TryGetValue(tag.FullyQualifiedId, out node))
                {
                    Utils.DebugPrint($"Node for respective {tag.FullyQualifiedId} is not found");
                    continue;
                }
                node.Data.BubbleGroupAddresses.Add(bubbleGroupAddress);

                // Update in tag id -> conversation ids table
                Expression<Func<TagConversationIds, bool>> filter = 
                    e => e.FullyQualifiedId.Equals(node.Data.FullyQualifiedId);
                var tagConversationIds = databaseManager.FindRow(filter);
                tagConversationIds.BubbleGroupAddresses.Add(bubbleGroupAddress);
                databaseManager.UpdateRow(tagConversationIds);
            }
            
            // Update conversation id -> tag ids table
            Expression<Func<ConversationTagIds, bool>> conversationFilter = 
                e => e.Id.Equals(bubbleGroupAddress);
            var conversationTagIds = databaseManager.FindRow(conversationFilter);
            var fullyQualifiedTagIds = tagsList.Select(t => t.FullyQualifiedId).ToHashSet();
            if (conversationTagIds == null)
            {
                conversationTagIds = new ConversationTagIds()
                {
                    Id = bubbleGroupAddress,
                    FullyQualifiedTagIds = fullyQualifiedTagIds,
                };
                databaseManager.InsertRow(conversationTagIds);
            }
            else
            {
                conversationTagIds.FullyQualifiedTagIds.UnionWith(fullyQualifiedTagIds);
                databaseManager.InsertOrReplaceRow(conversationTagIds);
            }
        }

        public static void Remove(Service service, string bubbleGroupAddress, IEnumerable<Tag> tags)
        {
            var tagsList = tags.ToList();
            foreach (var tag in tagsList)
            {
                Node<Tag> node;
                if (!fullyQualifiedIdDictionary.TryGetValue(tag.FullyQualifiedId, out node))
                {
                    continue;
                }
                node.Data.BubbleGroupAddresses.Remove(bubbleGroupAddress);
                
                // Update in database
                Expression<Func<TagConversationIds, bool>> filter = 
                    e => e.FullyQualifiedId.Equals(node.Data.FullyQualifiedId);
                var tagConversationIds = databaseManager.FindRow(filter);
                tagConversationIds.BubbleGroupAddresses.Remove(bubbleGroupAddress);
                databaseManager.UpdateRow(tagConversationIds);
            }
            
            // Update conversation id -> tag ids table
            Expression<Func<ConversationTagIds, bool>> conversationFilter = 
                e => e.Id.Equals(bubbleGroupAddress);
            var conversationTagIds = databaseManager.FindRow(conversationFilter);

            if (conversationTagIds != null)
            {
                var tagIds = tagsList.Select(t => t.Id).ToHashSet();
                conversationTagIds.FullyQualifiedTagIds.RemoveWhere(id => tagIds.Contains(id));
                databaseManager.InsertOrReplaceRow(conversationTagIds);
            }
            else
            {
                Utils.DebugPrint($"");
            }
        }

        public static List<Tag> GetAllServiceTags(Service service)
        {
            var serviceRoot = serviceRoots[service];
            lock (tree)
            {
                var nodes = serviceRoot.EnumerateAllDescendantsAndSelf();
                return nodes.Select(n => n.Data).ToHashSet().ToList();
            }
        }
        
        public static IList<Tag> FlatSubTree(Service service)
        {
            if (!serviceRoots.ContainsKey(service))
            {
                return new List<Tag>();
            }

            var serviceRoot = serviceRoots[service];

            lock (tree)
            {
                return serviceRoot.Children.SelectMany(child => child.FlatSubTreeWithData())
                    .Select(c =>
                    {
                        c.Value.ConvenientName = c.Key;
                        return c.Value;
                    })
                    .ToList();
            }
        }

        // Expose a method to return a node

        //public Node<Tag, HashSet<Conversation>> GetNode(Node<Tag, HashSet<Conversation>> root, string tag)
        //{
        //    var directoryNames = path.Split("/").ToList(tag);
        //    var currentNode = root;
        //    var index = 0;
        //    while (true)
        //    {
        //        if (index == directoryNames.Count)
        //        {
        //            break;
        //        }
        //        var directoryName = directoryNames[index];
        //        if (!currentNode.ChildrenDictionary.ContainsKey(directoryName))
        //        {
        //            break;
        //        }
        //        var child = currentNode.ChildrenDictionary[directoryName];
        //        currentNode = child;
        //        index++;
        //    }
        //    if (index == directoryNames.Count)
        //    {
        //        return currentNode;
        //    }
        //    return null;
        //}

        public static List<BubbleGroup> GetAllBubbleGroups(Tag tag)
        {
            var conversationIds = new HashSet<string>();
            
            Node<Tag> node;
            if (!fullyQualifiedIdDictionary.TryGetValue(tag.FullyQualifiedId, out node))
            {
                Utils.DebugPrint($"Node for respective {tag.FullyQualifiedId} is not found");
                return new List<BubbleGroup>();
            }

            HashSet<string> conversationIds;
            lock (tree)
            {
                conversationIds = node.EnumerateAllDescendantsAndSelfData()
                                      .SelectMany(t => t.BubbleGroupAddresses)
                                      .ToHashSet();
            }

            var bubbleGroups = BubbleGroupManager.FindAll((BubbleGroup bg) => conversationIds.Contains(bg.Address))
                                                 .ToList();
            return bubbleGroups;
        }

        public static HashSet<BubbleGroup> GetAllBubbleGroups(IEnumerable<Tag> tags)
        {
            if (tags == null)
            {
                Utils.DebugPrint($"{nameof(tags)} is null");
                return new HashSet<BubbleGroup>();
            }

            var conversationIds = new HashSet<string>();
            foreach (var tag in tags)
            {
                Node<Tag> node;
                if (!fullyQualifiedIdDictionary.TryGetValue(tag.FullyQualifiedId, out node))
                {
                    Utils.DebugPrint($"Node for respective {tag.FullyQualifiedId} is not found");
                    continue;
                }
                
                IEnumerable<string> tagConversationIds = null;
                lock (tree)
                {
                    tagConversationIds = node.EnumerateAllDescendantsAndSelfData()
                                             .SelectMany(t => t.BubbleGroupAddresses);
                }
                conversationIds.UnionWith(tagConversationIds);
            }

            var bubbleGroups = BubbleGroupManager.FindAll((BubbleGroup bg) => conversationIds.Contains(bg.Address))
                                                 .ToHashSet();
            return bubbleGroups;
        }
        
        public static void RemoveBubbleGroups(IEnumerable<string> bubbleGroupAdresses)
        {
            var bubbleGroupAdressSet = bubbleGroupAdresses.ToHashSet();
            // Update in database
            Expression<Func<ConversationTagIds, bool>> filter = e => bubbleGroupAdressSet.Contains(e.Id);
            var conversationTagIds = databaseManager.FindRows(filter);
            foreach (var conversationTagId in conversationTagIds)
            {
                foreach (var fullyQualifiedTagId in conversationTagId.FullyQualifiedTagIds)
                {
                    Node<Tag> node;
                    if (!fullyQualifiedIdDictionary.TryGetValue(fullyQualifiedTagId, out node))
                    {
                        Utils.DebugPrint($"Node for respective {fullyQualifiedTagId} is not found");
                        continue;
                    }
                    node.Data.BubbleGroupAddresses.Remove(conversationTagId.Id);
                }
            }

            databaseManager.DeleteRows(conversationTagIds);
            Persist();
        }

        public static void PersistTags()
        {
            var databasePath = Platform.GetDatabasePath();
            var treeDatabasePath = Path.Combine(databasePath, @"ConversationTree.protobytes");
            byte[] bytes = null;
            lock (tree)
            {
                bytes = Utils.ToProtoBytes(tree);
            }
            File.WriteAllBytes(treeDatabasePath, bytes);
        }

        public static void Persist()
        {
            var databasePath = Platform.GetDatabasePath();
            var conversationDatabasePath = Path.Combine(databasePath, @"ConversationTags.db");
            databaseManager = new DatabaseManager(conversationDatabasePath);

            conversationTagIdsTable = databaseManager.SetupTableObject<ConversationTagIds>();
            tagConversationIdsTable = databaseManager.SetupTableObject<TagConversationIds>();
            PersistTags();
        }

        public static string PrintHierarchyToString()
        {
            lock (tree)
            {
                return tree.PrintToString();           
            }
        }

        public static void PrintAllTags()
        {
            foreach (var tag in tags)
            {
                Console.WriteLine($"{tag}");
            }
        }
    }
}
