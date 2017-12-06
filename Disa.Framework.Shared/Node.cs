using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace Disa.Framework
{
    [ProtoContract]
    public class Node<T> where T : new()
    {
        
        [ProtoMember(1)]
        public string Name
        {
            get; set;
        }
        [ProtoMember(2)]
        public T Data
        {
            get; set;
        }
        public int Height { get; set; }

        //[ProtoMember(4, AsReference = true)]
        public Node<T> Parent { get; set; }
        [ProtoMember(5)]
        public List<Node<T>> Children { get; set; } = new List<Node<T>>();
        [ProtoMember(6)]
        public Dictionary<T, Node<T>> ChildrenDictionary { get; set; } = new Dictionary<T, Node<T>>();
        
        public Node()
        {
        }

        public Node(T data, Node<T> parent)
        {
            this.Data = data;
            Parent = parent;
        }

        public void AddChild(Node<T> node)
        {
            Children.Add(node);
            ChildrenDictionary[node.Data] = node;
        }

        public void RemoveChild(Node<T> node)
        {
            Children.Remove(node);
            ChildrenDictionary.Remove(node.Data);
        }

        public HashSet<Node<T>> EnumerateAllDescendantsAndSelf()
        {
            var set = new HashSet<Node<T>>() { this };
            foreach (var child in Children)
            {
                set.UnionWith(child.EnumerateAllDescendantsAndSelf());
            }
            return set;
        }
        
        public HashSet<T> EnumerateAllDescendantsAndSelfData()
        {
            var descendants = EnumerateAllDescendantsAndSelf();
            var set = new HashSet<T>();
            foreach (var descendant in descendants)
            {
                set.Add(descendant.Data);
            }
            return set;
        }

        public void Print(StringBuilder builder, string padding)
        {
            foreach (var child in Children)
            {
                builder.AppendLine($"{padding}|");
                builder.AppendLine($"{padding}----{child.Data}");
                child.Print(builder, $"{padding}    ");
            }
        }

        public string PrintToString(string padding)
        {
            var builder = new StringBuilder();
            foreach (var child in Children)
            {
                builder.AppendLine($"{padding}|");
                builder.AppendLine($"{padding}----{child.Data}");
                child.Print(builder, $"{padding}    ");
            }
            return builder.ToString();
        }

        public bool Equals(Node<T> otherNode)
        {
            if (otherNode == null)
            {
                return false;
            }
            return Data.Equals(otherNode.Data);
        }

        public override bool Equals(object obj)
        {
            var otherNode = obj as Node<T>;
            if (otherNode == null)
            {
                return false;
            }
            return Data.Equals(otherNode.Data);
        }

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                hash = hash * 23 + Data.GetHashCode();
                return hash;
            }
        }
    }
}
