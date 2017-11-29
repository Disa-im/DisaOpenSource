using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Disa.Framework
{
    public class TreeManager
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

        private static readonly string rootName = string.Empty;
        private readonly HashSet<Tag> tags = new HashSet<Tag>();
        private readonly Tree<Tag, HashSet<Conversation>> tree = new Tree<Tag, HashSet<Conversation>>(new Tag() { Id = rootName, Name = rootName, });
        private readonly Dictionary<Service, Node<Tag, HashSet<Conversation>>> serviceRoots =
            new Dictionary<Service, Node<Tag, HashSet<Conversation>>>();
        private readonly Dictionary<string, Node<Tag, HashSet<Conversation>>> pathDictionary =
            new Dictionary<string, Node<Tag, HashSet<Conversation>>>();

        public TreeManager()
        {

        }

        public Tag CreateNewPlugin(Service service)
        {
            var tag = new Tag()
            {
                Id = service.Information.ServiceName,
                Name = service.Information.ServiceName,
                Service = service,
                Path = $"{service.Information.ServiceName}",
            };

            var pluginRoot = new Node<Tag, HashSet<Conversation>>(tag, tree.Root);
            serviceRoots[service] = pluginRoot;
            pathDictionary[tag.Path] = pluginRoot;
            tree.Root.AddChild(pluginRoot);
            tags.Add(tag);
            //service.RootTag = tag;
            return tag;
        }
        
        public Tag Create(Tag tag)
        {
            if (tag.Parent == null || tag.Parent == tag)
            {
                throw new ArgumentException(nameof(Tag.Parent));
            }

            var fullTag = $"{tag.Service.Information.ServiceName}/{tag}";
            tag.Path = fullTag;

            if (tags.Contains(tag))
            {
                return null;
            }

            if (!pathDictionary.ContainsKey(tag.Parent.Path))
            {
                throw new ArgumentException($"{nameof(Tag.Parent)}");
            }
            var parentNode = pathDictionary[tag.Parent.Path];
            var childNode = new Node<Tag, HashSet<Conversation>>(tag, parentNode);
            parentNode.AddChild(childNode);
            pathDictionary[tag.Path] = childNode;
            tags.Add(tag);
            return tag;
        }

        public void Delete(Tag tag)
        {
            var node = pathDictionary[tag.Path];
            node.Parent.RemoveChild(node);
            pathDictionary.Remove(tag.Path);
            tags.Remove(tag);
        }

        //public bool Exists(Plugin plugin, Tag tag)
        //{
        //    var fullTag = $"{plugin.Name}/{tag}";
        //    return tags.Contains(fullTag);
        //}

        public void Add(Service service, BubbleGroup bubbleGroup, IEnumerable<Tag> tags)
        {
            foreach (var tag in tags)
            {
                var node = pathDictionary[tag.Path];
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
                var node = pathDictionary[tag.Path];
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

        public HashSet<BubbleGroup> GetAllBubbleGroups(IEnumerable<Tag> tags)
        {
            var conversationIds = new HashSet<string>();
            foreach (var tag in tags)
            {
                var node = pathDictionary[tag.Path];
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
            tree.Print();
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
