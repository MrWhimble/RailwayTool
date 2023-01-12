using System.Collections.Generic;
using UnityEngine;

namespace MrWhimble.RailwayMaker.Routing
{
    [CreateAssetMenu(fileName = "new RoutingTable", menuName = "MrWhimble/Routing Table", order = 0)]
    public class RoutingTable : ScriptableObject
    {
        public List<RoutingTableElement> elements;
    }
}