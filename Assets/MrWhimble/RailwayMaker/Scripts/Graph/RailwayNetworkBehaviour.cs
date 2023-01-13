using UnityEngine;

namespace MrWhimble.RailwayMaker.Graph
{
    [RequireComponent(typeof(RailwayManager))]
    public class RailwayNetworkBehaviour : MonoBehaviour
    {
        public RailwayNetwork railwayNetwork;

        private void Awake()
        {
            railwayNetwork = new RailwayNetwork(GetComponent<RailwayManager>());
        }
    }
}