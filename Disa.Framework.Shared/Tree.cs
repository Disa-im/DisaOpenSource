using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace Disa.Framework
{
    [ProtoContract]
    public class Tree<K, V> where V : new()
    {
        [ProtoMember(1)]
        private readonly Node<K, V> rootNode;
        public Node<K, V> Root { get => rootNode; }

        public Tree(K key)
        {
            rootNode = new Node<K, V>(key, null);
        }

        public Tree(K key, V rootData)
        {
            rootNode = new Node<K, V>(key, rootData, null);
        }

        public string PrintToString()
        {
            var builder = new StringBuilder();
            builder.AppendLine($"{rootNode.Key}");
            rootNode.Print(builder, "");
            return builder.ToString();
        }
    }
}
