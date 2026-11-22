using System.Collections.Generic;
using UnityEngine;

namespace FrogRunner.Core
{
    /// <summary>
    /// Low-poly primitive meshes generated once and shared by every prop.
    /// All meshes use hard low-poly shapes, explicit outward normals, and are rendered
    /// double-sided by RetroAssets so the silhouettes stay solid at any viewing angle.
    /// TUNING: lower the segment counts for a chunkier PS1 silhouette, raise them for smoother frogs.
    /// </summary>
    public static class RetroMeshFactory
    {
        static Mesh _cube;
        static Mesh _sphere;
        static Mesh _cylinder;
        static Mesh _cone;
        static Mesh _wing;

        public static Mesh Cube { get { if (_cube == null) _cube = BuildCube(); return _cube; } }
        public static Mesh Sphere { get { if (_sphere == null) _sphere = BuildSphere(10, 6); return _sphere; } }
        public static Mesh Cylinder { get { if (_cylinder == null) _cylinder = BuildCylinder(10); return _cylinder; } }
        public static Mesh Cone { get { if (_cone == null) _cone = BuildCone(10); return _cone; } }
        public static Mesh Wing { get { if (_wing == null) _wing = BuildWing(); return _wing; } }

        public static Mesh BuildSphere(int segments, int rings)
        {
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            for (int ring = 0; ring <= rings; ring++)
            {
                float v = ring / (float)rings;
                float phi = v * Mathf.PI;
                float y = Mathf.Cos(phi) * 0.5f;
                float radius = Mathf.Sin(phi) * 0.5f;
                for (int seg = 0; seg <= segments; seg++)
                {
                    float u = seg / (float)segments;
                    float theta = u * Mathf.PI * 2f;
                    var p = new Vector3(Mathf.Cos(theta) * radius, y, Mathf.Sin(theta) * radius);
                    verts.Add(p);
                    norms.Add(p.sqrMagnitude > 0.0001f ? p.normalized : Vector3.up);
                    uvs.Add(new Vector2(u, 1f - v));
                }
            }

            int stride = segments + 1;
            for (int ring = 0; ring < rings; ring++)
            {
                for (int seg = 0; seg < segments; seg++)
                {
                    int a = ring * stride + seg;
                    int b = a + stride;
                    tris.Add(a); tris.Add(b); tris.Add(a + 1);
                    tris.Add(b); tris.Add(b + 1); tris.Add(a + 1);
                }
            }

            var mesh = new Mesh { name = "RetroSphere" };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        static Mesh BuildCube()
        {
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n)
            {
                int i = verts.Count;
                verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
                norms.Add(n); norms.Add(n); norms.Add(n); norms.Add(n);
                uvs.Add(new Vector2(0f, 0f)); uvs.Add(new Vector2(1f, 0f));
                uvs.Add(new Vector2(1f, 1f)); uvs.Add(new Vector2(0f, 1f));
                tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
                tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
            }

            Quad(new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f), Vector3.forward);
            Quad(new Vector3(0.5f, -0.5f, -0.5f), new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(0.5f, 0.5f, -0.5f), Vector3.back);
            Quad(new Vector3(0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f), Vector3.right);
            Quad(new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, -0.5f), Vector3.left);
            Quad(new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f), Vector3.up);
            Quad(new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, 0.5f), new Vector3(-0.5f, -0.5f, 0.5f), Vector3.down);

            var mesh = new Mesh { name = "RetroCube" };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        static Mesh BuildCylinder(int segments)
        {
            const float radius = 0.5f;
            const float halfHeight = 0.5f;
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            // Sides (top/bottom rings)
            for (int r = 0; r < 2; r++)
            {
                float y = r == 0 ? -halfHeight : halfHeight;
                for (int i = 0; i <= segments; i++)
                {
                    float t = i / (float)segments;
                    float ang = t * Mathf.PI * 2f;
                    var n = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                    verts.Add(n * radius + Vector3.up * y);
                    norms.Add(n);
                    uvs.Add(new Vector2(t, r));
                }
            }
            int stride = segments + 1;
            for (int i = 0; i < segments; i++)
            {
                int a = i;
                int b = i + stride;
                tris.Add(a); tris.Add(b); tris.Add(a + 1);
                tris.Add(b); tris.Add(b + 1); tris.Add(a + 1);
            }

            // Caps
            for (int cap = 0; cap < 2; cap++)
            {
                float y = cap == 0 ? -halfHeight : halfHeight;
                Vector3 normal = cap == 0 ? Vector3.down : Vector3.up;
                int center = verts.Count;
                verts.Add(new Vector3(0f, y, 0f)); norms.Add(normal); uvs.Add(new Vector2(0.5f, 0.5f));
                for (int i = 0; i <= segments; i++)
                {
                    float t = i / (float)segments;
                    float ang = t * Mathf.PI * 2f;
                    verts.Add(new Vector3(Mathf.Cos(ang) * radius, y, Mathf.Sin(ang) * radius));
                    norms.Add(normal);
                    uvs.Add(new Vector2(Mathf.Cos(ang) * 0.5f + 0.5f, Mathf.Sin(ang) * 0.5f + 0.5f));
                }
                for (int i = 0; i < segments; i++)
                {
                    if (cap == 0) { tris.Add(center); tris.Add(center + 1 + i); tris.Add(center + 2 + i); }
                    else { tris.Add(center); tris.Add(center + 2 + i); tris.Add(center + 1 + i); }
                }
            }

            var mesh = new Mesh { name = "RetroCylinder" };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        static Mesh BuildCone(int segments)
        {
            const float radius = 0.5f;
            const float halfHeight = 0.5f;
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            int apex = verts.Count;
            verts.Add(new Vector3(0f, halfHeight, 0f));
            norms.Add(new Vector3(0f, 0.7f, 0f).normalized);
            uvs.Add(new Vector2(0.5f, 1f));

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float ang = t * Mathf.PI * 2f;
                verts.Add(new Vector3(Mathf.Cos(ang) * radius, -halfHeight, Mathf.Sin(ang) * radius));
                norms.Add(new Vector3(Mathf.Cos(ang) * 0.85f, 0.35f, Mathf.Sin(ang) * 0.85f).normalized);
                uvs.Add(new Vector2(t, 0f));
            }
            for (int i = 0; i < segments; i++)
            {
                tris.Add(apex); tris.Add(apex + 1 + i); tris.Add(apex + 2 + i);
            }

            int center = verts.Count;
            verts.Add(new Vector3(0f, -halfHeight, 0f)); norms.Add(Vector3.down); uvs.Add(new Vector2(0.5f, 0.5f));
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float ang = t * Mathf.PI * 2f;
                verts.Add(new Vector3(Mathf.Cos(ang) * radius, -halfHeight, Mathf.Sin(ang) * radius));
                norms.Add(Vector3.down);
                uvs.Add(new Vector2(Mathf.Cos(ang) * 0.5f + 0.5f, Mathf.Sin(ang) * 0.5f + 0.5f));
            }
            for (int i = 0; i < segments; i++)
            {
                tris.Add(center); tris.Add(center + 2 + i); tris.Add(center + 1 + i);
            }

            var mesh = new Mesh { name = "RetroCone" };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        static Mesh BuildWing()
        {
            // A simple low-poly triangular canopy, unit span/depth, double sided.
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            Vector3 front = new Vector3(0f, 0.10f, 0.55f);
            Vector3 left = new Vector3(-0.5f, 0f, -0.55f);
            Vector3 right = new Vector3(0.5f, 0f, -0.55f);
            Vector3 back = new Vector3(0f, 0.02f, -0.45f);

            void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 n, float uvScale)
            {
                int i = verts.Count;
                verts.Add(a); verts.Add(b); verts.Add(c);
                norms.Add(n); norms.Add(n); norms.Add(n);
                uvs.Add(new Vector2(0.5f, 1f) * uvScale);
                uvs.Add(new Vector2(0f, 0f) * uvScale);
                uvs.Add(new Vector2(1f, 0f) * uvScale);
                tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
            }

            // visible top panels (V-shaped billow)
            Tri(front, left, back, Vector3.up, 1f);
            Tri(front, back, right, Vector3.up, 1f);
            // underside panels so it reads from every angle
            Tri(front, back, left, Vector3.down, 1f);
            Tri(front, right, back, Vector3.down, 1f);

            var mesh = new Mesh { name = "RetroWing" };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
