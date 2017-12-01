using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using ProtoBuf;
using SQLite;

namespace Disa.Framework
{
    [ProtoContract]
    public static class TagManager
    {
        [ProtoContract]
        class ConversationTagIds : ISerializableType<ConversationTagIds>
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

        class TagConversationIds : ISerializableType<TagConversationIds>
        {
            [PrimaryKey]
            public string FullyQualifiedId { get; set; }
            public byte[] FullyQualifiedConversationIdsProtoBytes
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
                FullyQualifiedConversationIdsProtoBytes = Utils.ToProtoBytes(BubbleGroupAddresses);
                return this;
            }

            public TagConversationIds DeserializeProperties()
            {
                BubbleGroupAddresses = Utils.FromProtoBytesToObject<HashSet<string>>(FullyQualifiedConversationIdsProtoBytes);
                return this;
            }
        }

        [ProtoMember(1)]
        private static readonly string rootName = string.Empty;
        [ProtoMember(2)]
        private static HashSet<Tag> tags = new HashSet<Tag>();
        [ProtoMember(3)]
        private static Tree<Tag, HashSet<string>> tree = new Tree<Tag, HashSet<string>>(new Tag()
        {
            Id = rootName,
            Name = rootName,
            FullyQualifiedId = rootName,
        });
        private static Dictionary<Service, Node<Tag, HashSet<string>>> serviceRoots =
            new Dictionary<Service, Node<Tag, HashSet<string>>>();
        
        // string: internal path of a tag
        // (internal path of a tag ("email/Label_31/Label_34/Label_49") => "email/Label_49")
        // useful for providing quick tag object lookup for services
        [ProtoMember(4)]
        private static Dictionary<string, Node<Tag, HashSet<string>>> fullyQualifiedIdDictionary =
            new Dictionary<string, Node<Tag, HashSet<string>>>();

        // service root
        [ProtoMember(5)]
        private static Dictionary<string, Node<Tag, HashSet<string>>> serviceRootNodeDictionary =
            new Dictionary<string, Node<Tag, HashSet<string>>>();

        private static DatabaseManager databaseManager;
        private static AsyncTableQuery<ConversationTagIds> conversationTagIdsTable;
        private static AsyncTableQuery<TagConversationIds> tagConversationIdsTable;

        internal static void Initialize()
        {
            var databasePath = Platform.GetDatabasePath();
            var conversationDatabasePath = Path.Combine(databasePath, @"ConversationTags.db");
            databaseManager = new DatabaseManager(conversationDatabasePath);

            conversationTagIdsTable = databaseManager.SetupTableObject<ConversationTagIds>();
            tagConversationIdsTable = databaseManager.SetupTableObject<TagConversationIds>();

            var treeDatabasePath = Path.Combine(databasePath, @"ConversationTree.protobytes");
            if (File.Exists(treeDatabasePath))
            {
                tree = Utils.FromProtoBytesToObject <Tree<Tag, HashSet<string>>>(File.ReadAllBytes(treeDatabasePath));
                var nodes = new List<Node<Tag, HashSet<string>>>()
                {
                    tree.Root,
                };
                while (nodes.Count > 0)
                {
                    var node = nodes[0];
                    nodes.RemoveAt(0);
                    Expression<Func<TagConversationIds, bool>> filter = e => e.FullyQualifiedId.Equals(node.Key.FullyQualifiedId);
                    var tagConversationIds = databaseManager.FindRow<TagConversationIds>(filter);
                    if (tagConversationIds == null)
                    {
                        Utils.DebugPrint($"WUT");
                    }
                    else
                    {
                        node.Value = tagConversationIds.BubbleGroupAddresses;
                    }
                    fullyQualifiedIdDictionary[node.Key.FullyQualifiedId] = node;
                    tags.Add(node.Key);
                    nodes.AddRange(node.Children);
                }
            }
        }

        internal static void InitializeServices()
        {
            var services = ServiceManager.RegisteredNoUnified;
            foreach (var service in services)
            {
                if (!serviceRootNodeDictionary.ContainsKey(service.Information.ServiceName))
                {
                    var tag = CreateNewService(service);
                }
                else
                {
                    var node = serviceRootNodeDictionary[service.Information.ServiceName];
                    serviceRoots[service] = node;
                }
            }

            // TODO: add code for removing tag space when a plugin is uninstalled
        }

