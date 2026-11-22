using System.Collections.Generic;
using FrogRunner.Core;
using FrogRunner.RiverGlider;
using UnityEngine;

namespace FrogRunner.WorldGen
{
    /// <summary>
    /// Course decoration, lane paint, finish gate and sky. Kept in a partial file so the layout code
    /// in CourseBuilder.cs stays readable while every tuning value remains next to its prop type.
    /// </summary>
    public partial class CourseBuilder
    {
        void BuildMarkings()
        {
            var white = new List<Vector3>();
            var whiteNormals = new List<Vector3>();
            var whiteUvs = new List<Vector2>();
            var whiteTris = new List<int>();
            var yellow = new List<Vector3>();
            var yellowNormals = new List<Vector3>();
            var yellowUvs = new List<Vector2>();
            var yellowTris = new List<int>();
            const float markEndZ = 1225f;

            void Quad(List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs, List<int> tris,
                float x0, float x1, float z0, float z1, float y)
            {
                int i = verts.Count;
                verts.Add(new Vector3(x0, y, z0));
                verts.Add(new Vector3(x0, y, z1));
                verts.Add(new Vector3(x1, y, z1));
                verts.Add(new Vector3(x1, y, z0));
                for (int n = 0; n < 4; n++) norms.Add(Vector3.up);
                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(0f, 1f));
                uvs.Add(new Vector2(1f, 1f));
                uvs.Add(new Vector2(1f, 0f));
                tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
                tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
            }

            // Dashed white boundaries between forward lanes.
            float[] dashed = { -1.92f, 1.92f, 5.76f };
            for (int line = 0; line < dashed.Length; line++)
            {
                float x = dashed[line];
                for (float z = HighwayStartZ + 5f; z < 1225f; z += 8f)
                {
                    bool inRoad = z < HighwayEndZ - 3f;
                    if (!inRoad)
                    {
                        float local = (z - (RiverEndZ + 20f)) % 95f;
                        inRoad = z > RiverEndZ + 20f && local >= 0f && local <= 66f;
                    }
                    if (!inRoad) continue;
                    Quad(white, whiteNormals, whiteUvs, whiteTris, x - 0.11f, x + 0.11f, z, z + 3.4f, 0.014f);
                }
            }

            // Solid white edge lines across long course road segments.
            void Edge(float z0, float z1)
            {
                Quad(white, whiteNormals, whiteUvs, whiteTris, -9.38f, -9.16f, z0, z1, 0.014f);
                Quad(white, whiteNormals, whiteUvs, whiteTris, 9.16f, 9.38f, z0, z1, 0.014f);
                Quad(yellow, yellowNormals, yellowUvs, yellowTris, -5.90f, -5.78f, z0, z1, 0.014f);
                Quad(yellow, yellowNormals, yellowUvs, yellowTris, -5.67f, -5.55f, z0, z1, 0.014f);
            }
            Edge(HighwayStartZ + 2f, HighwayEndZ - 2f);
            for (int i = 0; i < 10; i++)
            {
                float segStart = RiverEndZ + 20f + i * 95f;
                float segEnd = Mathf.Min(CourseEndZ - 15f, segStart + 70f);
                if (segEnd > segStart + 5f) Edge(segStart, segEnd);
            }

            CreateMarkingMesh("LaneDashes", white, whiteNormals, whiteUvs, whiteTris, RetroAssets.RoadPaint);
            CreateMarkingMesh("CenterYellow", yellow, yellowNormals, yellowUvs, yellowTris,
                RetroAssets.Solid("MAT_RoadYellow", new Color(0.98f, 0.78f, 0.16f), 0.05f));
        }

