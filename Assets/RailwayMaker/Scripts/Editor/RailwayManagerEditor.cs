using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MrWhimble.RailwayMaker
{
    [CustomEditor(typeof(RailwayManager))]
    public class RailwayManagerEditor : Editor
    {
        private RailwayManager manager;

        //private SerializedProperty railwayNetworkProp;

        //private SerializedProperty pointsProp;

        public void OnEnable()
        {
            /*
            serializedObject.Update();
            
            railwayNetworkProp = serializedObject.FindProperty("railwayNetwork");

            if (railwayNetworkProp.managedReferenceValue == null)
                railwayNetworkProp.managedReferenceValue = new RailwayNetwork();

            Debug.Log(railwayNetworkProp);
            pointsProp = railwayNetworkProp.FindPropertyRelative("points");
            Debug.Log(pointsProp);

            serializedObject.ApplyModifiedProperties();
            */

            manager = (RailwayManager) target;

            if (manager.railwayNetwork == null)
                manager.railwayNetwork = new RailwayNetwork();
            
            EditorUtility.SetDirty(manager);
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();

            if (GUILayout.Button("Reset"))
            {
                manager.railwayNetwork = new RailwayNetwork();
            
                EditorUtility.SetDirty(manager);
            }
        }

        private void OnSceneGUI()
        {
            //if (railwayNetworkProp.managedReferenceValue == null)
            //    return;

            //serializedObject.Update();
            
            bool changed = false;

            /*
            int pointsCount = pointsProp.arraySize;
            
            //for (int i = 0; i < pointsCount; i++)
            //{
                //SerializedProperty sp = pointsProp.GetArrayElementAtIndex(i);
                //Point p = (Point) sp.managedReferenceValue;

                //bool updatePoint = false;
                
                
                switch (p)
                {
                    case AnchorPoint anchor:
                    {
                        Handles.color = Color.blue;
                        handlePos = Handles.FreeMoveHandle(anchor.position, anchor.rotation, 0.5f, Vector3.zero, Handles.SphereHandleCap);
                        if (anchor.position != handlePos)
                        {
                            anchor.UpdatePosition(handlePos);
                            updatePoint = true;
                        }
                        break;
                    }

                    case ControlPoint control:
                    {
                        Handles.color = Color.red;
                        handlePos = Handles.FreeMoveHandle(control.position, Quaternion.identity, 0.5f, Vector3.zero, Handles.SphereHandleCap);
                        if (control.position != handlePos)
                        {
                            control.UpdatePosition(handlePos);
                            updatePoint = true;
                        }
                        break;
                    }
                }

                
                sp.managedReferenceValue = p;
            }
            

            serializedObject.ApplyModifiedProperties();
            */
                
            
            foreach (Point p in manager.railwayNetwork.points)
            {
                Vector3 handlePos;
                switch (p)
                {
                    case AnchorPoint anchor:
                    {
                        Handles.color = Color.blue;
                        //handlePos = Handles.FreeMoveHandle(anchor.position, anchor.rotation, 0.5f, Vector3.zero, Handles.SphereHandleCap);
                        handlePos = Handles.PositionHandle(anchor.position, anchor.rotation);
                        if (anchor.position != handlePos)
                        {
                            anchor.UpdatePosition(handlePos);
                            changed = true;
                        }
                        break;
                    }
                    case ControlPoint control:
                    {
                        Handles.color = Color.red;
                        //handlePos = Handles.FreeMoveHandle(control.position, Quaternion.identity, 0.5f, Vector3.zero, Handles.SphereHandleCap);
                        handlePos = Handles.PositionHandle(control.position, Quaternion.identity);
                        if (control.position != handlePos)
                        {
                            control.UpdatePosition(handlePos);
                            changed = true;
                        }
                        break;
                    }
                }
            } 
            
            if (changed)
            {
                EditorUtility.SetDirty(manager);
            }
                
            Handles.DrawBezier(
                manager.railwayNetwork.points[0].position, 
                manager.railwayNetwork.points[3].position, 
                manager.railwayNetwork.points[1].position, 
                manager.railwayNetwork.points[2].position, 
                Color.magenta, null, 2);

            float dist = HandleUtility.DistancePointBezier(new Vector3(2, 0, 0), 
                manager.railwayNetwork.points[0].position, 
                manager.railwayNetwork.points[3].position, 
                manager.railwayNetwork.points[1].position, 
                manager.railwayNetwork.points[2].position);
                
            //Handles.SphereHandleCap(-1, new Vector3(2, 0, 0), Quaternion.identity, dist*2f, EventType.Repaint);
            
            
        }
    }
}