        private static Tag CreateNewService(Service service)
        {
            var tag = new Tag()
            {
                Id = service.Information.ServiceName,
                FullyQualifiedId = $"{service.Information.ServiceName}",
                Name = service.Information.ServiceName,
                Service = service,
                Parent = tree.Root.Key,
            };

            var serviceRoot = new Node<Tag, HashSet<string>>(tag, tree.Root);
            serviceRoots[service] = serviceRoot;
            serviceRootNodeDictionary[service.Information.ServiceName] = serviceRoot;
            fullyQualifiedIdDictionary[tag.FullyQualifiedId] = serviceRoot;
            tree.Root.AddChild(serviceRoot);
            tags.Add(tag);
            //service.RootTag = tag;
            return tag;
        }

        public static Tag GetServiceRootTag(Service service)
        {
            return serviceRoots[service].Key;
        }

        public static Tag GetTagById(Service service, string id)
        {
            var fullId = $"{service.Information.ServiceName}|{id}";
            if (!fullyQualifiedIdDictionary.ContainsKey(fullId))
            {
                return null;
            }
            return fullyQualifiedIdDictionary[fullId].Key;
        }

        public static Tag Create(Tag tag)
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

            if (!fullyQualifiedIdDictionary.ContainsKey(tag.Parent.FullyQualifiedId))
            {
                throw new ArgumentException($"{nameof(Tag.Parent)}");
            }
            var parentNode = fullyQualifiedIdDictionary[tag.Parent.FullyQualifiedId];
            var childNode = new Node<Tag, HashSet<string>>(tag, parentNode);
            parentNode.AddChild(childNode);
            fullyQualifiedIdDictionary[tag.FullyQualifiedId] = childNode;
            tags.Add(tag);
            return tag;
        }

        public static void Delete(Tag tag)
        {
            var node = fullyQualifiedIdDictionary[tag.FullyQualifiedId];
            node.Parent.RemoveChild(node);
            fullyQualifiedIdDictionary.Remove(tag.FullyQualifiedId);
            tags.Remove(tag);
        }

        public static bool Exists(Service service, Tag tag)
        {
            tag.FullyQualifiedId = $"{service.Information.ServiceName}|{tag.Id}";
            return tags.Contains(tag);
        }

        public static void Add(Service service, BubbleGroup bubbleGroup, IEnumerable<Tag> tags)
        {
            foreach (var tag in tags)
            {
                var node = fullyQualifiedIdDictionary[tag.FullyQualifiedId];
                node.Value.Add(bubbleGroup.Address);
            }
        }

        public static void Remove(Service service, BubbleGroup bubbleGroup, IEnumerable<Tag> tags)
        {
            foreach (var tag in tags)
            {
                var node = fullyQualifiedIdDictionary[tag.FullyQualifiedId];
                node.Value.Remove(bubbleGroup.Address);
            }
        }

        public static HashSet<Tag> GetAllTags()
        {
            return tags;
        }

        public static HashSet<Tag> GetAllServiceTags(Service service)
        {
            var serviceRoot = serviceRoots[service];
            var nodes = serviceRoot.EnumerateAllDescendantsAndSelf();
            return nodes.Select(n => n.Key).ToHashSet();
        }

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

        public static HashSet<BubbleGroup> GetAllBubbleGroups(IEnumerable<Tag> tags)
        {
            var conversationIds = new HashSet<string>();
            foreach (var tag in tags)
            {
                var node = fullyQualifiedIdDictionary[tag.FullyQualifiedId];
                if (node == null)
                {
                    continue;
                }
                var tagConversationIds = node.EnumerateAllDescendantsAndSelfData().SelectMany(list => list);
                conversationIds.UnionWith(tagConversationIds);
            }

            var bubbleGroups = BubbleGroupManager.FindAll((BubbleGroup bg) => conversationIds.Contains(bg.Address)).ToHashSet();
            return bubbleGroups;
        }

        internal static void Persist()
        {
            var databasePath = Platform.GetDatabasePath();
            var conversationDatabasePath = Path.Combine(databasePath, @"ConversationTags.db");
            databaseManager = new DatabaseManager(conversationDatabasePath);

            conversationTagIdsTable = databaseManager.SetupTableObject<ConversationTagIds>();
            tagConversationIdsTable = databaseManager.SetupTableObject<TagConversationIds>();

            var treeDatabasePath = Path.Combine(databasePath, @"ConversationTree.db");
            
            var bytes = Utils.ToProtoBytes(tree);
            File.WriteAllBytes(treeDatabasePath, bytes);
        }

        public static string PrintHierarchy()
        {
            return tree.PrintToString();
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
