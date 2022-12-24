using System.Collections.Generic;
using UnityEngine;

namespace MrWhimble.RailwayMaker.Graph
{
    [System.Serializable]
    public class RailNode
    {
        /*
        public AnchorPoint anchor;
        public Node a;
        public Node b;

        public RailNode(AnchorPoint ap)
        {
            anchor = ap;
            a = new Node(this, true);
            b = new Node(this, false);
        }*/

        public AnchorPoint anchor;
        public Vector3 direction;

        public Node nodeA;
        public Node nodeB;

        public int index;
        //public List<Neighbour> neighbours;

        public RailNode()
        {
            direction = Vector3.zero;
            //nodeA = new Node(this, true);
            //nodeB = new Node(this, false);
            nodeA = new Node(true);
            nodeA.railNode = this;
            nodeB = new Node(false);
            nodeB.railNode = this;
            //neighbours = new List<Neighbour>();
        }

        public Node GetNode(bool isNodeA)
        {
            return isNodeA ? nodeA : nodeB;
        }
    }
}