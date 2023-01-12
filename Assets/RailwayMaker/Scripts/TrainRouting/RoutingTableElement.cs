using UnityEngine;

namespace MrWhimble.RailwayMaker.Routing
{
    public enum LeaveCondition
    {
        AfterTime,
        AfterEvent
    }
    
    [System.Serializable]
    public class RoutingTableElement
    {
        public string waypointName;
        public bool side;
        public LeaveCondition leaveCondition;
        public float waitTime;
    }
}