using UnityEngine;

namespace MrWhimble.RailwayMaker.Routing
{
    public interface IWaypoint
    {
        GameObject gameObject { get; }
        
        int RailNodeIndex { get; set; }

        bool IsStoppedAt { get; }

        string Name { get; set; }
    }
}