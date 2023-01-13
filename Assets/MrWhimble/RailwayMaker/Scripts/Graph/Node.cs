using System.Collections.Generic;
using UnityEngine;

namespace MrWhimble.RailwayMaker.Graph
{
    public class Node
    {
        public bool isNodeA;
        public RailNode railNode;
        public List<Neighbour> neighbours;
        
        /// <param name="i">Is node A</param>
        public Node(bool i)
        {
            isNodeA = i;
            neighbours = new List<Neighbour>();
        }

        
        public void AddNeighbour(Neighbour other)
        {
            if (neighbours.Contains(other))
                return;
            neighbours.Add(other);
        }

        public void RemoveNeighbour(Neighbour other)
        {
            if (!neighbours.Contains(other))
                return;
            neighbours.Remove(other);
        }

        public Vector3 GetPosition()
        {
            if (isNodeA)
            {
                return railNode.anchor.position + (railNode.anchor.rotation * Vector3.right * 0.5f);
            }
            return railNode.anchor.position - (railNode.anchor.rotation * Vector3.right * 0.5f);
        }
    }
}