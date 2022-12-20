#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
#endif // UNITY_EDITOR

namespace MrWhimble.ConstantConsole
{
    public static class ConstantDebug
    {
#if UNITY_EDITOR
        public static Dictionary<string, List<int>> headers;
        public static Dictionary<int, ConstantDebugData> data;
        public static bool hasUpdated;

        private static string filePrefix = " (at ";
        //private static string filePrefix = "] in ";
#endif // UNITY_EDITOR

#if UNITY_EDITOR
        public static void Clear()
        {
            data = new Dictionary<int, ConstantDebugData>();
            headers = new Dictionary<string, List<int>>();
            Debug.Log("(ConstantDebug) Cleared Constants");
        }
#endif // UNITY_EDITOR

        /// <summary>
        /// Log message to Constant Console
        /// </summary>
        /// <param name="m">The message to log</param>
        /// <param name="ctx">The context object used to make the log more unique (useful for multiple GameObjects)</param>
        /// <param name="h">The header for sorting in the Constant Console</param>
        public static void Log(object m, object ctx = null, string h = "")
        {
            #if UNITY_EDITOR
            if (data == null || headers == null)
                Clear();

            // Get stack trace for ID, file and line number
            string stackTrace = StackTraceUtility.ExtractStackTrace().Split('\n')[2];
            //string stackTrace = Environment.StackTrace.Split('\n')[2];
            //Debug.Log(stackTrace);
            
            // use hash of stack trace and context for ID
            int id = GetID(stackTrace, ctx);
            
            // Try updating existing data or add new data
            if (data.TryGetValue(id, out ConstantDebugData d))
            {
                d.msg = m;
                d.lastUpdateTime = DateTime.Now;
            }
            else
            {
                // Get context GameObject
                GameObject go = null;
                if (ctx is GameObject g)
                    go = g;
                else if (ctx is MonoBehaviour mb)
                    go = mb.gameObject;

                // Get script asset and line number of log
                int startIndex = stackTrace.IndexOf(filePrefix) + filePrefix.Length;
                string pathAndLineString = stackTrace.Substring(startIndex, stackTrace.Length - startIndex - 1);
                string[] pathAndLine = pathAndLineString.Split(':');
                UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(pathAndLine[0]);
                int line = int.Parse(pathAndLine[1]);
                
                // Create new data
                data.Add(id, new ConstantDebugData(){
                    logType = LogType.Log, 
                    msg = m, 
                    context = ctx, 
                    gameObject = go,
                    instanceID = asset.GetInstanceID(),
                    lineNumber = line,
                    lastUpdateTime = DateTime.Now
                });
                
                // Log new constant log
                Debug.Log($"New Constant Debug Log (ID: {id})\nInitial Message:\n{m}\nContext: {ctx}\nHeader: {h}", go);
                
                // Add ID to headers list
                if (headers.TryGetValue(h, out List<int> ids))
                {
                    ids.Add(id);
                }
                else
                {
                    headers.Add(h, new List<int>{id});
                }
            }

            hasUpdated = true;

            #endif // UNITY_EDITOR
        }
        
        
        #if UNITY_EDITOR
        // https://stackoverflow.com/questions/1646807/quick-and-simple-hash-code-combinations
        private static int GetID(string trace, object context)
        {
            int h = 1009;
            h = h * 9176 + trace.GetHashCode();
            if (context != null) h = h * 9176 + context.GetHashCode();
            return h;
        }
        #endif // UNITY_EDITOR
    }
    
    
    #if UNITY_EDITOR
    public class ConstantDebugData
    {
        public LogType logType;
        public object context;
        public GameObject gameObject;
        public int instanceID;
        public int lineNumber;
        public DateTime lastUpdateTime;
        public object msg;
        public string Text => $"{msg}";
    }
    #endif // UNITY_EDITOR
}