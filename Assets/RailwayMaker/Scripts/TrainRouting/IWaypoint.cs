using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MrWhimble.RailwayMaker.Routing
{
    public interface IWaypoint
    {
        GameObject gameObject { get; }
        //int CurveIndex { get; set; }
        //float RatioAlongCurve { get; set; }
        int RailNodeIndex { get; set; }

        bool IsStoppedAt { get; }

        string Name { get; set; }
    }
}