using UnityEngine;

namespace MrWhimble.RailwayMaker
{
    public class Station : MonoBehaviour, IWaypoint
    {
        GameObject IWaypoint.gameObject => this.gameObject;
        
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
        }

        [SerializeField] private string stationName;
        public string Name
        {
            get => string.IsNullOrWhiteSpace(stationName) ? name : stationName;
            set => stationName = value;
        }
    }
}