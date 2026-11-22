using UnityEngine;

namespace FrogRunner.Core
{
    /// <summary>
    /// Generates tiny hand-made looking pixel textures at runtime.
    /// Every texture uses Point filtering and Repeat wrapping so world materials stay crunchy
    /// (no bilinear smear) regardless of render scale.
    /// TUNING: change the texture sizes (16/32) or palettes below to make the game more or less pixelated.
    /// </summary>
    public static class PixelTextureFactory
    {
        public static Texture2D Grass()
        {
            return Build("TEX_Grass", 16, 16, (x, y) =>
            {
                float h = Hash(x, y, 11);
                float blade = Hash(x * 3 + 1, y * 7 + 5, 23);
                Color32 c = h < 0.18f ? C(0.12f, 0.40f, 0.10f)
                    : h < 0.52f ? C(0.18f, 0.56f, 0.14f)
                    : h < 0.84f ? C(0.24f, 0.68f, 0.18f)
                    : C(0.33f, 0.80f, 0.23f);
                if (blade > 0.92f) c = C(0.10f, 0.34f, 0.09f);
                if (blade < 0.05f) c = C(0.40f, 0.86f, 0.30f);
                return c;
            });
        }

        public static Texture2D Dirt()
        {
            return Build("TEX_Dirt", 16, 16, (x, y) =>
            {
                float h = Hash(x, y, 41);
                return h < 0.25f ? C(0.32f, 0.23f, 0.14f)
                    : h < 0.7f ? C(0.42f, 0.31f, 0.18f)
                    : C(0.53f, 0.40f, 0.24f);
            });
        }

        public static Texture2D Asphalt()
        {
            return Build("TEX_Asphalt", 16, 16, (x, y) =>
            {
                float h = Hash(x, y, 73);
                float crack = Hash(x / 2, y * 5, 91);
                Color32 c = h < 0.35f ? C(0.15f, 0.15f, 0.17f)
                    : h < 0.78f ? C(0.20f, 0.20f, 0.23f)
                    : C(0.28f, 0.28f, 0.31f);
                if (crack > 0.93f) c = C(0.10f, 0.10f, 0.12f);
                return c;
            });
        }

        public static Texture2D RoadPaint()
        {
            return Build("TEX_RoadPaint", 8, 8, (x, y) =>
                Hash(x, y, 101) < 0.12f ? C(0.80f, 0.80f, 0.76f) : C(0.96f, 0.96f, 0.90f));
        }

        public static Texture2D Wood()
        {
            return Build("TEX_Wood", 16, 16, (x, y) =>
            {
                int grain = (x * 5 + y * 2) % 7;
                float h = Hash(x, y, 131);
                Color32 c = h < 0.2f ? C(0.26f, 0.15f, 0.07f)
                    : h < 0.75f ? C(0.43f, 0.26f, 0.12f)
                    : C(0.55f, 0.36f, 0.18f);
                if (grain == 0) c = C(0.30f, 0.18f, 0.08f);
                if (grain == 3) c = C(0.52f, 0.33f, 0.16f);
                return c;
            });
        }

        public static Texture2D Bark()
        {
            return Build("TEX_Bark", 16, 16, (x, y) =>
            {
                int stripe = (x * 3 + y) % 6;
                float h = Hash(x, y, 151);
                Color32 c = h < 0.25f ? C(0.20f, 0.12f, 0.07f)
                    : h < 0.8f ? C(0.32f, 0.20f, 0.11f)
                    : C(0.45f, 0.30f, 0.16f);
                if (stripe == 0) c = C(0.17f, 0.10f, 0.06f);
                return c;
            });
        }

