using System;
using UnityEditor;
using UnityEngine;

namespace MrWhimble.RailwayMaker
{
    [CreateAssetMenu(fileName = "new RailwayEditorSettings", menuName = "MrWhimble/Railway Editor Settings")]
    public class RailwayEditorSettings : ScriptableObject
    {
        private static string _editorSettingsPathKey => "MrWhimble_RailwayMaker_EditorSettingsPath";
        
        private static RailwayEditorSettings _instance;
        public static RailwayEditorSettings Instance
        {
            set
            {
                _instance = value;
                if (_instance == null)
                {
                    string path = AssetDatabase.GetAssetPath(_instance);
                    EditorPrefs.SetString(_editorSettingsPathKey, path);
                }
            }
            get
            {
                if (_instance == null)
                {
                    if (EditorPrefs.HasKey(_editorSettingsPathKey))
                    {
                        string path = EditorPrefs.GetString(_editorSettingsPathKey);
                        _instance = AssetDatabase.LoadAssetAtPath<RailwayEditorSettings>(path);
                    }
                    else
                    {
                        string[] guids = AssetDatabase.FindAssets($"t:{typeof(RailwayEditorSettings)}");
                        if (guids.Length == 0)
                        {
                            //throw new NullReferenceException("No editor settings found in preferences for MrWhimble -> Railway Maker");
                            Debug.LogError("No editor settings found. Go to Preferences > MrWhimble > Railway Maker");
                            return null;
                        }
                            
                        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        _instance = AssetDatabase.LoadAssetAtPath<RailwayEditorSettings>(path);
                    }
                }

                return _instance;
            }
        }
        
        public Color AnchorColor = Color.blue;
        public Color AnchorHighlightColor = new Color(0f, 1f, 1f, 1f);
        public Color ControlColor = Color.red;
        public Color ControlLineColor = Color.red;
        public Color InterControlLineColor = new Color(1f, 0.5f, 0, 1f);
        public Color RailLineColor = Color.magenta;
        public Color RailNormalColor = Color.white;
        public Color SplitLineColor = Color.green;

        public bool ControlLineShow = true;
        public bool InterControlLineShow = true;
        public bool RailNormalShow = true;

        [Min(0f)] public float AnchorSize = 0.5f;
        [Min(0f)] public float ControlSize = 0.35f;
        [Min(0f)] public float ControlLineThickness = 1f;
        [Min(0f)] public float InterControlLineThickness = 1f;
        [Min(0f)] public float RailLineThickness = 2f;
        [Min(0f)] public float RailNormalThickness = 1f;
        public float RailNormalLength = 0.2f;
        [Min(0f)] public float SplitLineThickness = 1f;

        [Min(2)] public int RailNormalCount = 20;
        [Min(2)] public int SplitDistantSearchCount = 25;
    }
}