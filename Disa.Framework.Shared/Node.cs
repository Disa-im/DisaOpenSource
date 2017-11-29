using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace Disa.Framework
{
    [ProtoContract]
    public class Node<K, V> where V : new()
    {
        [ProtoMember(1)]
        private readonly string name;
        [ProtoMember(2)]
        private readonly K key;
        [ProtoMember(3)]
        private readonly V value;
        [ProtoMember(4)]
        private readonly List<Node<K, V>> children = new List<Node<K, V>>();
        [ProtoMember(5)]
        private readonly Dictionary<K, Node<K, V>> idChildren = new Dictionary<K, Node<K, V>>();

        public string Name
        {
            get => name;
        }
        public K Key
        {
            get => key;
        }
        public V Value
        {
            get => value;
        }
        public int Height { get; set; }

        [ProtoMember(6, AsReference = true)]
        public Node<K, V> Parent { get; set; }
        public IReadOnlyList<Node<K, V>> Children { get => children.AsReadOnly(); }
        public Dictionary<K, Node<K, V>> ChildrenDictionary { get => idChildren; }

        public Node()
        {
        }

        public Node(K key, Node<K, V> parent)
        {
            this.key = key;
            this.value = new V();
            Parent = parent;
        }

        public Node(K key, V data, Node<K, V> parent)
        {
            this.key = key;
            this.value = data;
            Parent = parent;
        }

        public void AddChild(Node<K, V> node)
        {
            children.Add(node);
            idChildren[node.Key] = node;
        }

        public void RemoveChild(Node<K, V> node)
        {
            children.Remove(node);
            idChildren.Remove(node.Key);
        }

        public HashSet<Node<K, V>> EnumerateAllDescendantsAndSelf()
        {
            var set = new HashSet<Node<K, V>>() { this };
            foreach (var child in children)
            {
                set.UnionWith(child.EnumerateAllDescendantsAndSelf());
            }
            return set;
        }

        public HashSet<V> EnumerateAllDescendantsAndSelfData()
        {
            var descendants = EnumerateAllDescendantsAndSelf();
            var set = new HashSet<V>();
            foreach (var descendant in descendants)
            {
                set.Add(descendant.Value);
            }
            return set;
        }

        public void Print(string padding)
        {
            foreach (var child in children)
            {
                Console.WriteLine($"{padding}|");
                Console.WriteLine($"{padding}----{child.Key}");
                child.Print($"{padding}    ");
            }
        }

        public bool Equals(Node<K, V> otherNode)
        {
            if (otherNode == null)
            {
                return false;
            }
            return value.Equals(otherNode.Value);
        }

        public override bool Equals(object obj)
        {
            var otherNode = obj as Node<K, V>;
            if (otherNode == null)
            {
                return false;
            }
            return Value.Equals(otherNode.Value);
        }

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                hash = hash * 23 + key.GetHashCode();
                return hash;
            }
        }
    }
}