        public static Texture2D Leaves(int variant)
        {
            int seed = 200 + variant * 37;
            return Build("TEX_Leaves" + variant, 16, 16, (x, y) =>
            {
                float h = Hash(x, y, seed);
                float clump = Hash(x / 3, y / 3, seed + 5);
                Color32 baseA = variant == 0 ? C(0.12f, 0.45f, 0.12f) : variant == 1 ? C(0.18f, 0.52f, 0.16f) : C(0.27f, 0.62f, 0.20f);
                Color32 light = variant == 0 ? C(0.24f, 0.68f, 0.20f) : variant == 1 ? C(0.31f, 0.74f, 0.24f) : C(0.40f, 0.82f, 0.30f);
                Color32 dark = variant == 0 ? C(0.07f, 0.30f, 0.08f) : variant == 1 ? C(0.10f, 0.36f, 0.10f) : C(0.16f, 0.44f, 0.13f);
                Color32 c = h < 0.28f ? dark : h < 0.78f ? baseA : light;
                if (clump > 0.86f) c = C(0.56f, 0.72f, 0.22f); // tiny sunlit leaves
                return c;
            });
        }

        public static Texture2D Water()
        {
            return Build("TEX_Water", 32, 32, (x, y) =>
            {
                float wave = Mathf.Sin((y * 2f + x * 0.7f) * 0.55f) * 0.5f + 0.5f;
                float bands = Mathf.Sin((y + x * 0.35f) * 0.9f) * 0.5f + 0.5f;
                float h = Hash(x, y, 303);
                Color32 c = C(0.08f + wave * 0.07f, 0.36f + bands * 0.12f, 0.56f + wave * 0.16f);
                if (Mathf.Abs(bands - 0.5f) < 0.035f) c = C(0.45f, 0.78f, 0.92f); // ripple highlight
                if (h > 0.975f) c = C(0.80f, 0.95f, 1f); // sparkle
                return c;
            });
        }

        public static Texture2D FrogSkin()
        {
            return Build("TEX_FrogSkin", 16, 16, (x, y) =>
            {
                float h = Hash(x, y, 401);
                float spot = Hash(x / 2, y / 2, 409);
                Color32 c = h < 0.2f ? C(0.20f, 0.48f, 0.18f)
                    : h < 0.8f ? C(0.31f, 0.66f, 0.24f)
                    : C(0.44f, 0.78f, 0.30f);
                if (spot < 0.12f) c = C(0.16f, 0.38f, 0.14f);
                if (h > 0.965f) c = C(0.65f, 0.88f, 0.35f); // highlight freckle
                return c;
            });
        }

        public static Texture2D FrogBelly()
        {
            return Build("TEX_FrogBelly", 16, 16, (x, y) =>
            {
                float h = Hash(x, y, 421);
                return h < 0.25f ? C(0.80f, 0.76f, 0.44f) : h < 0.8f ? C(0.92f, 0.88f, 0.58f) : C(0.98f, 0.95f, 0.70f);
            });
        }

        public static Texture2D VehiclePaint()
        {
            return Build("TEX_VehiclePaint", 8, 8, (x, y) =>
            {
                float h = Hash(x, y, 501);
                return h < 0.15f ? C(0.72f, 0.72f, 0.72f) : h < 0.85f ? C(0.92f, 0.92f, 0.92f) : C(0.99f, 0.99f, 0.99f);
            });
        }

        public static Texture2D GliderCanopy()
        {
            return Build("TEX_Glider", 32, 16, (x, y) =>
            {
                if (y < 2) return C(0.16f, 0.11f, 0.10f); // dark keel edge
                if (x < 2 || x > 29) return C(0.13f, 0.10f, 0.10f);
                bool stripe = (x / 4) % 2 == 0;
                Color32 c = stripe ? C(0.90f, 0.24f, 0.16f) : C(0.98f, 0.76f, 0.18f);
                if (y > 12) c = stripe ? C(0.98f, 0.40f, 0.22f) : C(1f, 0.86f, 0.34f);
                return c;
            });
        }

        public static Texture2D Checker()
        {
            return Build("TEX_Checker", 8, 8, (x, y) => ((x / 2 + y / 2) % 2 == 0) ? C(0.95f, 0.95f, 0.90f) : C(0.08f, 0.08f, 0.09f));
        }

