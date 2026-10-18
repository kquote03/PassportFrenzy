using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PassportFrenzy.EditorTools
{
    /// <summary>
    /// Procedural parallax backdrops: white silhouette PNGs per country (far landmarks, mid skyline) plus a shared
    /// cloud layer and sky-haze gradient, written to Placeholder/Backgrounds and tinted at runtime by ParallaxBackground.
    /// Every image is 32 x 10 world units at 24 PPU (768 x 240 px) and wraps horizontally, so tiles connect seamlessly.
    /// Menu: Passport Frenzy/Generate Country Backgrounds. MCP: PrototypeBuilder.GenerateBackgrounds(false).
    /// </summary>
    public static partial class PrototypeBuilder
    {
        const string BackgroundsDir = Root + "/Placeholder/Backgrounds";
        const float BgUnitsW = 32f, BgUnitsH = 10f;
        const int BgPpu = 24, BgSuper = 2;

        [MenuItem("Passport Frenzy/Generate Country Backgrounds", priority = 21)]
        static void GenerateBackgroundsMenu() => Debug.Log(GenerateBackgrounds(false));

        [MenuItem("Passport Frenzy/Generate Country Backgrounds (overwrite)", priority = 22)]
        static void RegenerateBackgroundsMenu() => Debug.Log(GenerateBackgrounds(true));

        internal static Sprite BgSprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{BackgroundsDir}/{name}.png");

        public static string GenerateBackgrounds(bool overwrite)
        {
            _log.Clear();
            EnsureFolder(BackgroundsDir);
            int n = 0;
            n += WriteBackground("bg_sky", 64, 240, false, r => r.Gradient(y01 => Mathf.Pow(1f - y01, 1.6f) * 0.45f), overwrite);
            n += WriteBackground("bg_clouds", 768, 240, true, r => Clouds(r, new System.Random(7)), overwrite);
            var ids = new List<string> { "generic", Home.id };
            foreach (var s in Destinations) ids.Add(s.id);
            foreach (var id in ids)
            {
                var recipe = Recipes.TryGetValue(id, out var rec) ? rec : Recipes["generic"];
                n += WriteBackground($"bg_{id}_far", 768, 240, true, r => recipe.far(r, new System.Random(id.GetHashCode())), overwrite);
                n += WriteBackground($"bg_{id}_mid", 768, 240, true, r => recipe.mid(r, new System.Random(id.GetHashCode() * 31)), overwrite);
            }
            AssignCountryBackgrounds(overwrite);
            AssetDatabase.SaveAssets();
            return $"[PrototypeBuilder] backgrounds written: {n}\n" + string.Join("\n", _log);
        }

        /// <summary>Point every Country asset at its far/mid silhouettes (only empty fields unless overwriting).</summary>
        static void AssignCountryBackgrounds(bool overwrite)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:CountryDefinition", new[] { DataDir + "/Countries" }))
            {
                var c = AssetDatabase.LoadAssetAtPath<CountryDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (c == null) continue;
                var far = BgSprite($"bg_{c.id}_far") ?? BgSprite("bg_generic_far");
                var mid = BgSprite($"bg_{c.id}_mid") ?? BgSprite("bg_generic_mid");
                bool changed = false;
                if ((overwrite || c.backgroundFar == null) && far != null && c.backgroundFar != far) { c.backgroundFar = far; changed = true; }
                if ((overwrite || c.backgroundMid == null) && mid != null && c.backgroundMid != mid) { c.backgroundMid = mid; changed = true; }
                if (changed) { EditorUtility.SetDirty(c); _log.Add("background assigned " + c.name); }
            }
        }

        static int WriteBackground(string name, int w, int h, bool worldSize, Action<Raster> draw, bool overwrite)
        {
            string path = $"{BackgroundsDir}/{name}.png";
            if (!overwrite && File.Exists(path)) return 0;
            // Draw in world units on a supersampled canvas, then box-filter down to the PNG size.
            float unitsW = worldSize ? BgUnitsW : w / (float)BgPpu, unitsH = worldSize ? BgUnitsH : h / (float)BgPpu;
            var r = new Raster(w * BgSuper, h * BgSuper, unitsW, unitsH);
            draw(r);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(r.Downsample(BgSuper));
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = BgPpu;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.filterMode = FilterMode.Bilinear;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            var s = new TextureImporterSettings();
            imp.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect;
            s.spriteAlignment = (int)SpriteAlignment.Center;
            imp.SetTextureSettings(s);
            imp.SaveAndReimport();
            _log.Add("background " + path);
            return 1;
        }

        // ================================================================== Recipes (x 0..32 wraps, y 0..10 up)

        struct BgRecipe
        {
            public Action<Raster, System.Random> far, mid;
        }

        static readonly Dictionary<string, BgRecipe> Recipes = new Dictionary<string, BgRecipe>
        {
            ["generic"] = new BgRecipe { far = (r, g) => { Hills(r, g, 2.2f); Skyline(r, g, 0f, 32f, 4f, 8f, Roof.Flat, 0.15f); }, mid = (r, g) => { Skyline(r, g, 0f, 32f, 2f, 5f, Roof.Flat); Trees(r, g, 5); } },
            ["ae"] = new BgRecipe { far = Dubai, mid = (r, g) => { Skyline(r, g, 0f, 32f, 2f, 5.5f, Roof.Flat); Palms(r, g, 6); } },
            ["in"] = new BgRecipe { far = Agra, mid = (r, g) => { Skyline(r, g, 0f, 32f, 1.8f, 4f, Roof.Dome); Palms(r, g, 4); } },
            ["cn"] = new BgRecipe { far = Beijing, mid = (r, g) => { Skyline(r, g, 0f, 32f, 2f, 4.5f, Roof.Pagoda); Trees(r, g, 4); } },
            ["us"] = new BgRecipe { far = NewYork, mid = (r, g) => Skyline(r, g, 0f, 32f, 3f, 6.5f, Roof.Flat) },
            ["gb"] = new BgRecipe { far = London, mid = (r, g) => { Skyline(r, g, 0f, 32f, 2f, 4.5f, Roof.Chimney); Trees(r, g, 3); } },
            ["de"] = new BgRecipe { far = Berlin, mid = (r, g) => { Skyline(r, g, 0f, 32f, 2f, 5f, Roof.Gable); Trees(r, g, 4); } },
            ["fr"] = new BgRecipe { far = Paris, mid = (r, g) => { Skyline(r, g, 0f, 32f, 2f, 4.5f, Roof.Mansard); Trees(r, g, 3); } },
            ["jp"] = new BgRecipe { far = Tokyo, mid = (r, g) => { Skyline(r, g, 0f, 32f, 1.6f, 4f, Roof.Pagoda); Trees(r, g, 6, round: true); } },
            ["th"] = new BgRecipe { far = Bangkok, mid = (r, g) => { Skyline(r, g, 0f, 32f, 1.6f, 4f, Roof.Spire); Palms(r, g, 7); } },
            ["ru"] = new BgRecipe { far = Moscow, mid = (r, g) => { Skyline(r, g, 0f, 32f, 2f, 4.5f, Roof.Onion); Trees(r, g, 4, conifer: true); } },
            ["es"] = new BgRecipe { far = Barcelona, mid = (r, g) => { Skyline(r, g, 0f, 32f, 1.8f, 4f, Roof.Flat); Trees(r, g, 4, round: true); } },
        };

        // ---- shared pieces

        static void Hills(Raster r, System.Random g, float h)
        {
            for (int i = 0; i < 5; i++)
            {
                float cx = i * 6.4f + Rnd(g, -1f, 1f);
                r.Ellipse(cx, -0.5f, Rnd(g, 4f, 6f), Rnd(g, h * 0.7f, h));
            }
        }

        static void Clouds(Raster r, System.Random g)
        {
            for (int i = 0; i < 7; i++)
            {
                float cx = i * 4.6f + Rnd(g, -1f, 1f), cy = Rnd(g, 5.5f, 9f), s = Rnd(g, 0.7f, 1.3f);
                r.Ellipse(cx, cy, 1.4f * s, 0.5f * s);
                r.Ellipse(cx - 0.7f * s, cy + 0.15f * s, 0.8f * s, 0.55f * s);
                r.Ellipse(cx + 0.6f * s, cy + 0.3f * s, 0.9f * s, 0.65f * s);
                r.Ellipse(cx + 0.1f * s, cy + 0.45f * s, 0.7f * s, 0.55f * s);
            }
        }

        enum Roof { Flat, Gable, Mansard, Dome, Pagoda, Spire, Onion, Chimney }

        /// <summary>Row of buildings from x0 to x1 (wrapping) with a roof style; the main body of the mid layer.</summary>
        static void Skyline(Raster r, System.Random g, float x0, float x1, float minH, float maxH, Roof roof, float gap = 0.25f)
        {
            float x = x0;
            while (x < x1)
            {
                float w = Rnd(g, 1.6f, 3.4f);
                if (x + w > x1) w = x1 - x;
                if (w < 0.8f) break;
                float h = Rnd(g, minH, maxH);
                float cx = x + w / 2f, bw = w - gap;
                r.Box(cx, 0f, bw, h);
                switch (roof)
                {
                    case Roof.Gable: r.Tri(cx - bw / 2f, h, cx + bw / 2f, h, cx, h + bw * 0.45f); break;
                    case Roof.Mansard: r.Trap(cx, h, bw, bw * 0.6f, 0.7f); break;
                    case Roof.Dome: r.Dome(cx, h, bw * 0.4f); r.Box(cx, h + bw * 0.4f, 0.12f, 0.5f); break;
                    case Roof.Pagoda: r.Trap(cx, h, bw * 1.35f, bw * 0.7f, 0.5f); if (g.NextDouble() < 0.5) r.Trap(cx, h + 0.9f, bw * 1.1f, bw * 0.5f, 0.45f); break;
                    case Roof.Spire: r.Spire(cx, h, bw * 0.5f, Rnd(g, 1f, 2.2f)); break;
                    case Roof.Onion: r.Onion(cx, h, bw * 0.35f); break;
                    case Roof.Chimney: r.Box(cx - bw * 0.3f, h, 0.25f, 0.6f); r.Box(cx + bw * 0.3f, h, 0.25f, 0.45f); break;
                }
                x += w;
            }
        }

        static void Trees(Raster r, System.Random g, int count, bool round = false, bool conifer = false)
        {
            for (int i = 0; i < count; i++)
            {
                float cx = Rnd(g, 0f, 32f), s = Rnd(g, 0.7f, 1.2f);
                r.Box(cx, 0f, 0.18f * s, 1.1f * s);
                if (conifer) { r.Spire(cx, 0.6f * s, 1.3f * s, 2.4f * s); r.Spire(cx, 1.4f * s, 1f * s, 1.8f * s); }
                else if (round) { r.Circle(cx, 1.6f * s, 0.8f * s); r.Circle(cx - 0.45f * s, 1.3f * s, 0.55f * s); r.Circle(cx + 0.45f * s, 1.35f * s, 0.55f * s); }
                else { r.Ellipse(cx, 1.7f * s, 0.75f * s, 0.9f * s); }
            }
        }

        static void Palms(Raster r, System.Random g, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float cx = Rnd(g, 0f, 32f), h = Rnd(g, 1.8f, 3f), lean = Rnd(g, -0.3f, 0.3f);
                r.Poly(new Vector2(cx - 0.12f, 0f), new Vector2(cx + 0.12f, 0f), new Vector2(cx + lean + 0.08f, h), new Vector2(cx + lean - 0.08f, h));
                float tx = cx + lean;
                for (int k = 0; k < 6; k++)
                {
                    float a = Mathf.PI * (0.1f + 0.8f * k / 5f);
                    float dx = Mathf.Cos(a) * 1.1f, dy = Mathf.Sin(a) * 0.5f - 0.1f;
                    r.Tri(tx, h, tx + dx, h + dy, tx + dx * 0.55f, h + dy + 0.35f);
                }
            }
        }

        // ---- landmarks (far layer). Baseline y 0, tallest things reach ~9.5 so they clear the near buildings.

        static void Dubai(Raster r, System.Random g)
        {
            Hills(r, g, 1.2f);
            Skyline(r, g, 0f, 32f, 3f, 6f, Roof.Flat);
            // Burj Khalifa: stepped spire
            float bx = 9f;
            for (int i = 0; i < 6; i++) r.Box(bx, 0f, 2.2f - i * 0.33f, 3.5f + i * 1f);
            r.Spire(bx, 8.4f, 0.35f, 1.5f);
            // Burj Al Arab: sail
            float sx = 22f;
            r.Poly(new Vector2(sx - 1.4f, 0f), new Vector2(sx + 0.6f, 0f), new Vector2(sx + 0.6f, 6.8f), new Vector2(sx - 0.2f, 7.4f));
            r.Box(sx + 0.9f, 5.6f, 1.2f, 0.25f);
            // Mosque domes
            r.Dome(28f, 2.6f, 1.3f); r.Box(28f, 0f, 3f, 2.6f); r.Box(26.2f, 0f, 0.3f, 5.2f); r.Box(29.8f, 0f, 0.3f, 5.2f);
        }

        static void Agra(Raster r, System.Random g)
        {
            Hills(r, g, 1.4f);
            // Taj Mahal: plinth, main dome, four minarets, two side chhatris
            float cx = 16f;
            r.Box(cx, 0f, 12f, 1.4f);
            r.Box(cx, 1.4f, 6f, 3.2f);
            r.Dome(cx, 4.4f, 2.1f); r.Box(cx, 4.4f, 2.6f, 0.9f); r.Box(cx, 6.5f, 0.12f, 0.9f);
            foreach (float mx in new[] { cx - 5.2f, cx + 5.2f }) { r.Box(mx, 1.4f, 0.5f, 6f); r.Dome(mx, 7.4f, 0.4f); }
            foreach (float mx in new[] { cx - 2.6f, cx + 2.6f }) { r.Box(mx, 4.6f, 0.9f, 0.6f); r.Dome(mx, 5.2f, 0.6f); }
            Skyline(r, g, 24f, 32f, 2f, 4f, Roof.Dome);
            Skyline(r, g, 0f, 8f, 2f, 4f, Roof.Dome);
        }

        static void Beijing(Raster r, System.Random g)
        {
            Hills(r, g, 3f);
            // Great Wall with crenellations along a hill
            for (float x = 0f; x < 32f; x += 0.5f)
            {
                float y = 2.2f + Mathf.Sin(x * 0.35f) * 0.8f;
                r.Box(x + 0.25f, 0f, 0.5f, y);
                if (((int)(x * 2)) % 2 == 0) r.Box(x + 0.25f, y, 0.3f, 0.35f);
            }
            foreach (float tx in new[] { 4f, 20f }) r.Box(tx, 0f, 1.6f, 4.2f);
            // Pagoda
            float px = 12f;
            for (int i = 0; i < 5; i++) { float y = 1.5f + i * 1.3f; r.Box(px, y, 2.6f - i * 0.4f, 1.3f); r.Trap(px, y + 1.1f, 3.6f - i * 0.5f, 1.6f - i * 0.25f, 0.5f); }
            r.Spire(px, 8f, 0.5f, 1.4f);
            // Modern towers
            r.Box(27f, 0f, 1.8f, 6.5f); r.Box(29.5f, 0f, 1.2f, 5f);
        }

        static void NewYork(Raster r, System.Random g)
        {
            Skyline(r, g, 0f, 32f, 4f, 8.2f, Roof.Flat, 0.2f);
            // Empire State-style stepped tower + antenna
            float ex = 8f;
            r.Box(ex, 0f, 2.4f, 6f); r.Box(ex, 6f, 1.6f, 1.8f); r.Box(ex, 7.8f, 0.9f, 0.9f); r.Box(ex, 8.7f, 0.12f, 1.2f);
            // Statue of Liberty torch on a pedestal
            float lx = 20f;
            r.Box(lx, 0f, 1.4f, 2.4f); r.Box(lx, 2.4f, 0.6f, 2.2f); r.Circle(lx, 4.9f, 0.35f);
            for (int i = 0; i < 7; i++) { float a = Mathf.PI * (0.15f + 0.7f * i / 6f); r.Tri(lx, 5f, lx + Mathf.Cos(a) * 0.9f, 5f + Mathf.Sin(a) * 0.9f, lx + Mathf.Cos(a) * 0.5f, 5f + Mathf.Sin(a) * 0.5f + 0.2f); }
            r.Box(lx + 0.75f, 4.6f, 0.14f, 1.9f); r.Circle(lx + 0.75f, 6.7f, 0.28f);
            // Suspension bridge towers + cables
            foreach (float bx in new[] { 26f, 31f }) { r.Box(bx, 0f, 0.6f, 5.5f); r.Box(bx, 3.2f, 1.2f, 0.3f); }
            for (float x = 26f; x <= 31f; x += 0.15f) { float u = (x - 26f) / 5f; r.Circle(x, 5.3f - Mathf.Sin(u * Mathf.PI) * 2.4f, 0.08f); }
        }

        static void London(Raster r, System.Random g)
        {
            Hills(r, g, 1f);
            Skyline(r, g, 0f, 32f, 2.5f, 4.5f, Roof.Chimney);
            // Big Ben
            float bx = 7f;
            r.Box(bx, 0f, 1.5f, 6.5f); r.Box(bx, 6.5f, 1.8f, 0.8f); r.Circle(bx, 6.1f, 0.5f); r.Spire(bx, 7.3f, 1.4f, 2.2f);
            r.Box(bx + 3f, 0f, 4f, 3.6f);
            // London Eye
            float ex = 22f, er = 3.6f, ey = 4.2f;
            for (int i = 0; i < 360; i += 2) { float a = i * Mathf.Deg2Rad; r.Circle(ex + Mathf.Cos(a) * er, ey + Mathf.Sin(a) * er, 0.12f); }
            for (int i = 0; i < 16; i++) { float a = i * Mathf.PI / 8f; r.Poly(new Vector2(ex - 0.05f, ey), new Vector2(ex + 0.05f, ey), new Vector2(ex + Mathf.Cos(a) * er + 0.05f, ey + Mathf.Sin(a) * er), new Vector2(ex + Mathf.Cos(a) * er - 0.05f, ey + Mathf.Sin(a) * er)); }
            r.Tri(ex - 1.6f, 0f, ex + 1.6f, 0f, ex, ey);
            // The Shard
            r.Spire(29f, 0f, 2f, 9f);
        }

        static void Berlin(Raster r, System.Random g)
        {
            Hills(r, g, 1.3f);
            Skyline(r, g, 0f, 32f, 2.2f, 4.2f, Roof.Gable);
            // Brandenburg Gate
            float gx = 9f;
            r.Box(gx, 0f, 7f, 1.2f);
            for (int i = -2; i <= 2; i++) r.Box(gx + i * 1.4f, 1.2f, 0.5f, 3.4f);
            r.Box(gx, 4.6f, 7.4f, 0.8f); r.Box(gx, 5.4f, 3f, 0.6f);
            r.Box(gx, 6f, 1.6f, 0.5f); r.Box(gx - 0.5f, 6.5f, 0.3f, 0.6f); r.Box(gx + 0.5f, 6.5f, 0.3f, 0.6f);
            // Fernsehturm
            float tx = 23f;
            r.Trap(tx, 0f, 1.2f, 0.5f, 6.2f); r.Circle(tx, 6.8f, 0.95f); r.Box(tx, 7.7f, 0.18f, 2f);
            // Cathedral dome
            r.Box(29f, 0f, 3f, 3f); r.Dome(29f, 3f, 1.4f); r.Box(29f, 4.4f, 0.4f, 0.8f);
        }

        static void Paris(Raster r, System.Random g)
        {
            Hills(r, g, 1f);
            Skyline(r, g, 0f, 32f, 2.4f, 4f, Roof.Mansard);
            // Eiffel Tower
            float ex = 11f;
            r.Poly(new Vector2(ex - 3.2f, 0f), new Vector2(ex - 2.2f, 0f), new Vector2(ex - 0.28f, 9f), new Vector2(ex + 0.28f, 9f), new Vector2(ex + 2.2f, 0f), new Vector2(ex + 3.2f, 0f), new Vector2(ex + 1.1f, 3.2f), new Vector2(ex - 1.1f, 3.2f));
            r.Box(ex, 3f, 4.6f, 0.4f); r.Box(ex, 5.6f, 2.3f, 0.3f); r.Box(ex, 9f, 0.12f, 0.9f);
            r.Dome(ex, 1.6f, 1.3f); // arch
            // Arc de Triomphe
            float ax = 24f;
            r.Box(ax, 0f, 4f, 4.2f);
            // Sacré-Cœur domes
            r.Box(30f, 0f, 2.6f, 2.4f); r.Dome(30f, 2.4f, 1.1f); r.Box(30f, 3.5f, 0.6f, 0.9f); r.Dome(30f, 4.4f, 0.3f);
        }

        static void Tokyo(Raster r, System.Random g)
        {
            // Mount Fuji with a snow notch left white (gap) is not possible in a silhouette, so just the cone
            r.Tri(4f, 0f, 24f, 0f, 14f, 8.6f);
            r.Box(14f, 8.3f, 1.6f, 0.3f);
            Skyline(r, g, 0f, 32f, 1.8f, 3.5f, Roof.Pagoda);
            // Pagoda
            float px = 27f;
            for (int i = 0; i < 5; i++) { float y = 1f + i * 1.25f; r.Box(px, y, 2.4f - i * 0.35f, 1.25f); r.Trap(px, y + 1.05f, 3.4f - i * 0.45f, 1.4f - i * 0.2f, 0.45f); }
            r.Box(px, 7.3f, 0.14f, 1.6f);
            // Torii
            float tx = 5f;
            r.Box(tx - 1f, 0f, 0.3f, 3.6f); r.Box(tx + 1f, 0f, 0.3f, 3.6f); r.Box(tx, 3.4f, 3.2f, 0.35f); r.Box(tx, 2.6f, 2.6f, 0.25f);
        }

        static void Bangkok(Raster r, System.Random g)
        {
            Hills(r, g, 1.2f);
            Skyline(r, g, 0f, 32f, 1.8f, 3.6f, Roof.Spire);
            // Wat Arun prangs
            foreach (var (px, h) in new[] { (10f, 8.5f), (7.6f, 5f), (12.4f, 5f) })
            {
                r.Trap(px, 0f, 2.4f * h / 8.5f + 0.6f, 1.2f * h / 8.5f + 0.3f, h * 0.6f);
                r.Spire(px, h * 0.6f, 1.2f * h / 8.5f + 0.3f, h * 0.4f);
                for (int i = 1; i <= 4; i++) r.Box(px, h * 0.6f * i / 5f, 2.6f * h / 8.5f + 0.5f - i * 0.25f, 0.12f);
            }
            // Stupa
            r.Box(23f, 0f, 3f, 1f); r.Dome(23f, 1f, 1.5f); r.Spire(23f, 2.4f, 0.8f, 3.2f);
            // Towers
            r.Box(29f, 0f, 1.6f, 6.4f); r.Box(31f, 0f, 1.2f, 5f);
        }

        static void Moscow(Raster r, System.Random g)
        {
            Hills(r, g, 1f);
            Skyline(r, g, 0f, 32f, 2f, 3.6f, Roof.Flat);
            // Kremlin wall + Spasskaya tower
            for (float x = 0f; x < 32f; x += 1f) r.Box(x + 0.5f, 0f, 0.6f, 2.6f);
            r.Box(16f, 0f, 2f, 3f);
            float sx = 9f;
            r.Box(sx, 0f, 2.2f, 5f); r.Box(sx, 5f, 1.6f, 1.2f); r.Spire(sx, 6.2f, 1.6f, 2.6f); r.Circle(sx, 8.9f, 0.25f);
            // St Basil's onion domes
            foreach (var (dx, y, rad) in new[] { (22f, 3f, 0.9f), (20.4f, 2.4f, 0.6f), (23.6f, 2.4f, 0.6f), (19.2f, 1.8f, 0.45f), (24.8f, 1.8f, 0.45f) })
            {
                r.Box(dx, 0f, rad * 1.6f, y);
                r.Onion(dx, y, rad);
            }
            r.Spire(22f, 5.4f, 0.6f, 1.8f);
        }

        static void Barcelona(Raster r, System.Random g)
        {
            Hills(r, g, 2.4f);
            Skyline(r, g, 0f, 32f, 1.8f, 3.6f, Roof.Flat);
            // Sagrada Família spires
            foreach (var (sx, h) in new[] { (12f, 9.2f), (10.4f, 7.6f), (13.6f, 7.6f), (9f, 6.2f), (15f, 6.2f) })
            {
                r.Trap(sx, 0f, 1.6f, 1f, h * 0.55f);
                r.Spire(sx, h * 0.55f, 1f, h * 0.45f);
                r.Circle(sx, h + 0.1f, 0.22f);
            }
            // Windmill
            float wx = 25f;
            r.Trap(wx, 0f, 2f, 1.4f, 3.6f); r.Dome(wx, 3.6f, 0.75f);
            for (int i = 0; i < 4; i++) { float a = i * Mathf.PI / 2f + 0.4f; r.Poly(new Vector2(wx, 3.9f), new Vector2(wx + Mathf.Cos(a) * 2.4f, 3.9f + Mathf.Sin(a) * 2.4f), new Vector2(wx + Mathf.Cos(a + 0.18f) * 2.2f, 3.9f + Mathf.Sin(a + 0.18f) * 2.2f)); }
            // Bull billboard
            r.Box(30f, 0f, 0.2f, 3f); r.Box(30f, 3f, 3f, 1.8f); r.Box(29.4f, 4.8f, 0.9f, 0.5f);
        }

        static float Rnd(System.Random g, float a, float b) => a + (float)g.NextDouble() * (b - a);

        // ================================================================== Raster

        /// <summary>Alpha canvas in world units with horizontal wrap-around; shapes are max-composited.</summary>
        sealed class Raster
        {
            readonly int _w, _h;
            readonly float _ppu;
            readonly float[] _a;

            public Raster(int w, int h, float unitsW, float unitsH)
            {
                _w = w; _h = h;
                _ppu = w / unitsW;
                _a = new float[w * h];
            }

            void Fill(float minX, float maxX, float minY, float maxY, Func<float, float, bool> inside)
            {
                int px0 = Mathf.FloorToInt(minX * _ppu), px1 = Mathf.CeilToInt(maxX * _ppu);
                int py0 = Mathf.Max(0, Mathf.FloorToInt(minY * _ppu)), py1 = Mathf.Min(_h - 1, Mathf.CeilToInt(maxY * _ppu));
                for (int py = py0; py <= py1; py++)
                {
                    float y = (py + 0.5f) / _ppu;
                    for (int px = px0; px <= px1; px++)
                    {
                        float x = (px + 0.5f) / _ppu;
                        if (!inside(x, y)) continue;
                        int wx = ((px % _w) + _w) % _w;
                        _a[py * _w + wx] = 1f;
                    }
                }
            }

            public void Rect(float x0, float y0, float w, float h) => Fill(x0, x0 + w, y0, y0 + h, (x, y) => true);
            public void Box(float cx, float y0, float w, float h) => Rect(cx - w / 2f, y0, w, h);

            public void Tri(float ax, float ay, float bx, float by, float cx, float cy) =>
                Poly(new Vector2(ax, ay), new Vector2(bx, by), new Vector2(cx, cy));

            public void Circle(float cx, float cy, float r) => Ellipse(cx, cy, r, r);

            public void Ellipse(float cx, float cy, float rx, float ry) =>
                Fill(cx - rx, cx + rx, cy - ry, cy + ry, (x, y) => Sq((x - cx) / rx) + Sq((y - cy) / ry) <= 1f);

            /// <summary>Upper half of a circle sitting on y0.</summary>
            public void Dome(float cx, float y0, float r) =>
                Fill(cx - r, cx + r, y0, y0 + r, (x, y) => Sq((x - cx) / r) + Sq((y - y0) / r) <= 1f);

            public void Spire(float cx, float y0, float w, float h) => Tri(cx - w / 2f, y0, cx + w / 2f, y0, cx, y0 + h);

            public void Trap(float cx, float y0, float wBottom, float wTop, float h) =>
                Poly(new Vector2(cx - wBottom / 2f, y0), new Vector2(cx + wBottom / 2f, y0), new Vector2(cx + wTop / 2f, y0 + h), new Vector2(cx - wTop / 2f, y0 + h));

            /// <summary>Onion dome: a bulb on a short neck with a point.</summary>
            public void Onion(float cx, float y0, float r)
            {
                Box(cx, y0, r * 1.1f, r * 0.4f);
                Ellipse(cx, y0 + r * 1.2f, r, r * 0.95f);
                Spire(cx, y0 + r * 1.9f, r * 1.1f, r * 1.4f);
            }

            public void Poly(params Vector2[] p)
            {
                float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
                foreach (var v in p) { minX = Mathf.Min(minX, v.x); maxX = Mathf.Max(maxX, v.x); minY = Mathf.Min(minY, v.y); maxY = Mathf.Max(maxY, v.y); }
                Fill(minX, maxX, minY, maxY, (x, y) =>
                {
                    bool inside = false;
                    for (int i = 0, j = p.Length - 1; i < p.Length; j = i++)
                    {
                        if ((p[i].y > y) != (p[j].y > y) && x < (p[j].x - p[i].x) * (y - p[i].y) / (p[j].y - p[i].y) + p[i].x)
                            inside = !inside;
                    }
                    return inside;
                });
            }

            /// <summary>Vertical gradient over the whole canvas: alpha(y01), y01 = 0 at the bottom.</summary>
            public void Gradient(Func<float, float> alpha)
            {
                for (int py = 0; py < _h; py++)
                {
                    float a = Mathf.Clamp01(alpha((py + 0.5f) / _h));
                    for (int px = 0; px < _w; px++) _a[py * _w + px] = a;
                }
            }

            public Color32[] Downsample(int factor)
            {
                int ow = _w / factor, oh = _h / factor;
                var px = new Color32[ow * oh];
                float inv = 1f / (factor * factor);
                for (int y = 0; y < oh; y++)
                for (int x = 0; x < ow; x++)
                {
                    float sum = 0f;
                    for (int sy = 0; sy < factor; sy++)
                    for (int sx = 0; sx < factor; sx++)
                        sum += _a[(y * factor + sy) * _w + x * factor + sx];
                    px[y * ow + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(sum * inv) * 255f));
                }
                return px;
            }

            static float Sq(float v) => v * v;
        }
    }
}
