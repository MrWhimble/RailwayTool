using System;
using System.Collections.Generic;
using UnityEngine;

namespace MrWhimble.RailwayMaker
{
    [RequireComponent(typeof(RailwayManager))]
    public class RailwayMeshManager : MonoBehaviour
    {
        [SerializeField] private Material material;
        [SerializeField, Min(2)] private int curveSegments;
        [SerializeField] private float trackSeperation;
        [SerializeField] private float trackHeight;
        [SerializeField] private float trackWidth;
        
        private RailwayManager manager;
        
        private List<Point> points;
        private List<BezierCurve> curves;

        private void Start()
        {
            manager = GetComponent<RailwayManager>();
            GeneratePointsAndCurves();
            foreach (var c in curves)
            {
                GenerateMeshForCurve(c);
            }
        }
        
        private void GeneratePointsAndCurves()
        {
            if (manager == null || manager.PathData == null)
                return;

            points = manager.PathData.GetPoints();
            curves = manager.PathData.GetCurves(points);
        }

        private GameObject CreateGameObject(string n = "new GameObject", Transform parent = null, params Type[] components)
        {
            GameObject go = new GameObject(n, components);
            go.transform.SetParent(parent);
            return go;
        }

        private void GenerateMeshForCurve(BezierCurve curve)
        {
            if (curve.IsInvalid())
                return;

            GameObject meshGo = CreateGameObject("Curve", transform, 
                typeof(MeshFilter), typeof(MeshRenderer));

            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();

            float delta = 1f / (float) curveSegments;
            for (float t = 0f; t <= 1.0001f; t+=delta)
            {
                Vector3 pos = curve.GetPosition(t);
                Vector3 tangent = curve.GetTangent(t);
                Vector3 normal = curve.GetNormal(t);
                Vector3 binormal = Vector3.Cross(normal, tangent);
                
                Vector3 trackHalfHeight = normal * trackHeight * 0.5f;
                Vector3 trackHalfWidth = binormal * trackWidth * 0.5f;
                
                Vector3 trackCenter = pos + binormal * trackSeperation;
                verts.Add(trackCenter + trackHalfHeight - trackHalfWidth);
                verts.Add(trackCenter + trackHalfHeight + trackHalfWidth);
                
                verts.Add(trackCenter + trackHalfHeight + trackHalfWidth);
                verts.Add(trackCenter - trackHalfHeight + trackHalfWidth);
                
                verts.Add(trackCenter - trackHalfHeight + trackHalfWidth);
                verts.Add(trackCenter - trackHalfHeight - trackHalfWidth);
                
                verts.Add(trackCenter - trackHalfHeight - trackHalfWidth);
                verts.Add(trackCenter + trackHalfHeight - trackHalfWidth);
                
                trackCenter = pos - binormal * trackSeperation;
                verts.Add(trackCenter + trackHalfHeight - trackHalfWidth);
                verts.Add(trackCenter + trackHalfHeight + trackHalfWidth);
                
                verts.Add(trackCenter + trackHalfHeight + trackHalfWidth);
                verts.Add(trackCenter - trackHalfHeight + trackHalfWidth);
                
                verts.Add(trackCenter - trackHalfHeight + trackHalfWidth);
                verts.Add(trackCenter - trackHalfHeight - trackHalfWidth);
                
                verts.Add(trackCenter - trackHalfHeight - trackHalfWidth);
                verts.Add(trackCenter + trackHalfHeight - trackHalfWidth);
            }

            for (int i = 0; i < curveSegments; i++)
            {
                int baseI = i * 16;
                // top
                AddTri(ref tris, baseI, baseI+16, baseI+1);
                AddTri(ref tris, baseI+1, baseI+16, baseI+17);
                
                AddTri(ref tris, 2+baseI, 2+baseI+16, 2+baseI+1);
                AddTri(ref tris, 2+baseI+1, 2+baseI+16, 2+baseI+17);
                
                AddTri(ref tris, 4+baseI, 4+baseI+16, 4+baseI+1);
                AddTri(ref tris, 4+baseI+1, 4+baseI+16, 4+baseI+17);
                
                AddTri(ref tris, 6+baseI, 6+baseI+16, 6+baseI+1);
                AddTri(ref tris, 6+baseI+1, 6+baseI+16, 6+baseI+17);
                
                AddTri(ref tris, 8+baseI, 8+baseI+16, 8+baseI+1);
                AddTri(ref tris, 8+baseI+1, 8+baseI+16, 8+baseI+17);
                
                AddTri(ref tris, 8+2+baseI, 8+2+baseI+16, 8+2+baseI+1);
                AddTri(ref tris, 8+2+baseI+1, 8+2+baseI+16, 8+2+baseI+17);
                
                AddTri(ref tris, 8+4+baseI, 8+4+baseI+16, 8+4+baseI+1);
                AddTri(ref tris, 8+4+baseI+1, 8+4+baseI+16, 8+4+baseI+17);
                
                AddTri(ref tris, 8+6+baseI, 8+6+baseI+16, 8+6+baseI+1);
                AddTri(ref tris, 8+6+baseI+1, 8+6+baseI+16, 8+6+baseI+17);
            }
            
            Mesh mesh = new Mesh();
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            MeshFilter mf = meshGo.GetComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            MeshRenderer mr = meshGo.GetComponent<MeshRenderer>();
            mr.sharedMaterial = material;
        }

        private void AddTri(ref List<int> t, int a, int b, int c)
        {
            t.Add(a);
            t.Add(b);
            t.Add(c);
        }
    }
}