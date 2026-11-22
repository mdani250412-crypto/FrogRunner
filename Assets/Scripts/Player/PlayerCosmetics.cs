using FrogRunner.SaveSystem;
using UnityEngine;

namespace FrogRunner.Player
{
    /// <summary>Applies saved Closet customization (color/hat/name) to a frog instance. Lightweight runtime visuals.</summary>
    [DisallowMultipleComponent]
    public class PlayerCosmetics : MonoBehaviour
    {
        public static readonly Color[] FrogColors =
        {
            new Color(0.35f, 0.85f, 0.35f), // Green
            new Color(0.20f, 0.85f, 0.95f), // Cyan
            new Color(0.95f, 0.55f, 0.25f), // Orange
            new Color(0.85f, 0.35f, 0.85f), // Purple
            new Color(0.95f, 0.9f, 0.35f),  // Yellow
            new Color(0.92f, 0.92f, 0.92f)  // White
        };
        public static readonly string[] HatNames = { "None", "Cap", "Crown", "Wizard", "Helmet" };

        public string PlayerName { get; private set; } = "Frog";
        public int ColorIndex { get; private set; }
        public int HatIndex { get; private set; }

        Transform _hatRoot;

        public void ApplyFromProfile(PlayerProfileData profile)
        {
            if (profile == null) return;
            PlayerName = string.IsNullOrWhiteSpace(profile.playerName) ? "Frog" : profile.playerName;
            ColorIndex = Mathf.Clamp(profile.frogColorIndex, 0, FrogColors.Length - 1);
            HatIndex = Mathf.Clamp(profile.hatIndex, 0, HatNames.Length - 1);
            ApplyVisuals();
        }

        public void Apply(int colorIndex, int hatIndex, string name)
        {
            ColorIndex = Mathf.Clamp(colorIndex, 0, FrogColors.Length - 1);
            HatIndex = Mathf.Clamp(hatIndex, 0, HatNames.Length - 1);
            PlayerName = string.IsNullOrWhiteSpace(name) ? "Frog" : name;
            ApplyVisuals();
        }

        public static Color GetColor(int index) => FrogColors[Mathf.Clamp(index, 0, FrogColors.Length - 1)];

        void OnEnable() { ApplyVisuals(); }

        public void Reapply() => ApplyVisuals();

        void ApplyVisuals()
        {
            Color c = FrogColors[ColorIndex];
            var renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null) continue;
                string n = r.name;
                // Never tint shield, hat, name tag, eyes, mouth, belly, glider.
                if (n.Contains("ForceField") || n.Contains("NameTag") || n.Contains("Eye") || n.Contains("Mouth") || n.Contains("Belly") || n.Contains("Glider") || n.Contains("Hat") || n.Contains("Brim") || n.Contains("Crown") || n.Contains("Visor") || n.Contains("Ridge") || n.Contains("Star") || n.Contains("Gem"))
                    continue;
                bool skin = n.Contains("Body") || n.Contains("Head") || n.Contains("Arm") || n.Contains("Leg") || n.Contains("Thigh") || n.Contains("Shin") || n.Contains("Foot") || n.Contains("Hand") || n.Contains("Cheek") || n.Contains("Webbed") || n.Contains("UpperArm") || n.Contains("Forearm");
                if (!skin) continue;
                var mat = r.material; // instance per frog
                if (mat != null)
                {
                    if (mat.HasProperty("_Color")) mat.color = c;
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                }
            }

