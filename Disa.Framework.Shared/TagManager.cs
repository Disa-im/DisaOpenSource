using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Disa.Framework
{
    [ProtoContract]
    public class TagManager
    {
        [ProtoContract]
        class Conversation
        {
            [ProtoMember(1)]
            public string Id { get; set; }

            public override int GetHashCode()
            {
                return Id.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                var conversation = obj as Conversation;
                if (conversation == null)
                {
                    return false;
                }
                return Id.Equals(conversation.Id);
            }
        }

        [ProtoMember(1)]
        private static readonly string rootName = string.Empty;
        [ProtoMember(2)]
        private readonly HashSet<Tag> tags = new HashSet<Tag>();
        [ProtoMember(3)]
        private readonly Tree<Tag, HashSet<Conversation>> tree = new Tree<Tag, HashSet<Conversation>>(new Tag() { Id = rootName, Name = rootName, });
        private readonly Dictionary<Service, Node<Tag, HashSet<Conversation>>> serviceRoots =
            new Dictionary<Service, Node<Tag, HashSet<Conversation>>>();
        
        // string: internal path of a tag
        // (internal path of a tag ("email/Label_31/Label_34/Label_49") => "email/Label_49")
        // useful for providing quick tag object lookup for services
        [ProtoMember(4)]
        private readonly Dictionary<string, Node<Tag, HashSet<Conversation>>> fullyQualifiedIdDictionary =
            new Dictionary<string, Node<Tag, HashSet<Conversation>>>();

        // service root
        [ProtoMember(5)]
        private readonly Dictionary<string, Node<Tag, HashSet<Conversation>>> serviceRootNodeDictionary =
            new Dictionary<string, Node<Tag, HashSet<Conversation>>>();

        public TagManager()
        {

        }

        internal void InitializeServices()
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

        private Tag CreateNewService(Service service)
        {
            var tag = new Tag()
            {
                Id = service.Information.ServiceName,
                FullyQualifiedId = $"{service.Information.ServiceName}",
                Name = service.Information.ServiceName,
                Service = service,
            };

            var serviceRoot = new Node<Tag, HashSet<Conversation>>(tag, tree.Root);
            serviceRoots[service] = serviceRoot;
            serviceRootNodeDictionary[service.Information.ServiceName] = serviceRoot;
            fullyQualifiedIdDictionary[tag.FullyQualifiedId] = serviceRoot;
            tree.Root.AddChild(serviceRoot);
            tags.Add(tag);
            //service.RootTag = tag;
            return tag;
        }

        public Tag GetServiceRootTag(Service service)
        {
            return serviceRoots[service].Key;
        }

        public Tag GetTagById(Service service, string id)
        {
            var fullId = $"{service.Information.ServiceName}|{id}";
            if (!fullyQualifiedIdDictionary.ContainsKey(fullId))
            {
                return null;
            }
            return fullyQualifiedIdDictionary[fullId].Key;
        }

        public Tag Create(Tag tag)
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
            var childNode = new Node<Tag, HashSet<Conversation>>(tag, parentNode);
            parentNode.AddChild(childNode);
            fullyQualifiedIdDictionary[tag.FullyQualifiedId] = childNode;
            tags.Add(tag);
            return tag;
        }

        public void Delete(Tag tag)
        {
            var node = fullyQualifiedIdDictionary[tag.FullyQualifiedId];
            node.Parent.RemoveChild(node);
            fullyQualifiedIdDictionary.Remove(tag.FullyQualifiedId);
            tags.Remove(tag);
        }

        public bool Exists(Service service, Tag tag)
        {
            return tags.Contains(tag);
        }

        public void Add(Service service, BubbleGroup bubbleGroup, IEnumerable<Tag> tags)
        {
            foreach (var tag in tags)
            {
                var node = fullyQualifiedIdDictionary[tag.FullyQualifiedId];
                var conversation = new Conversation()
                {
                    Id = bubbleGroup.Address,
                };
                node.Value.Add(conversation);
            }
        }

        public void Remove(Service service, BubbleGroup bubbleGroup, IEnumerable<Tag> tags)
        {
            foreach (var tag in tags)
            {
                var node = fullyQualifiedIdDictionary[tag.FullyQualifiedId];
                var conversation = new Conversation()
                {
                    Id = bubbleGroup.Address,
                };
                node.Value.Remove(conversation);
            }
        }

        public HashSet<Tag> GetAllTags()
        {
            return tags;
        }

        public HashSet<Tag> GetAllServiceTags(Service service)
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

        public HashSet<BubbleGroup> GetAllBubbleGroups(IEnumerable<Tag> tags)
        {
            var conversationIds = new HashSet<string>();
            foreach (var tag in tags)
            {
                var node = fullyQualifiedIdDictionary[tag.FullyQualifiedId];
                if (node == null)
                {
                    continue;
                }
                var tagConversations = node.EnumerateAllDescendantsAndSelfData().SelectMany(list => list);
                var tagConversationIds = tagConversations.Select(c => c.Id).ToHashSet();
                conversationIds.UnionWith(tagConversationIds);
            }

            var bubbleGroups = BubbleGroupManager.FindAll((BubbleGroup bg) => conversationIds.Contains(bg.Address)).ToHashSet();
            return bubbleGroups;
        }

        public void PrintHierarchy()
        {
            tree.PrintToString();
        }

        public void PrintAllTags()
        {
            foreach (var tag in tags)
            {
                Console.WriteLine($"{tag}");
            }
        }
    }
}