        public static Texture2D Stone()
        {
            return Build("TEX_Stone", 16, 16, (x, y) =>
            {
                float h = Hash(x, y, 601);
                float block = Hash(x / 4, y / 4, 607);
                Color32 c = h < 0.3f ? C(0.35f, 0.35f, 0.36f) : h < 0.8f ? C(0.47f, 0.46f, 0.45f) : C(0.58f, 0.56f, 0.52f);
                if (block < 0.12f) c = C(0.30f, 0.30f, 0.31f);
                return c;
            });
        }

        public static Texture2D UIPanel()
        {
            return Build("TEX_UIPanel", 32, 32, (x, y) =>
            {
                bool border = x < 2 || y < 2 || x > 29 || y > 29;
                bool inner = x < 4 || y < 4 || x > 27 || y > 27;
                float h = Hash(x, y, 701);
                if (border) return h < 0.35f ? C(0.35f, 0.26f, 0.13f) : C(0.62f, 0.48f, 0.24f);
                if (inner) return C(0.10f, 0.09f, 0.08f);
                Color32 c = h < 0.18f ? C(0.10f, 0.10f, 0.12f) : h < 0.82f ? C(0.15f, 0.14f, 0.17f) : C(0.19f, 0.18f, 0.21f);
                if (y > 26 && h > 0.5f) c = C(0.24f, 0.22f, 0.20f); // worn top highlight
                return c;
            }, false);
        }

        public static Texture2D UIButton(bool hover, bool pressed)
        {
            string name = pressed ? "TEX_UIButtonDown" : hover ? "TEX_UIButtonHover" : "TEX_UIButton";
            return Build(name, 32, 16, (x, y) =>
            {
                bool border = x < 1 || y < 1 || x > 30 || y > 14;
                float h = Hash(x, y, pressed ? 733 : hover ? 731 : 727);
                if (border) return pressed ? C(0.26f, 0.20f, 0.12f) : hover ? C(0.96f, 0.76f, 0.38f) : C(0.70f, 0.55f, 0.28f);
                if (y > 12) return pressed ? C(0.18f, 0.15f, 0.12f) : C(0.55f, 0.44f, 0.26f); // top bevel
                if (y < 2) return C(0.10f, 0.08f, 0.07f); // bottom shadow
                Color32 c = pressed ? C(0.24f, 0.19f, 0.14f) : hover ? C(0.62f, 0.48f, 0.29f) : C(0.43f, 0.35f, 0.24f);
                if (h > 0.9f) c = C(0.50f, 0.40f, 0.26f);
                return c;
            }, false);
        }

        public static Texture2D Heart(bool filled)
        {
            // 9x8 pixel heart, crisp edges
            string[] mask =
            {
                "01100110",
                "11111111",
                "11111111",
                "11111111",
                "01111110",
                "00111100",
                "00011000",
                "00000000"
            };
            return Build(filled ? "TEX_Heart" : "TEX_HeartEmpty", 8, 8, (x, y) =>
            {
                bool on = mask[7 - y][x] == '1';
                if (!on) return new Color32(0, 0, 0, 0);
                return filled ? C(0.94f, 0.22f, 0.18f) : C(0.32f, 0.18f, 0.17f);
            }, false);
        }

        public static Texture2D White()
        {
            return Build("TEX_White", 4, 4, (x, y) => C(1f, 1f, 1f));
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        static Texture2D Build(string name, int w, int h, System.Func<int, int, Color32> paint, bool repeat = true)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, false)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp,
                anisoLevel = 0
            };
            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++) pixels[y * w + x] = paint(x, y);
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return tex;
        }

        static Color32 C(float r, float g, float b) => new Color32((byte)(r * 255f), (byte)(g * 255f), (byte)(b * 255f), 255);

        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int n = x * 374761393 + y * 668265263 + seed * 69069;
                n = (n ^ (n >> 13)) * 1274126177;
                return ((n >> 8) & 0xFFFF) / 65535f;
            }
        }
    }
}
