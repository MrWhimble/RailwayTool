using System.Collections;
using System.Collections.Generic;
using System.Transactions;
using MrWhimble.ConstantConsole;
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