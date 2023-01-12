using System;
using UnityEngine;

namespace MrWhimble.RailwayMaker.Routing
{
    public class Waypoint : MonoBehaviour, IWaypoint
    {
        GameObject IWaypoint.gameObject => this.gameObject;
        
        /*
        [SerializeField, HideInInspector] private int _curveIndex;
        public int CurveIndex
        {
            get => _curveIndex;
            set => _curveIndex = value;
        }
        
        [SerializeField, HideInInspector] private float _ratioAlongCurve;
        public float RatioAlongCurve
        {
            get => _ratioAlongCurve;
            set => _ratioAlongCurve = Mathf.Clamp01(value);
        }*/

        [SerializeField, HideInInspector] private int _railNodeIndex;

        public int RailNodeIndex
        {
            get => _railNodeIndex;
            set => _railNodeIndex = value;
        }

        public bool IsStoppedAt => false;
        
        [SerializeField] private string waypointName;
        public string Name
        {
            get => string.IsNullOrWhiteSpace(waypointName) ? name : waypointName;
            set => waypointName = value;
        }
    }
}