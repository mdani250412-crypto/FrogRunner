using UnityEngine;

namespace FrogRunner.Core
{
    /// <summary>
    /// Tiny helper for building low-poly props out of shared meshes.
    /// TUNING: pass collider=true for anything the frog can stand on or bump into.
    /// </summary>
    public static class RetroBuilder
    {
        public static GameObject Box(
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 size,
            Material material,
            bool collider = false,
            bool castShadows = true,
            bool receiveShadows = true,
            Quaternion? localRotation = null)
        {
            var go = CreateMeshObject(name, parent, RetroMeshFactory.Cube, material, localPosition, size, localRotation, castShadows, receiveShadows);
            if (collider) AddBoxCollider(go);
            return go;
        }

        public static GameObject Sphere(
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 size,
            Material material,
            bool collider = false,
            bool castShadows = true,
            bool receiveShadows = true,
            Quaternion? localRotation = null)
        {
            var go = CreateMeshObject(name, parent, RetroMeshFactory.Sphere, material, localPosition, size, localRotation, castShadows, receiveShadows);
            if (collider) AddBoxCollider(go);
            return go;
        }

        public static GameObject Cylinder(
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 size,
            Material material,
            bool collider = false,
            bool castShadows = true,
            bool receiveShadows = true,
            Quaternion? localRotation = null)
        {
            var go = CreateMeshObject(name, parent, RetroMeshFactory.Cylinder, material, localPosition, size, localRotation, castShadows, receiveShadows);
            if (collider) AddBoxCollider(go);
            return go;
        }

        public static GameObject Cone(
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 size,
            Material material,
            bool collider = false,
            bool castShadows = true,
            bool receiveShadows = true,
            Quaternion? localRotation = null)
        {
            var go = CreateMeshObject(name, parent, RetroMeshFactory.Cone, material, localPosition, size, localRotation, castShadows, receiveShadows);
            if (collider) AddBoxCollider(go);
            return go;
        }

        public static GameObject Wing(
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 size,
            Material material,
            Quaternion? localRotation = null)
        {
            return CreateMeshObject(name, parent, RetroMeshFactory.Wing, material, localPosition, size, localRotation, true, false);
        }

        static GameObject CreateMeshObject(
            string name,
            Transform parent,
            Mesh mesh,
            Material material,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion? localRotation,
            bool castShadows,
            bool receiveShadows)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = localRotation ?? Quaternion.identity;
            go.transform.localScale = localScale;

            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = castShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = receiveShadows;
            return go;
        }

        static void AddBoxCollider(GameObject go)
        {
            var box = go.AddComponent<BoxCollider>();
            box.size = Vector3.one;
        }

        /// <summary>
        /// Blocky tree with three distinct silhouettes. Trees sit outside the playable lane,
        /// so they have no colliders and cost one draw per part.
        /// </summary>
        public static GameObject Tree(Transform parent, Vector3 position, int variant, float scale, int seed)
        {
            var root = new GameObject("Tree_" + variant);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;
            root.transform.localRotation = Quaternion.Euler(0f, (seed * 37) % 360, 0f);
            root.transform.localScale = Vector3.one * scale;

            Material bark = RetroAssets.Bark;
            Material leaf = variant % 3 == 0 ? RetroAssets.Leaf0 : variant % 3 == 1 ? RetroAssets.Leaf1 : RetroAssets.Leaf2;

            if (variant % 3 == 0)
            {
                // Broad blocky oak / stacked cubes.
                Box("Trunk", root.transform, new Vector3(0f, 1.0f, 0f), new Vector3(0.55f, 2.0f, 0.55f), bark);
                Box("CanopyA", root.transform, new Vector3(0f, 2.55f, 0f), new Vector3(2.5f, 1.5f, 2.5f), leaf, false, true, true, Quaternion.Euler(0f, 45f, 0f));
                Box("CanopyB", root.transform, new Vector3(0.15f, 3.35f, -0.1f), new Vector3(1.75f, 1.25f, 1.75f), leaf, false, true, true, Quaternion.Euler(0f, 18f, 0f));
                Box("CanopyC", root.transform, new Vector3(-0.1f, 3.95f, 0.05f), new Vector3(1.0f, 0.9f, 1.0f), leaf, false, true, true, Quaternion.Euler(0f, 62f, 0f));
            }
            else if (variant % 3 == 1)
            {
                // Pine / stacked cones.
                Box("Trunk", root.transform, new Vector3(0f, 0.9f, 0f), new Vector3(0.42f, 1.8f, 0.42f), bark);
                Cone("CanopyA", root.transform, new Vector3(0f, 2.15f, 0f), new Vector3(2.6f, 2.2f, 2.6f), leaf);
                Cone("CanopyB", root.transform, new Vector3(0f, 3.15f, 0f), new Vector3(2.0f, 1.9f, 2.0f), leaf);
                Cone("CanopyC", root.transform, new Vector3(0f, 4.05f, 0f), new Vector3(1.35f, 1.5f, 1.35f), leaf);
            }
            else
            {
                // Tall, narrow palm-ish / spruce silhouette.
                Box("Trunk", root.transform, new Vector3(0f, 1.25f, 0f), new Vector3(0.38f, 2.5f, 0.38f), bark);
                Box("CanopyA", root.transform, new Vector3(0f, 2.65f, 0f), new Vector3(1.5f, 0.8f, 1.5f), leaf);
                Box("CanopyB", root.transform, new Vector3(0f, 3.35f, 0f), new Vector3(1.2f, 0.7f, 1.2f), leaf);
                Box("CanopyC", root.transform, new Vector3(0f, 3.95f, 0f), new Vector3(0.9f, 0.6f, 0.9f), leaf);
            }

            return root;
        }

        public static GameObject Rock(Transform parent, Vector3 position, float scale, int variant)
        {
            var root = new GameObject("Rock");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;
            root.transform.localRotation = Quaternion.Euler(0f, variant * 47f, 0f);
            root.transform.localScale = Vector3.one;
            Sphere("RockBody", root.transform, new Vector3(0f, scale * 0.25f, 0f), new Vector3(scale * 1.6f, scale * 0.7f, scale * 1.2f), RetroAssets.Stone, false);
            return root;
        }
    }
}
