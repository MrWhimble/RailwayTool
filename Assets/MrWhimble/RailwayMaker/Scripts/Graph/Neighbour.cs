using UnityEngine;

namespace MrWhimble.RailwayMaker.Graph
{
    public class Neighbour
    {
        public RailNode railNode;
        public Vector3 leavingDir;
        public Vector3 enteringDir;
        public float distance;
        public Node node;
        
        /// <param name="n">Neighbour RailNode</param>
        /// <param name="f">is flipped</param>
        /// <param name="dist">distance to RailNode</param>
        /// <param name="lDir">leaving direction</param>
        /// <param name="eDir">entering direction</param>
        public Neighbour(RailNode rn, Node n, float dist, Vector3 lDir, Vector3 eDir)
        {
            railNode = rn;
            node = n;
            distance = dist;
            leavingDir = lDir;
            enteringDir = eDir;
        }
    }
}