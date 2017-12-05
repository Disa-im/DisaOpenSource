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
        public string Name
        {
            get; set;
        }
        [ProtoMember(2)]
        public K Key
        {
            get; set;
        }
        //[ProtoMember(3)]
        public V Value
        {
            get; set;
        }
        public int Height { get; set; }

        //[ProtoMember(4, AsReference = true)]
        public Node<K, V> Parent { get; set; }
        [ProtoMember(5)]
        public List<Node<K, V>> Children { get; set; } = new List<Node<K, V>>();
        [ProtoMember(6)]
        public Dictionary<K, Node<K, V>> ChildrenDictionary { get; set; } = new Dictionary<K, Node<K, V>>();
        
        public Node()
        {
        }

        public Node(K key, Node<K, V> parent)
        {
            this.Key = key;
            this.Value = new V();
            Parent = parent;
        }

        public Node(K key, V data, Node<K, V> parent)
        {
            this.Key = key;
            this.Value = data;
            Parent = parent;
        }

        public void AddChild(Node<K, V> node)
        {
            Children.Add(node);
            ChildrenDictionary[node.Key] = node;
        }

        public void RemoveChild(Node<K, V> node)
        {
            Children.Remove(node);
            ChildrenDictionary.Remove(node.Key);
        }

        public HashSet<Node<K, V>> EnumerateAllDescendantsAndSelf()
        {
            var set = new HashSet<Node<K, V>>() { this };
            foreach (var child in Children)
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

        public void Print(StringBuilder builder, string padding)
        {
            foreach (var child in Children)
            {
                builder.AppendLine($"{padding}|");
                builder.AppendLine($"{padding}----{child.Key}");
                child.Print(builder, $"{padding}    ");
            }
        }

        public string PrintToString(string padding)
        {
            var builder = new StringBuilder();
            foreach (var child in Children)
            {
                builder.AppendLine($"{padding}|");
                builder.AppendLine($"{padding}----{child.Key}");
                child.Print(builder, $"{padding}    ");
            }
            return builder.ToString();
        }

        public bool Equals(Node<K, V> otherNode)
        {
            if (otherNode == null)
            {
                return false;
            }
            return Value.Equals(otherNode.Value);
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
                hash = hash * 23 + Key.GetHashCode();
                return hash;
            }
        }
    }
}
