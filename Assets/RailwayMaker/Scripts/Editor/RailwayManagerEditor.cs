using System;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MrWhimble.RailwayMaker
{
    [CustomEditor(typeof(RailwayManager))]
    public class RailwayManagerEditor : Editor
    {
        private RailwayManager manager;

        private SerializedProperty nodesProp;

        private void OnEnable()
        {
            manager = (RailwayManager) target;

            nodesProp = serializedObject.FindProperty("nodes");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Add New Curve"))
            {
                AddCurve();
            }
        }

        private void OnSceneGUI()
        {
            manager = (RailwayManager) target;

            if (manager == null || manager.nodes == null)
                return;
            
            if (nodesProp.arraySize != 0)
            {
                for (int i = 0; i < nodesProp.arraySize; i++)
                {
                    Vector3 pos = manager.nodes[i].position;
                    Handles.DrawLine(pos, pos + (manager.nodes[i].rotation * Vector3.up));
                }
            }
        }

        private void AddCurve()
        {
            int index = nodesProp.arraySize;
            nodesProp.InsertArrayElementAtIndex(index);
            RailNode n = new RailNode()
            {
                position = Random.insideUnitSphere * 10f
            };
            manager.nodes[index-1] = n;
        }
    }
}