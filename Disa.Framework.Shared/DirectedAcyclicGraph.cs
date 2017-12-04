using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace Disa.Framework
{
    [ProtoContract]
    public class DirectedAcyclicGraph<K, V> where V : new()
    {
        [ProtoMember(1)]
        private readonly Node<K, V> rootNode;
        public Node<K, V> Root { get => rootNode; }

        public DirectedAcyclicGraph()
        {
            rootNode = new Node<K, V>(default(K), null);
        }

        public DirectedAcyclicGraph(K key)
        {
            rootNode = new Node<K, V>(key, null);
        }

        public DirectedAcyclicGraph(K key, V rootData)
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
