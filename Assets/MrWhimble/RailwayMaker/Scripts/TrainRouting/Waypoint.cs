using UnityEngine;

namespace MrWhimble.RailwayMaker.Routing
{
    public class Waypoint : MonoBehaviour, IWaypoint
    {
        GameObject IWaypoint.gameObject => this.gameObject;
        
        [SerializeField, HideInInspector] private int _railNodeIndex;
        public int RailNodeIndex
        {
            get => _railNodeIndex;
            set => _railNodeIndex = value;
        }

        public bool IsStoppedAt => false;
        
        //[SerializeField] private string waypointName;
        public string Name
        {
            //get => string.IsNullOrWhiteSpace(waypointName) ? name : waypointName;
            get => name;
            set => name = value;
        }
    }
}