        void CreateMarkingMesh(string name, List<Vector3> verts, List<Vector3> norms,
            List<Vector2> uvs, List<int> tris, Material material)
        {
            if (verts.Count == 0) return;
            var mesh = new Mesh { name = name };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();

            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        void BuildDecorations()
        {
            var rng = new System.Random(42069);

            // Tree line along the highway. Keep everything outside the 12 m playable strip.
            for (float z = HighwayStartZ + 4f; z < HighwayEndZ; z += 9f + (float)rng.NextDouble() * 8f)
            {
                float offset = 14.5f + (float)rng.NextDouble() * 19f;
                int variant = rng.Next(3);
                float scale = 0.85f + (float)rng.NextDouble() * 0.65f;
                RetroBuilder.Tree(transform, new Vector3(-offset, 0f, z), variant, scale, rng.Next(999));
                variant = rng.Next(3);
                scale = 0.85f + (float)rng.NextDouble() * 0.65f;
                RetroBuilder.Tree(transform, new Vector3(offset, 0f, z + 3.5f), variant, scale, rng.Next(999));
            }

            // Grass bank grove.
            for (int i = 0; i < 26; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float x = side * (14f + (float)rng.NextDouble() * 18f);
                float z = 178f + (float)rng.NextDouble() * 50f;
                RetroBuilder.Tree(transform, new Vector3(x, 0f, z), rng.Next(3),
                    0.8f + (float)rng.NextDouble() * 0.7f, rng.Next(999));
            }

            // Far-bank and long-course grove framing.
            for (int i = 0; i < 120; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float x = side * (13f + (float)rng.NextDouble() * 22f);
                float z = 310f + (float)rng.NextDouble() * (CourseEndZ - 330f);
                RetroBuilder.Tree(transform, new Vector3(x, 0f, z), rng.Next(3),
                    0.9f + (float)rng.NextDouble() * 0.8f, rng.Next(999));
            }

            // Rocks and roadside posts.
            for (int i = 0; i < 36; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float x = side * (12.8f + (float)rng.NextDouble() * 5f);
                float z = HighwayStartZ + 10f + (float)rng.NextDouble() * (HighwayEndZ - HighwayStartZ - 10f);
                RetroBuilder.Rock(transform, new Vector3(x, 0f, z),
                    0.5f + (float)rng.NextDouble() * 0.8f, i);
            }

            for (float z = HighwayStartZ + 14f; z < HighwayEndZ - 8f; z += 22f)
            {
                BuildRoadPost(new Vector3(-9.95f, 0f, z));
                BuildRoadPost(new Vector3(9.95f, 0f, z));
            }

            BuildWarningSign(new Vector3(-8.1f, 0f, 222f));
            BuildWarningSign(new Vector3(8.1f, 0f, 222f));
            BuildFinishGate();

            // Distant hills close the horizon without any collision cost.
            for (int i = 0; i < 18; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float x = side * (65f + (i % 5) * 8f);
                float z = 130f + i * 65f;
                RetroBuilder.Cone("Hill" + i, transform, new Vector3(x, -1.5f, z),
                    new Vector3(34f + (i % 3) * 8f, 22f + (i % 4) * 4f, 34f),
                    RetroAssets.Leaf0, false, false, false);
            }
        }

        void BuildRoadPost(Vector3 position)
        {
            RetroBuilder.Box("RoadPost", transform, position + new Vector3(0f, 0.55f, 0f),
                new Vector3(0.16f, 1.1f, 0.16f), RetroAssets.White, false, false, true);
            RetroBuilder.Box("Reflector", transform, position + new Vector3(0f, 0.86f, 0f),
                new Vector3(0.22f, 0.16f, 0.20f),
                RetroAssets.Solid("MAT_Reflector", new Color(0.95f, 0.42f, 0.12f), 0.25f, true),
                false, false, true);
        }

        void BuildWarningSign(Vector3 position)
        {
            RetroBuilder.Cylinder("SignPost", transform, position + new Vector3(0f, 1.0f, 0f),
                new Vector3(0.16f, 2.0f, 0.16f), RetroAssets.Wood, false, true, true);
            Material warning = RetroAssets.Surface(RetroAssets.Checker, 4f, 1f);
            RetroBuilder.Box("WarningSign", transform, position + new Vector3(0f, 1.9f, 0f),
                new Vector3(2.2f, 1.0f, 0.10f), warning, false, false, true,
                Quaternion.Euler(0f, 0f, 6f));
        }

        void BuildFinishGate()
        {
            float z = FinishZ;
            for (int side = -1; side <= 1; side += 2)
            {
                RetroBuilder.Box("FinishPillar", transform, new Vector3(side * 7.2f, 2.7f, z),
                    new Vector3(0.78f, 5.4f, 0.78f), RetroAssets.Stone);
                RetroBuilder.Box("FinishCap", transform, new Vector3(side * 7.2f, 5.55f, z),
                    new Vector3(1.3f, 0.3f, 1.3f), RetroAssets.Bark);
            }

            RetroBuilder.Box("FinishBeam", transform, new Vector3(0f, 5.55f, z),
                new Vector3(15.4f, 0.55f, 0.80f), RetroAssets.Checker);

            Material bannerMat = RetroAssets.Surface(RetroAssets.Checker, 10f, 1f);
            RetroBuilder.Box("FinishBanner", transform, new Vector3(0f, 4.45f, z),
                new Vector3(13.2f, 1.75f, 0.22f), bannerMat);

            // Small bunting flags add colour above the banner.
            for (int i = -4; i <= 4; i++)
            {
                Material flag = i % 2 == 0 ? RetroAssets.RoadPaint : RetroAssets.Solid(
                    "MAT_Flag", new Color(0.92f, 0.28f, 0.16f), 0.05f);
                RetroBuilder.Box("Bunting" + i, transform, new Vector3(i * 1.45f, 6.12f, z),
                    new Vector3(0.55f, 0.45f, 0.08f), flag, false, false, true,
                    Quaternion.Euler(0f, 0f, 45f));
            }

            RetroBuilder.Box("FinishLine", transform, new Vector3(0f, 0.015f, z),
                new Vector3(12.8f, 0.02f, 1.4f), RetroAssets.Checker, false, false, true);
        }

        void BuildClouds()
        {
            var rng = new System.Random(7311);
            for (int i = 0; i < 40; i++)
            {
                var cluster = new GameObject("Cloud" + i);
                cluster.transform.SetParent(transform, false);
                cluster.transform.localPosition = new Vector3(
                    -90f + (float)rng.NextDouble() * 180f,
                    25f + (float)rng.NextDouble() * 26f,
                    -25f + i * 34f + (float)rng.NextDouble() * 10f);

                int puffs = 3 + rng.Next(3);
                for (int p = 0; p < puffs; p++)
                {
                    float sx = 4f + (float)rng.NextDouble() * 5f;
                    float sy = 1.8f + (float)rng.NextDouble() * 1.8f;
                    float sz = 3f + (float)rng.NextDouble() * 4f;
                    var puff = RetroBuilder.Box("Puff", cluster.transform,
                        new Vector3((float)rng.NextDouble() * 8f - 4f,
                            (float)rng.NextDouble() * 1.6f,
                            (float)rng.NextDouble() * 4f - 2f),
                        new Vector3(sx, sy, sz), RetroAssets.Cloud, false, false, false);
                    var puffRenderer = puff.GetComponent<MeshRenderer>();
                    if (puffRenderer != null) puffRenderer.receiveShadows = false;
                }
                cluster.AddComponent<CloudDrift>();
            }
        }
    }
}
