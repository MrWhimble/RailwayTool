using System.Collections.Generic;
using MrWhimble.RailwayMaker.Graph;
using UnityEditor;
using UnityEngine;

namespace MrWhimble.RailwayMaker.Routing
{
    [CustomEditor(typeof(RoutingTable))]
    public class RoutingTableEditor : Editor
    {
        private RoutingTable table;
        private RailwayNetwork network;
        private List<RailwayRoute> routes;
        
        private void OnEnable()
        {
            table = target as RoutingTable;
            network = new RailwayNetwork(GameObject.FindObjectOfType<RailwayManager>());
            
            UpdateRoutes();
            
            SceneView.duringSceneGui += OnDuringSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnDuringSceneGUI;
        }

        public override void OnInspectorGUI()
        {
            if (DrawDefaultInspector())
            {
                // Changed -> update route

                UpdateRoutes();
            }
        }

        private void UpdateRoutes()
        {
            if (table.elements.Count < 2)
            {
                routes = new List<RailwayRoute>();
                return;
            }
                
            var element = table.elements[0];
            var waypoint = network.Waypoints[element.waypointName];
            var node = network.GetNodeAtIndex(waypoint.RailNodeIndex, element.side);

            Vector3 direction = node.railNode.direction;// * (table.reverseDirection ? -1f : 1f);
                
            int count = table.elements.Count;
            routes = new List<RailwayRoute>(count);
            for (int i = 0; i < count; i++)
            {
                int startIndex = i;
                int endIndex = (i + 1) % count;

                var startElement = table.elements[startIndex];
                var startWaypoint = network.Waypoints[startElement.waypointName];
                var startNode = network.GetNodeAtIndex(startWaypoint.RailNodeIndex, startElement.side);
                    
                var endElement = table.elements[endIndex];
                var endWaypoint = network.Waypoints[endElement.waypointName];
                var endNode = network.GetNodeAtIndex(endWaypoint.RailNodeIndex, endElement.side);

                RailwayRoute route = new RailwayRoute();
                var state = network.GetRoute(startNode, direction, endNode, ref route);
                if (state == RailwayNetwork.RouteState.Failed)
                {
                    Debug.LogError($"Failed to get route between index:{startIndex} and index:{endIndex}");
                    return;
                }
                    
                routes.Add(route);

                direction = endNode.railNode.direction * (endNode.isNodeA ? 1f : -1f);
            }
        }

        private void OnDuringSceneGUI(SceneView obj)
        {
            if (routes == null || routes.Count < 2)
                return;


            Color[] colors = new[] { Color.red, Color.yellow, Color.green, Color.cyan, Color.blue, Color.magenta };
            for (var i = 0; i < routes.Count; i++)
            {
                var r = routes[i];
                if (r == null || !r.HasRoute)
                    return;

                Vector3 offset = new Vector3(0, i * 0.025f, 0);
                Handles.color = colors[i % colors.Length];
                for (int j = 0; j < r.sections.Count - 1; j++)
                {
                    //Gizmos.DrawLine(r.sections[j].node.GetPosition(), r.sections[j+1].node.GetPosition());
                    Handles.DrawLine(r.sections[j].node.GetPosition() + offset, r.sections[j + 1].node.GetPosition() + offset, 1f);
                }
            }
        }
    }
}