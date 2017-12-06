using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace Disa.Framework
{
    [ProtoContract]
    public class DirectedAcyclicGraph<T> where T : new()
    {
        [ProtoMember(1)]
        private readonly Node<T> rootNode;
        public Node<T> Root { get => rootNode; }

        public DirectedAcyclicGraph()
        {
            rootNode = new Node<T>(default(T), null);
        }

        public DirectedAcyclicGraph(T data)
        {
            rootNode = new Node<T>(data, null);
        }

        public string PrintToString()
        {
            var builder = new StringBuilder();
            builder.AppendLine($"{rootNode.Data}");
            rootNode.Print(builder, "");
            return builder.ToString();
        }
    }
}
