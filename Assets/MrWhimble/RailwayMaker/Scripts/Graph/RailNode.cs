using UnityEngine;

namespace MrWhimble.RailwayMaker.Graph
{
    [System.Serializable]
    public class RailNode
    {
        public AnchorPoint anchor;
        public Vector3 direction;

        public Node nodeA;
        public Node nodeB;

        public int index;

        public RailNode()
        {
            direction = Vector3.zero;
            nodeA = new Node(true);
            nodeA.railNode = this;
            nodeB = new Node(false);
            nodeB.railNode = this;
        }

        public Node GetNode(bool isNodeA)
        {
            return isNodeA ? nodeA : nodeB;
        }
    }
}