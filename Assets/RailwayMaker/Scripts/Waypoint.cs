using UnityEngine;

namespace MrWhimble.RailwayMaker
{
    public class Waypoint : MonoBehaviour, IWaypoint
    {
        private int _curveIndex;

        public int CurveIndex
        {
            get => _curveIndex;
            set { }
        }
        
        private float _ratioAlongCurve;
        public float RatioAlongCurve
        {
            get => _ratioAlongCurve;
            set => _ratioAlongCurve = Mathf.Clamp01(value);
        }
        
    }

    public interface IWaypoint
    {
        int CurveIndex
        {
            get;
            set;
        }

        float RatioAlongCurve
        {
            get;
            set;
        }
    }
}