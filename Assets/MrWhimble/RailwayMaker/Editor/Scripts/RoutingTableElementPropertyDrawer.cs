using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MrWhimble.RailwayMaker.Routing
{
    [CustomPropertyDrawer(typeof(RoutingTableElement))]
    public class RoutingTableElementPropertyDrawer : PropertyDrawer
    {
        
        
        public override void OnGUI(Rect position, SerializedProperty property,
            GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            position.height = 20;
            
            string[] waypointNames = GetWaypointNames(WaypointEditorManager.Waypoints);
            SerializedProperty waypointNameProp = property.FindPropertyRelative("waypointName");
            int selectedIndex = GetIndex(waypointNameProp.stringValue, waypointNames);
            EditorGUI.BeginChangeCheck();
            selectedIndex = EditorGUI.Popup(position, "Name", selectedIndex, waypointNames);
            if (EditorGUI.EndChangeCheck())
            {
                waypointNameProp.stringValue = waypointNames[selectedIndex];
            }
            position.y += 20;

            if (selectedIndex < 0 || selectedIndex >= WaypointEditorManager.Waypoints.Count)
            {
                EditorGUI.EndProperty();
                return;
            }
            
            SerializedProperty waypointSideProp = property.FindPropertyRelative("side");
            int sideIndex = waypointSideProp.boolValue ? 1 : 0;
            EditorGUI.BeginChangeCheck();
            sideIndex = EditorGUI.Popup(position, "Side", sideIndex, new[] {"A", "B"});
            if (EditorGUI.EndChangeCheck())
            {
                waypointSideProp.boolValue = sideIndex != 0;
            }

            position.y += 20;
            
            IndentRect(ref position, true);
            
            if (WaypointEditorManager.Waypoints[selectedIndex].IsStoppedAt)
            {
                EditorGUI.LabelField(position, "Stop Settings:");
                position.y += 20;
                SerializedProperty leaveConditionProp = property.FindPropertyRelative("leaveCondition");
                EditorGUI.PropertyField(position, leaveConditionProp);
                if (leaveConditionProp.enumNames[leaveConditionProp.enumValueIndex].Equals("AfterTime"))
                {
                    position.y += 20;
                    EditorGUI.PropertyField(position, property.FindPropertyRelative("waitTime"));
                }
            }
            else
            {
                EditorGUI.LabelField(position, "No Stopping");
            }
            IndentRect(ref position, false);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 100;
        }

        private void IndentRect(ref Rect r, bool increase, float indentAmount = 24)
        {
            
            if (increase)
            {
                r.x += indentAmount;
                r.width -= indentAmount;
            }
            else
            {
                r.x -= indentAmount;
                r.width += indentAmount;
            }
        }

        private string[] GetWaypointNames(List<IWaypoint> waypoints)
        {
            string[] ret = new string[waypoints.Count];
            for (int i = 0; i < waypoints.Count; i++)
            {
                ret[i] = waypoints[i].Name;
            }
            //Array.Sort(ret);
            return ret;
        }

        private int GetIndex(string s, string[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (s.Equals(array[i]))
                    return i;
            }

            return -1;
        }
    }
}