using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PassportFrenzy.EditorTools
{
    /// <summary>
    /// Generated white UI sprites (tinted via Image.color): a 9-sliced rounded rect, a rounded outline (selection ring),
    /// a soft drop shadow, a plane pictogram and a dash for boarding-pass perforations.
    /// Written to Placeholder/UI (never Assets/Art). Menu: Passport Frenzy/Generate UI Sprites.
    /// </summary>
    public static partial class PrototypeBuilder
    {
        const string UiSpritesDir = Root + "/Placeholder/UI";

        /// <summary>Corner radius (UI units at multiplier 1) of <see cref="RoundSprite"/> and friends.</summary>
        const float BaseRadius = 32f;

        internal static Sprite RoundSprite => UiSpr("UI_Rounded");
        internal static Sprite RingSprite => UiSpr("UI_RoundedOutline");
        internal static Sprite ShadowSprite => UiSpr("UI_SoftShadow");
        internal static Sprite PlaneSprite => UiSpr("UI_Plane");
        internal static Sprite DashSprite => UiSpr("UI_Dash");
        internal static Sprite DotSprite => UiSpr("UI_Circle");

        static Sprite UiSpr(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{UiSpritesDir}/{name}.png");

        [MenuItem("Passport Frenzy/Generate UI Sprites", priority = 20)]
        static void GenerateUiSpritesMenu() => Debug.Log(GenerateUiSprites(true));

        /// <summary>MCP: PassportFrenzy.EditorTools.PrototypeBuilder.GenerateUiSprites(true)</summary>
        public static string GenerateUiSprites(bool overwrite)
        {
            EnsureFolder(UiSpritesDir);
            int made = 0;
            // Rounded rect: 128 px, radius 32 -> 32 px 9-slice borders.
            made += WriteSprite("UI_Rounded", 128, 128, (x, y) => Coverage(SdRoundRect(x, y, 128, 128, 0f, BaseRadius)),
                new Vector4(32, 32, 32, 32), overwrite);
            // Outline ring (selection highlight / photo slots): 7 px stroke.
            made += WriteSprite("UI_RoundedOutline", 128, 128, (x, y) =>
            {
                float d = SdRoundRect(x, y, 128, 128, 0f, BaseRadius);
                return Coverage(d) * (1f - Coverage(d + 7f));
            }, new Vector4(32, 32, 32, 32), overwrite);
            // Outer-only soft shadow: a hole the shape of the panel (inset 24, radius 24) with alpha falling off outside it,
            // so the shadow can be drawn as the panel's first child without darkening the panel itself.
            made += WriteSprite("UI_SoftShadow", 128, 128, (x, y) =>
            {
                float d = SdRoundRect(x, y, 128, 128, 24f, 24f);
                if (d <= 0f) return 0f;
                float a = 1f - Mathf.SmoothStep(0f, 1f, d / 24f);
                return a * a * Mathf.Clamp01(d * 2f);
            }, new Vector4(48, 48, 48, 48), overwrite);
            made += WriteSprite("UI_Circle", 64, 64, (x, y) => Coverage(new Vector2(x - 32f, y - 32f).magnitude - 31f),
                Vector4.zero, overwrite);
            // Perforation dash: an 8x8 rounded dot in a 16 px tall tile (tiled vertically).
            made += WriteSprite("UI_Dash", 8, 16, (x, y) => Coverage(SdRoundRect(x, y - 4f, 8, 8, 0.5f, 3f)), Vector4.zero, overwrite,
                wrap: true);
            made += WriteSprite("UI_Plane", 128, 128, PlaneCoverage, Vector4.zero, overwrite);
            AssetDatabase.Refresh();
            return $"[PrototypeBuilder] UI sprites: {made} written to {UiSpritesDir}";
        }

        // ------------------------------------------------------------------ shapes

        /// <summary>Signed distance (px, negative inside) to a rounded rect inset by <paramref name="inset"/>.</summary>
        static float SdRoundRect(float px, float py, float w, float h, float inset, float r)
        {
            float hx = w * 0.5f - inset, hy = h * 0.5f - inset;
            float qx = Mathf.Abs(px - w * 0.5f) - (hx - r);
            float qy = Mathf.Abs(py - h * 0.5f) - (hy - r);
            float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
            return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        static float Coverage(float signedDistance) => Mathf.Clamp01(0.5f - signedDistance);

        /// <summary>Top-down airliner pictogram pointing right (airport wayfinding style), 4x4 supersampled.</summary>
        static float PlaneCoverage(float x, float y)
        {
            float sum = 0f;
            for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
                if (InPlane(x - 0.5f + (i + 0.5f) / 4f, y - 0.5f + (j + 0.5f) / 4f)) sum += 1f;
            return sum / 16f;
        }

        static bool InPlane(float x, float y)
        {
            // Fuselage: capsule along the x axis.
            float cx = Mathf.Clamp(x, 22f, 108f);
            if (new Vector2(x - cx, y - 64f).magnitude <= 9f) return true;
            // Main wings (swept back) and tail planes, mirrored about y = 64.
            float my = 64f + Mathf.Abs(y - 64f);
            if (InQuad(x, my, new Vector2(78f, 64f), new Vector2(56f, 64f), new Vector2(44f, 116f), new Vector2(56f, 116f))) return true;
            if (InQuad(x, my, new Vector2(36f, 64f), new Vector2(22f, 64f), new Vector2(14f, 88f), new Vector2(22f, 88f))) return true;
            return false;
        }

        static bool InQuad(float x, float y, Vector2 a, Vector2 b, Vector2 c, Vector2 d) =>
            InTri(x, y, a, b, c) || InTri(x, y, a, c, d);

        static bool InTri(float x, float y, Vector2 a, Vector2 b, Vector2 c)
        {
            float Cross(Vector2 p, Vector2 q, float px, float py) => (q.x - p.x) * (py - p.y) - (q.y - p.y) * (px - p.x);
            float d1 = Cross(a, b, x, y), d2 = Cross(b, c, x, y), d3 = Cross(c, a, x, y);
            bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
            return !(neg && pos);
        }

        // ------------------------------------------------------------------ io

        static int WriteSprite(string name, int w, int h, Func<float, float, float> alpha, Vector4 border, bool overwrite,
            bool wrap = false)
        {
            string path = $"{UiSpritesDir}/{name}.png";
            if (!overwrite && File.Exists(path)) return 0;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha(x + 0.5f, y + 0.5f)) * 255f));
            tex.SetPixels32(px);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 100f;
            imp.spriteBorder = border;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.filterMode = FilterMode.Bilinear;
            imp.wrapMode = wrap ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            var s = new TextureImporterSettings();
            imp.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect;
            imp.SetTextureSettings(s);
            imp.SaveAndReimport();
            _log.Add("ui sprite " + path);
            return 1;
        }
    }
}
