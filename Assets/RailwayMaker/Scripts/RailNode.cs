using UnityEngine;

namespace MrWhimble.RailwayMaker
{
    [System.Serializable]
    public class RailNode
    {
        
        public AnchorPoint anchor;
        public Node a;
        public Node b;

        public RailNode(AnchorPoint ap)
        {
            anchor = ap;
            a = new Node(this, true);
            b = new Node(this, false);
        }
    }
}