using UnityEngine;

namespace FrogRunner.Core
{
    /// <summary>
    /// Shared retro materials. Built-in RP Standard shader + nearest-neighbour textures.
    /// Culling is disabled on generated meshes so low-poly silhouettes never disappear
    /// because of inverted winding. TUNING: raise smoothness slightly for wet logs/water.
    /// </summary>
    public static class RetroAssets
    {
        static Material _grass, _road, _dirt, _roadPaint, _white, _wood, _bark;
        static Material _leaf0, _leaf1, _leaf2, _water, _stone, _checker;
        static Material _frogSkin, _frogBelly, _eyeWhite, _eyeBlack, _glider, _cloud;
        static Texture2D _grassTex, _roadTex, _dirtTex, _paintTex, _whiteTex, _woodTex, _barkTex;
        static Texture2D _waterTex, _stoneTex, _vehiclePaintTex;

        public static Texture2D GrassTexture { get { if (_grassTex == null) _grassTex = PixelTextureFactory.Grass(); return _grassTex; } }
        public static Texture2D RoadTexture { get { if (_roadTex == null) _roadTex = PixelTextureFactory.Asphalt(); return _roadTex; } }
        public static Texture2D DirtTexture { get { if (_dirtTex == null) _dirtTex = PixelTextureFactory.Dirt(); return _dirtTex; } }
        public static Texture2D PaintTexture { get { if (_paintTex == null) _paintTex = PixelTextureFactory.RoadPaint(); return _paintTex; } }
        public static Texture2D WhiteTexture { get { if (_whiteTex == null) _whiteTex = PixelTextureFactory.White(); return _whiteTex; } }
        public static Texture2D WoodTexture { get { if (_woodTex == null) _woodTex = PixelTextureFactory.Wood(); return _woodTex; } }
        public static Texture2D BarkTexture { get { if (_barkTex == null) _barkTex = PixelTextureFactory.Bark(); return _barkTex; } }
        public static Texture2D WaterTexture { get { if (_waterTex == null) _waterTex = PixelTextureFactory.Water(); return _waterTex; } }
        public static Texture2D StoneTexture { get { if (_stoneTex == null) _stoneTex = PixelTextureFactory.Stone(); return _stoneTex; } }
        public static Texture2D VehiclePaintTexture { get { if (_vehiclePaintTex == null) _vehiclePaintTex = PixelTextureFactory.VehiclePaint(); return _vehiclePaintTex; } }

        public static Material Grass { get { if (_grass == null) _grass = Make("MAT_Grass", GrassTexture, Color.white, 0.02f); return _grass; } }
        public static Material Road { get { if (_road == null) _road = Make("MAT_Road", RoadTexture, Color.white, 0.03f); return _road; } }
        public static Material Dirt { get { if (_dirt == null) _dirt = Make("MAT_Dirt", DirtTexture, Color.white, 0.02f); return _dirt; } }
        public static Material RoadPaint { get { if (_roadPaint == null) _roadPaint = Make("MAT_RoadPaint", PaintTexture, Color.white, 0.05f); return _roadPaint; } }
        public static Material White { get { if (_white == null) _white = Make("MAT_White", WhiteTexture, Color.white, 0.05f); return _white; } }
        public static Material Wood { get { if (_wood == null) _wood = Make("MAT_Wood", WoodTexture, Color.white, 0.04f); return _wood; } }
        public static Material Bark { get { if (_bark == null) _bark = Make("MAT_Bark", BarkTexture, Color.white, 0.02f); return _bark; } }
        public static Material Leaf0 { get { if (_leaf0 == null) _leaf0 = Make("MAT_Leaf0", LeafTexture(0), Color.white, 0.02f); return _leaf0; } }
        public static Material Leaf1 { get { if (_leaf1 == null) _leaf1 = Make("MAT_Leaf1", LeafTexture(1), Color.white, 0.02f); return _leaf1; } }
        public static Material Leaf2 { get { if (_leaf2 == null) _leaf2 = Make("MAT_Leaf2", LeafTexture(2), Color.white, 0.02f); return _leaf2; } }
        public static Material Water { get { if (_water == null) _water = Make("MAT_Water", WaterTexture, Color.white, 0.22f); return _water; } }
        public static Material Stone { get { if (_stone == null) _stone = Make("MAT_Stone", StoneTexture, Color.white, 0.03f); return _stone; } }
        public static Material Checker { get { if (_checker == null) _checker = Make("MAT_Checker", CheckerTexture(), Color.white, 0.05f); return _checker; } }
        public static Material FrogSkin { get { if (_frogSkin == null) _frogSkin = Make("MAT_FrogSkin", FrogSkinTexture(), Color.white, 0.05f); return _frogSkin; } }
        public static Material FrogBelly { get { if (_frogBelly == null) _frogBelly = Make("MAT_FrogBelly", FrogBellyTexture(), Color.white, 0.08f); return _frogBelly; } }
        public static Material EyeWhite { get { if (_eyeWhite == null) _eyeWhite = Solid("MAT_EyeWhite", new Color(0.96f, 0.96f, 0.88f)); return _eyeWhite; } }
        public static Material EyeBlack { get { if (_eyeBlack == null) _eyeBlack = Solid("MAT_EyeBlack", new Color(0.04f, 0.05f, 0.05f)); return _eyeBlack; } }
        public static Material Glider { get { if (_glider == null) _glider = Make("MAT_Glider", GliderTexture(), Color.white, 0.10f); return _glider; } }
        public static Material Metal { get { if (_metal == null) _metal = Solid("MAT_Metal", new Color(0.34f, 0.35f, 0.38f), 0.3f); return _metal; } }
        public static Material Cloud { get { if (_cloud == null) _cloud = Solid("MAT_Cloud", new Color(0.98f, 0.99f, 1f), 0f, true); return _cloud; } }
        static Material _metal;

        public static Material Surface(Material source, float u, float v)
        {
            var mat = new Material(source);
            mat.name = source.name + "_Tiled";
            if (mat.HasProperty("_MainTex")) mat.mainTextureScale = new Vector2(u, v);
            return mat;
        }

        public static Material Tinted(string name, Color color, float smoothness = 0.08f)
        {
            var mat = Make(name, VehiclePaintTexture, color, smoothness);
            return mat;
        }

        public static Material Solid(string name, Color color, float smoothness = 0.04f, bool emission = false)
        {
            var mat = Make(name, WhiteTexture, color, smoothness, emission);
            return mat;
        }

        static Material Make(string name, Texture2D texture, Color color, float smoothness, bool emission = false)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Unlit/Texture");
            var mat = new Material(shader) { name = name, enableInstancing = true };
            if (mat.HasProperty("_MainTex")) mat.mainTexture = texture;
            if (mat.HasProperty("_Color")) mat.color = color;
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            // Generated meshes have consistent outward normals, but disabling culling makes the
            // PS1-style double-sided silhouettes robust to extreme camera angles.
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);
            if (emission && mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(0.20f, 0.21f, 0.22f));
            }
            return mat;
        }

        static Texture2D LeafTexture(int variant) => PixelTextureFactory.Leaves(variant);
        static Texture2D CheckerTexture() => PixelTextureFactory.Checker();
        static Texture2D FrogSkinTexture() => PixelTextureFactory.FrogSkin();
        static Texture2D FrogBellyTexture() => PixelTextureFactory.FrogBelly();
        static Texture2D GliderTexture() => PixelTextureFactory.GliderCanopy();
    }
}
