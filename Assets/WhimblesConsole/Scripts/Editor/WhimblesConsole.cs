using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MrWhimble.ConstantConsole
{
    public class WhimblesConsole : EditorWindow
    {
        private Vector2 scrollPos;

        private Color originalColor;
        private Color offsetColor;
        private Color selectedColor;
        
        [MenuItem("Tools/MrWhimble/ConstantConsole")]
        private static void ShowWindow()
        {
            var window = GetWindow<WhimblesConsole>();
            window.titleContent = new GUIContent("Constant Console");
            window.Show();
        }

        private void OnGUI()
        {
            if (GUILayout.Button("Clear"))
            {
                ConstantDebug.Clear();
            }
            
            if (ConstantDebug.data == null || ConstantDebug.headers == null)
                return;

            
            
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Width(position.width), GUILayout.Height(position.height-20f));
            
            GUIStyle style = new GUIStyle(GUI.skin.window);
            style.wordWrap = true;
            style.stretchHeight = false;
            style.padding = new RectOffset(3, 3, 3, 3);

            originalColor = GUI.backgroundColor;
            offsetColor = originalColor - new Color(0.1f, 0.1f, 0.1f, 0);
            selectedColor = Color.grey;

            int index = 0;
            DateTime timeNow = DateTime.Now;
            foreach (KeyValuePair<string, List<int>> kv in ConstantDebug.headers)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                if (kv.Key != "")EditorGUILayout.LabelField(kv.Key);
                EditorGUILayout.BeginVertical();
                foreach (int id in kv.Value)
                {
                    if (index % 2 == 1)
                        GUI.backgroundColor = offsetColor;
                    EditorGUILayout.BeginVertical(style);

                    ConstantDebugData d = ConstantDebug.data[id];


                    EditorGUILayout.BeginHorizontal();

                    TimeSpan timeSince = timeNow.Subtract(d.lastUpdateTime);
                    string timeLabel = $"[{d.lastUpdateTime.Hour:00}:{d.lastUpdateTime.Minute:00}:{d.lastUpdateTime.Second:00}]";
                    if (timeSince.TotalSeconds >= 0.25f)
                        timeLabel += $" (T-{timeSince.TotalSeconds:0.00})";
                    EditorGUILayout.LabelField(timeLabel);

                    if (d.gameObject == null)
                        EditorGUILayout.LabelField("", GUILayout.Width(50f));
                    else
                    {
                        GUI.backgroundColor = Selection.Contains(d.gameObject) ? selectedColor : originalColor;
                        if (GUILayout.Button("Select", GUILayout.Width(50f)))
                        {
                            Selection.objects = new[] {(Object)d.gameObject};
                        }
                    }
                    GUI.backgroundColor = originalColor;
                    
                    if (GUILayout.Button("Goto", GUILayout.Width(38f)))
                    {
                        AssetDatabase.OpenAsset(d.instanceID, d.lineNumber);
                    }
                    
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.LabelField($"{d.Text}");
                    
                    EditorGUILayout.EndVertical();

                    index++;
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndScrollView();
        }

        
        private void Update()
        {
            if (ConstantDebug.hasUpdated)
            {
                ConstantDebug.hasUpdated = false;
                Repaint();
            }
        }
    }
}