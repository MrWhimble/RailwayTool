using UnityEngine;

namespace MrWhimble.RailwayMaker.Routing
{
    public class Station : MonoBehaviour, IWaypoint
    {
        GameObject IWaypoint.gameObject => this.gameObject;
        
        [SerializeField, HideInInspector] private int _railNodeIndex;
        public int RailNodeIndex
        {
            get => _railNodeIndex;
            set => _railNodeIndex = value;
        }
        
        public bool IsStoppedAt => true;

        //[SerializeField] private string stationName;
        public string Name
        {
            //get => string.IsNullOrWhiteSpace(waypointName) ? name : stationName;
            get => name;
            set => name = value;
        }
    }
}