using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MrWhimble.RailwayMaker
{
    [CustomEditor(typeof(RailwayManager))]
    public class RailwayManagerEditor : Editor
    {
        private SerializedProperty railPathProp;
        private SerializedObject railPathObj;

        private List<Point> points;


        private void OnEnable()
        {
            Debug.Log("OnEnable");

            railPathProp = serializedObject.FindProperty("pathData");
            railPathObj = new SerializedObject(railPathProp.objectReferenceValue);

            if (railPathProp == null)
            {
                Debug.Log("railPathProp == null");
                return;
            }
                

            points = ((RailPathData)railPathObj.targetObject).GetPoints();

            Debug.Log(points.Count);

            if (points.Count != 0)
                return;
            points.Add(new AnchorPoint());
            points.Add(new ControlPoint());
            points.Add(new ControlPoint());
            points.Add(new AnchorPoint());

            ((ControlPoint)points[2]).flipped = true;

            ((AnchorPoint)points[0]).AddControlPoint((ControlPoint)points[1]);
            ((AnchorPoint)points[3]).AddControlPoint((ControlPoint)points[2]);

            ((AnchorPoint)points[0]).UpdatePosition(new Vector3(0, 0, 0));
            ((AnchorPoint)points[3]).UpdatePosition(new Vector3(4, 0, 0));

            ((ControlPoint)points[1]).UpdatePosition(new Vector3(1, 1, 0));
            ((ControlPoint)points[2]).UpdatePosition(new Vector3(3, -1, 0));

            Debug.Log(points.Count);
        }

        private void OnSceneGUI()
        {
            Vector3 handlePos;
            for (int i = 0; i < points.Count; i++)
            {
                switch (points[i])
                {
                    case AnchorPoint anchor:
                        {
                            Handles.color = Color.blue;
                            handlePos = Handles.PositionHandle(anchor.position, anchor.rotation);
                            if (anchor.position != handlePos)
                            {
                                anchor.UpdatePosition(handlePos);
                                //updatePoint = true;
                            }
                            break;
                        }

                    case ControlPoint control:
                        {
                            Handles.color = Color.red;
                            handlePos = Handles.PositionHandle(control.position, Quaternion.identity);
                            if (control.position != handlePos)
                            {
                                control.UpdatePosition(handlePos);
                                //updatePoint = true;
                            }
                            break;
                        }
                }
            }

            Handles.DrawBezier(
            points[0].position,
            points[3].position,
            points[1].position,
            points[2].position,
            Color.magenta, null, 2);
        }

        private void OnDisable()
        {
            Debug.Log("OnDisable");

            railPathObj.Update();

            //SerializedProperty railPathPointsProp = railPathProp.FindPropertyRelative("pathPoints");
            SerializedProperty railPathPointsProp = railPathObj.GetIterator();
            do
            {
                if (railPathPointsProp.name == "pathPoints")
                {
                    Debug.Log(railPathPointsProp.name);
                    break;
                }
            } while (railPathPointsProp.Next(true));

            //SerializedProperty railPathPointsProp = railPathObj.FindProperty("pathPoints");

            if (railPathPointsProp == null)
            {
                Debug.Log("railPathPointsProp == null");
                railPathPointsProp.managedReferenceValue = new List<PathPoint>();
            }
                
            
            railPathPointsProp.ClearArray();

            for (int i = 0; i < points.Count; i++)
            {
                railPathPointsProp.InsertArrayElementAtIndex(i);
                SerializedProperty pointProp = railPathPointsProp.GetArrayElementAtIndex(i);
                SerializedProperty pointPointTypeProp = pointProp.FindPropertyRelative("pointType");
                SerializedProperty pointPositionProp = pointProp.FindPropertyRelative("position");
                SerializedProperty pointRotationProp = pointProp.FindPropertyRelative("rotation");
                SerializedProperty pointFlippedProp = pointProp.FindPropertyRelative("flipped");
                SerializedProperty pointConnectedPointsProp = pointProp.FindPropertyRelative("connectedPoints");

                switch (points[i])
                {
                    case AnchorPoint p:
                    {
                        pointPointTypeProp.enumValueIndex = 0;
                            pointPositionProp.vector3Value = p.position;
                            pointRotationProp.quaternionValue = p.rotation;
                            pointConnectedPointsProp.ClearArray();
                            for (int j = 0; j < p.controlPoints.Count; j++)
                            {
                                pointConnectedPointsProp.InsertArrayElementAtIndex(j);
                                SerializedProperty pointConnectedPointsIndexProp = pointConnectedPointsProp.GetArrayElementAtIndex(0);
                                pointConnectedPointsIndexProp.intValue = points.IndexOf(p.controlPoints[j]);
                            }

                            break;
                    }
                    case ControlPoint p:
                    {
                        pointPointTypeProp.enumValueIndex = 1;
                            pointPositionProp.vector3Value = p.position;
                            pointFlippedProp.boolValue = p.flipped;
                            pointConnectedPointsProp.ClearArray();
                            pointConnectedPointsProp.InsertArrayElementAtIndex(0);
                            SerializedProperty pointConnectedPointsIndexProp = pointConnectedPointsProp.GetArrayElementAtIndex(0);
                            pointConnectedPointsIndexProp.intValue = points.IndexOf(p.anchorPoint);
                            break;
                    }
                }
            }

            railPathObj.ApplyModifiedProperties();
        }

        /*


        private RailwayManager manager;

        //private SerializedProperty railwayNetworkProp;

        //private SerializedProperty pointsProp;

        public void OnEnable()
        {*/
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
        /*
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
        */
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

        /*
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

    public void OnDisable()
    {
        //Debug.Log("Yam");


    }

*/
    }
}