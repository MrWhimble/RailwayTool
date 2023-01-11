using System;
using MrWhimble.RailwayMaker.Graph;
using UnityEditor;
using UnityEngine;

namespace MrWhimble.RailwayMaker
{
    [CustomEditor(typeof(RoutingTable))]
    public class RoutingTableEditor : Editor
    {
        private RoutingTable table;
        private RailwayNetwork network;
        private RailwayRoute route;
        
        private void OnEnable()
        {
            table = target as RoutingTable;
            network = new RailwayNetwork(GameObject.FindObjectOfType<RailwayManager>());
            
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

                //network.GetRoute(table.elements[0], table.elements[^1], ref route);
            }
        }

        private void OnDuringSceneGUI(SceneView obj)
        {
            
        }
    }
}