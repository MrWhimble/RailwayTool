using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MrWhimble.RailwayMaker
{
    public interface IWaypoint
    {
        GameObject gameObject { get; }
        int CurveIndex { get; set; }
        float RatioAlongCurve { get; set; }
        
        string Name { get; set; }
    }
}