            BuildHat();
        }

        void BuildHat()
        {
            if (_hatRoot != null) Destroy(_hatRoot.gameObject);
            if (HatIndex <= 0) return;
            Transform head = transform.Find("Visual/BodyRoot/HeadPivot");
            if (head == null) return;

            var rootGo = new GameObject("HatRoot");
            _hatRoot = rootGo.transform;
            _hatRoot.SetParent(head, false);
            _hatRoot.localPosition = Vector3.zero;
            _hatRoot.localRotation = Quaternion.identity;
            _hatRoot.localScale = Vector3.one;

            switch (HatIndex)
            {
                case 1: BuildCap(_hatRoot); break;
                case 2: BuildCrown(_hatRoot); break;
                case 3: BuildWizard(_hatRoot); break;
                case 4: BuildHelmet(_hatRoot); break;
            }
        }

        static Material HatMat(Color c)
        {
            var sh = Shader.Find("Standard") ?? Shader.Find("Diffuse");
            var m = new Material(sh) { enableInstancing = true };
            if (m.HasProperty("_Color")) m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.05f);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            // Point-filtered 1x1 texture gives crisp pixel look even on solid colors.
            var tex = FrogRunner.Core.RetroAssets.WhiteTexture;
            if (tex != null && m.HasProperty("_MainTex")) { m.mainTexture = tex; m.mainTextureScale = Vector2.one; }
            return m;
        }

        static void Part(Transform parent, string name, PrimitiveType type, Vector3 lp, Vector3 ls, Color c, Vector3 le = default)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = lp;
            go.transform.localScale = ls;
            go.transform.localEulerAngles = le;
            var col = go.GetComponent<Collider>(); if (col != null) Object.Destroy(col);
            var r = go.GetComponent<Renderer>(); if (r != null) r.sharedMaterial = HatMat(c);
        }

        static void BuildCap(Transform r)
        {
            Color c = new Color(0.9f, 0.2f, 0.2f);
            Part(r, "HatCap", PrimitiveType.Sphere, new Vector3(0f, 0.32f, 0f), new Vector3(0.62f, 0.28f, 0.62f), c);
            Part(r, "HatBrim", PrimitiveType.Cylinder, new Vector3(0f, 0.26f, 0.16f), new Vector3(0.62f, 0.03f, 0.72f), c);
            Part(r, "HatButton", PrimitiveType.Sphere, new Vector3(0f, 0.44f, 0f), new Vector3(0.1f, 0.1f, 0.1f), Color.white);
        }

        static void BuildCrown(Transform r)
        {
            Color gold = new Color(1f, 0.84f, 0.2f);
            Part(r, "HatBand", PrimitiveType.Cylinder, new Vector3(0f, 0.34f, 0f), new Vector3(0.58f, 0.08f, 0.58f), gold);
            for (int i = 0; i < 5; i++)
            {
                float a = i * 72f * Mathf.Deg2Rad;
                Vector3 p = new Vector3(Mathf.Cos(a) * 0.22f, 0.45f, Mathf.Sin(a) * 0.22f);
                Part(r, "HatPoint", PrimitiveType.Cube, p, new Vector3(0.07f, 0.18f, 0.07f), gold);
                Part(r, "HatGem", PrimitiveType.Sphere, p + Vector3.up * 0.1f, new Vector3(0.05f, 0.05f, 0.05f), new Color(0.9f, 0.2f, 0.3f));
            }
        }

        static void BuildWizard(Transform r)
        {
            Color purple = new Color(0.35f, 0.2f, 0.65f);
            Part(r, "HatBrim", PrimitiveType.Cylinder, new Vector3(0f, 0.28f, 0f), new Vector3(0.92f, 0.025f, 0.92f), purple);
            Part(r, "HatCone1", PrimitiveType.Cylinder, new Vector3(0f, 0.44f, 0f), new Vector3(0.52f, 0.16f, 0.52f), purple);
            Part(r, "HatCone2", PrimitiveType.Cylinder, new Vector3(0.03f, 0.62f, 0f), new Vector3(0.34f, 0.16f, 0.34f), purple, new Vector3(0, 0, 8f));
            Part(r, "HatCone3", PrimitiveType.Cylinder, new Vector3(0.08f, 0.78f, 0f), new Vector3(0.18f, 0.14f, 0.18f), purple, new Vector3(0, 0, 16f));
            Part(r, "HatStar", PrimitiveType.Sphere, new Vector3(-0.08f, 0.52f, 0.22f), new Vector3(0.06f, 0.06f, 0.02f), new Color(1f, 0.9f, 0.3f));
            Part(r, "HatStar2", PrimitiveType.Sphere, new Vector3(0.12f, 0.68f, 0.14f), new Vector3(0.05f, 0.05f, 0.02f), new Color(1f, 0.9f, 0.3f));
        }

        static void BuildHelmet(Transform r)
        {
            Color metal = new Color(0.62f, 0.66f, 0.72f);
            Part(r, "HatDome", PrimitiveType.Sphere, new Vector3(0f, 0.33f, 0.02f), new Vector3(0.66f, 0.42f, 0.68f), metal);
            Part(r, "HatVisor", PrimitiveType.Cube, new Vector3(0f, 0.31f, 0.28f), new Vector3(0.52f, 0.1f, 0.18f), new Color(0.25f, 0.28f, 0.32f));
            Part(r, "HatRidge", PrimitiveType.Cube, new Vector3(0f, 0.52f, 0f), new Vector3(0.08f, 0.08f, 0.55f), new Color(0.75f, 0.78f, 0.82f));
        }
    }
}
