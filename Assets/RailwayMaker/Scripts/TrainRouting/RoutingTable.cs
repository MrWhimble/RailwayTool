using System.Collections.Generic;
using UnityEngine;

namespace MrWhimble.RailwayMaker
{
    [CreateAssetMenu(fileName = "new RoutingTable", menuName = "MrWhimble/Routing Table", order = 0)]
    public class RoutingTable : ScriptableObject
    {
        //public int element;
        public List<RoutingTableElement> elements;
    }
}