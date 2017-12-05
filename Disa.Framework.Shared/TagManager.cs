﻿using System;
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
        private static DirectedAcyclicGraph<Tag, HashSet<string>> tree = new DirectedAcyclicGraph<Tag, HashSet<string>>(new Tag()
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

        internal static Node<Tag, HashSet<string>> Root { get => tree.Root; }

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
                tree = Utils.FromProtoBytesToObject <DirectedAcyclicGraph<Tag, HashSet<string>>>(File.ReadAllBytes(treeDatabasePath));

                //Initialize serviceRootNodeDictionary
                foreach (var child in tree.Root.Children)
                {
                    serviceRootNodeDictionary[child.Key.FullyQualifiedId] = child;
                }

                var nodes = new List<Node<Tag, HashSet<string>>>()
                {
                    tree.Root,
                };
                while (nodes.Count > 0)
                {
                    var node = nodes[0];
                    nodes.RemoveAt(0);
                    // Setup values, which are stored in a sqlite database
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
                    // Setup Parents
                    foreach (var child in node.Children)
                    {
                        child.Parent = node;
                    }
                    nodes.AddRange(node.Children);
                }
            }
            InitializeServices();
        }

        internal static void InitializeServices()
        {
            //var services = ServiceManager.RegisteredNoUnified;
            var services = ServiceManager.AllNoUnified;
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

        // TODO: Extract Create and CreateService to common method
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

            var tagConversationIds = new TagConversationIds()
            {
                FullyQualifiedId = tag.FullyQualifiedId,
                BubbleGroupAddresses = new HashSet<string>()
            };
            databaseManager.InsertRow(tagConversationIds);
            Persist();

            return tag;
        }

        public static void Delete(Tag tag)
        {
            var node = fullyQualifiedIdDictionary[tag.FullyQualifiedId];
            node.Parent.RemoveChild(node);
            fullyQualifiedIdDictionary.Remove(tag.FullyQualifiedId);
            tags.Remove(tag);
            Persist();
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
            foreach (var tag in tags)
            {
                var node = fullyQualifiedIdDictionary[tag.FullyQualifiedId];
                node.Value.Add(bubbleGroupAddress);

                // Update in database
                Expression<Func<TagConversationIds, bool>> filter = e => e.FullyQualifiedId.Equals(node.Key.FullyQualifiedId);
                var tagConversationIds = databaseManager.FindRow(filter);
                tagConversationIds.BubbleGroupAddresses.Add(bubbleGroupAddress);
                databaseManager.UpdateRow(tagConversationIds);

                var conversationTagIds = new ConversationTagIds()
                {
                    Id = bubbleGroupAddress,
                    FullyQualifiedTagIds = tags.Select(t => t.Id).ToHashSet(),
                };
                //databaseManager.InsertRow();
            }
        }

        public static void Remove(Service service, string bubbleGroupAddress, IEnumerable<Tag> tags)
        {
            foreach (var tag in tags)
            {
                var node = fullyQualifiedIdDictionary[tag.FullyQualifiedId];
                node.Value.Remove(bubbleGroupAddress);
                
                // Update in database
                Expression<Func<TagConversationIds, bool>> filter = e => e.FullyQualifiedId.Equals(node.Key.FullyQualifiedId);
                var tagConversationIds = databaseManager.FindRow(filter);
                tagConversationIds.BubbleGroupAddresses.Remove(bubbleGroupAddress);
                databaseManager.UpdateRow(tagConversationIds);

                var conversationTagIds = new ConversationTagIds()
                {
                    Id = bubbleGroupAddress,
                    FullyQualifiedTagIds = tags.Select(t => t.Id).ToHashSet(),
                };
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

        public static void Persist()
        {
            var databasePath = Platform.GetDatabasePath();
            var conversationDatabasePath = Path.Combine(databasePath, @"ConversationTags.db");
            databaseManager = new DatabaseManager(conversationDatabasePath);

            conversationTagIdsTable = databaseManager.SetupTableObject<ConversationTagIds>();
            tagConversationIdsTable = databaseManager.SetupTableObject<TagConversationIds>();

            var treeDatabasePath = Path.Combine(databasePath, @"ConversationTree.protobytes");
            
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
