using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MrWhimble.RailwayMaker
{
    [CustomEditor(typeof(RailPathData))]
    public class RailPathDataEditor : Editor
    {
        SerializedProperty _pathPointsProp;
        SerializedProperty _pathCurvesProp;

        private void OnEnable()
        {
            _pathPointsProp = serializedObject.FindProperty("pathPoints");
            _pathCurvesProp = serializedObject.FindProperty("pathCurves");
        }

        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Clear Lists"))
                ClearLists();

            if (GUILayout.Button("Validate"))
                Validate();
            
            base.OnInspectorGUI();
        }

        private void ClearLists()
        {
            serializedObject.Update();
            
            _pathPointsProp.ClearArray();
            _pathCurvesProp.ClearArray();

            serializedObject.ApplyModifiedProperties();
        }

        private void Validate()
        {
            bool success = true;

            if (_pathPointsProp.arraySize == 0)
            {
                Debug.LogWarning($"There is no list of points");
                success = false;
            }
            else
            {
                for (int i = 0; i < _pathPointsProp.arraySize; i++)
                {
                    SerializedProperty elementProp = _pathPointsProp.GetArrayElementAtIndex(i);
                    SerializedProperty elementArrayProp = elementProp.FindPropertyRelative("connectedPoints");
                    if (elementArrayProp.arraySize == 0)
                    {
                        Debug.LogWarning($"Point at {i} has not connected Points");
                        success = false;
                        continue;
                    }

                    for (int j = 0; j < elementArrayProp.arraySize; j++)
                    {
                        SerializedProperty elementArrayElemProp = elementArrayProp.GetArrayElementAtIndex(j);
                        if (elementArrayElemProp.intValue == -1)
                        {
                            Debug.LogWarning($"Point at {i} has an invalid index");
                            success = false;
                            break;
                        }
                    }
                }
            }

            if (_pathCurvesProp.arraySize == 0)
            {
                Debug.LogWarning($"There is no list of curves");
                success = false;
            }
            else
            {
                for (int i = 0; i < _pathCurvesProp.arraySize; i++)
                {
                    SerializedProperty elementProp = _pathCurvesProp.GetArrayElementAtIndex(i);
                    SerializedProperty startIndexProp = elementProp.FindPropertyRelative("start");
                    if (startIndexProp.intValue == -1)
                    {
                        Debug.LogWarning($"Curve at {i} has an invalid index");
                        success = false;
                        break;
                    }

                    SerializedProperty controlStartIndexProp = elementProp.FindPropertyRelative("controlStart");
                    if (controlStartIndexProp.intValue == -1)
                    {
                        Debug.LogWarning($"Curve at {i} has an invalid index");
                        success = false;
                        break;
                    }

                    SerializedProperty controlEndIndexProp = elementProp.FindPropertyRelative("controlEnd");
                    if (controlEndIndexProp.intValue == -1)
                    {
                        Debug.LogWarning($"Curve at {i} has an invalid index");
                        success = false;
                        break;
                    }

                    SerializedProperty endIndexProp = elementProp.FindPropertyRelative("end");
                    if (endIndexProp.intValue == -1)
                    {
                        Debug.LogWarning($"Curve at {i} has an invalid index");
                        success = false;
                        break;
                    }
                }
            }

            if (success)
            {
                Debug.Log($"Successfully validated {serializedObject.targetObject.name}");
            }
        }
    }
}