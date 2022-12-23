using System.Collections.Generic;

namespace MrWhimble.RailwayMaker
{
    public class Node
    {
        public bool isNodeA;
        public RailNode railNode;
        public List<Node> toNodes;

        public Node(RailNode rn, bool i)
        {
            isNodeA = i;
            railNode = rn;
            toNodes = new List<Node>();
        }

        public void AddNode(Node other)
        {
            if (toNodes.Contains(other))
                return;
            
            toNodes.Add(other);
        }

        public void RemoveNode(Node other)
        {
            if (!toNodes.Contains(other))
                return;

            toNodes.Remove(other);
        }
    }
}