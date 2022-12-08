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
        private List<BezierCurve> curves;

        private int selectedPoint = -1;
        private Vector3 selectedPointRot;

        //private int pointControlIDIndex;
        private List<int> pointControlIDs;
        private int prevHotControl;


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


            RailPathData data = (RailPathData) railPathObj.targetObject;
            points = data.GetPoints();
            curves = data.GetCurves(points);
            
            //Debug.Log(points.Count);
            //Debug.Log(curves.Count);

            if (points.Count == 0)
            {
                points.Add(new AnchorPoint());
                points.Add(new ControlPoint());
                points.Add(new ControlPoint());
                points.Add(new AnchorPoint());

                ((ControlPoint) points[2]).flipped = true;

                ((AnchorPoint) points[0]).AddControlPoint((ControlPoint) points[1]);
                ((AnchorPoint) points[3]).AddControlPoint((ControlPoint) points[2]);

                ((AnchorPoint) points[0]).UpdatePosition(new Vector3(0, 0, 0));
                ((AnchorPoint) points[3]).UpdatePosition(new Vector3(4, 0, 0));

                ((ControlPoint) points[1]).UpdatePosition(new Vector3(1, 1, 0));
                ((ControlPoint) points[2]).UpdatePosition(new Vector3(3, -1, 0));

                curves.Add(new BezierCurve(
                    (AnchorPoint) points[0],
                    (ControlPoint) points[1],
                    (ControlPoint) points[2],
                    (AnchorPoint) points[3]));
            }

            pointControlIDs = new List<int>();
            for (int i = 0; i < points.Count; i++)
            {
                pointControlIDs.Add(-1);
            }
            
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();

            int prevSelectedPoint = selectedPoint;
            selectedPoint = EditorGUILayout.IntField("Current Point Index", selectedPoint);
            if (prevSelectedPoint != selectedPoint)
            {
                if (selectedPoint >= 0 && selectedPoint < points.Count)
                {
                    if (points[selectedPoint].GetType() == typeof(AnchorPoint))
                        selectedPointRot = ((AnchorPoint) points[selectedPoint]).rotation.eulerAngles;
                }
                // update current curve
            }

            Vector3 prevV3;
            Vector3 newV3;
            float prevFloat;
            float newFloat;
            bool prevBool;
            bool newBool;

            bool updateScene = false;

            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            if (selectedPoint >= 0 && selectedPoint < points.Count)
            {
                switch (points[selectedPoint])
                {
                    case AnchorPoint p:
                    {
                        EditorGUILayout.LabelField("Type: Anchor");
                        
                        prevV3 = p.position;
                        newV3 = EditorGUILayout.Vector3Field("Position", p.position);
                        if (prevV3 != newV3)
                        {
                            p.UpdatePosition(newV3);
                            updateScene = true;
                        }

                        prevV3 = selectedPointRot;
                        newV3 = EditorGUILayout.Vector3Field("Rotation", selectedPointRot);
                        if (prevV3 != newV3)
                        {
                            selectedPointRot = newV3;
                            p.rotation = Quaternion.Euler(newV3);
                            p.UpdateControls();
                            updateScene = true;
                        }
                        
                        break;
                    }
                    
                    case ControlPoint p:
                    {
                        EditorGUILayout.LabelField("Type: Control");
                        //p.position = EditorGUILayout.Vector3Field("Position", p.position);
                        prevV3 = p.position;
                        newV3 = EditorGUILayout.Vector3Field("Position", p.position);
                        if (prevV3 != newV3)
                        {
                            p.UpdatePosition(newV3);
                            updateScene = true;
                        }
                        
                        prevFloat = p.distance;
                        newFloat = EditorGUILayout.FloatField("Distance", p.distance);
                        if (prevFloat != newFloat)
                        {
                            p.distance = newFloat;
                            p.UpdatePosition();
                            updateScene = true;
                        }

                        prevBool = p.flipped;
                        newBool = EditorGUILayout.Toggle("Flipped", p.flipped);
                        if (prevBool != newBool)
                        {
                            p.flipped = newBool;
                            p.UpdatePosition();
                            updateScene = true;
                        }
                        break;
                    }
                }

                if (updateScene)
                {
                    SceneView.RepaintAll();
                }
                
            }
            else
            {
                EditorGUILayout.LabelField("Invalid Selected Point");
            }
            EditorGUILayout.EndVertical();
            
            
        }

        private void OnSceneGUI()
        {
            Vector3 handlePos;
            bool updateInspector = false;
            //pointControlIDIndex = 0;
            for (int i = 0; i < points.Count; i++)
            {
                switch (points[i])
                {
                    case AnchorPoint p:
                    {
                        Handles.color = Color.blue;

                        if (selectedPoint == i)
                        {
                            if (Tools.pivotRotation == PivotRotation.Local)
                                handlePos = Handles.PositionHandle(p.position, p.rotation);
                            else
                                handlePos = Handles.PositionHandle(p.position, Quaternion.identity);
                            pointControlIDs[i] = -1;
                        }
                        else
                        {
                            pointControlIDs[i] = GUIUtility.GetControlID(i, FocusType.Passive);
                            handlePos = Handles.FreeMoveHandle(pointControlIDs[i], p.position, p.rotation, 0.5f, Vector3.zero,
                                Handles.SphereHandleCap);
                        }

                        if (p.position != handlePos)
                        {
                            p.UpdatePosition(handlePos);
                            selectedPointRot = p.rotation.eulerAngles;
                            updateInspector = true;
                            //updatePoint = true;
                        }
                        break;
                    }

                    case ControlPoint p:
                    {
                        Handles.color = Color.red;
                        if (selectedPoint == i)
                        {
                            handlePos = Handles.PositionHandle(p.position, Quaternion.identity);
                            pointControlIDs[i] = -1;
                        }
                        else
                        {
                            pointControlIDs[i] = GUIUtility.GetControlID(i, FocusType.Passive);
                            handlePos = Handles.FreeMoveHandle(pointControlIDs[i], p.position, Quaternion.identity, 0.5f, Vector3.zero,
                                Handles.SphereHandleCap);
                        }

                        if (p.position != handlePos)
                        {
                            p.UpdatePosition(handlePos);
                            selectedPointRot = p.anchorPoint.rotation.eulerAngles;
                            updateInspector = true;
                            //updatePoint = true;
                        }
                        break;
                    }
                }
            }

            

            for (int i = 0; i < curves.Count; i++)
            {
                Handles.DrawBezier(
                    curves[i].start.position,
                    curves[i].end.position,
                    curves[i].controlStart.position,
                    curves[i].controlEnd.position,
                    Color.magenta, null, 2);
            }

            
            if (prevHotControl != GUIUtility.hotControl && Event.current.shift)
            {
                if (GUIUtility.hotControl != 0)
                {
                    selectedPoint = pointControlIDs.IndexOf(GUIUtility.hotControl);
                    prevHotControl = GUIUtility.hotControl;

                    updateInspector = true;
                }
            }
            
            
            if (updateInspector)
            {
                Repaint();
            }
        }

        private void OnDisable()
        {
            Debug.Log("OnDisable");

            railPathObj.Update();

            //SerializedProperty railPathPointsProp = railPathProp.FindPropertyRelative("pathPoints");
            /*
            SerializedProperty railPathPointsProp = railPathObj.GetIterator();
            do
            {
                if (railPathPointsProp.name == "pathPoints")
                {
                    Debug.Log(railPathPointsProp.name);
                    break;
                }
            } while (railPathPointsProp.Next(true));
            */
            
            SerializedProperty railPathPointsProp = railPathObj.FindProperty("pathPoints");
            SerializedProperty railPathCurvesProp = railPathObj.FindProperty("pathCurves");

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

            railPathCurvesProp.ClearArray();
            
            for (int i = 0; i < curves.Count; i++)
            {
                railPathCurvesProp.InsertArrayElementAtIndex(i);
                SerializedProperty curveProp = railPathCurvesProp.GetArrayElementAtIndex(i);
                SerializedProperty curveStartProp = curveProp.FindPropertyRelative("start");
                SerializedProperty curveControlStartProp = curveProp.FindPropertyRelative("controlStart");
                SerializedProperty curveControlEndProp = curveProp.FindPropertyRelative("controlEnd");
                SerializedProperty curveEndProp = curveProp.FindPropertyRelative("end");

                curveStartProp.intValue = points.IndexOf(curves[i].start);
                curveControlStartProp.intValue = points.IndexOf(curves[i].controlStart);
                curveControlEndProp.intValue = points.IndexOf(curves[i].controlEnd);
                curveEndProp.intValue = points.IndexOf(curves[i].end);
            }

            railPathObj.ApplyModifiedProperties();
        }

        private void CustomSphereCapFunction(int controlID, Vector3 pos, Quaternion rot, float size, EventType eventType)
        {
            pointControlIDs[0] = controlID;
            Handles.SphereHandleCap(controlID, pos, rot, size, eventType);
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