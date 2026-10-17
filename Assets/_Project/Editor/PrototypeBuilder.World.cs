using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace PassportFrenzy.EditorTools
{
    public static partial class PrototypeBuilder
    {
        // Built prefabs, reused when assembling chunks and data.
        static GameObject _suitcase, _barrier, _puddle, _cone, _cart, _wetFloor, _crowdStreet, _crowdAirport;
        static GameObject _bull, _fence, _lantern, _commuters, _cow, _rickshaw;
        static GameObject _festGeneric, _festTomatina, _festHanami, _festHoli, _pickup;
        internal static Chunk ChArrival, ChEntrance, ChLobby, ChFreezone, ChGate, ChHomeLobby, ChHomeDoor;
        internal static Chunk ChFlat, ChSuitcases, ChCones, ChCrowd, ChMixed, ChFestival;
        internal static Chunk ChAirCrowd, ChAirCarts, ChAirMixed, ChAirClear;
        internal static Chunk ChSpainBulls, ChSpainPlaza, ChSpainTomatina, ChJapanLanterns, ChJapanCommuters, ChJapanHanami;
        internal static Chunk ChIndiaCows, ChIndiaRickshaw, ChIndiaHoli;
        internal static GameObject PlayerPrefab, CameraPrefab;

        // ================================================================== Sprites

        internal static Sprite SqSprite => Spr("Square");
        internal static Sprite CircleSprite => Spr("Circle");
        internal static Sprite TriSprite => Spr("Triangle");
        internal static Sprite CapsuleSprite => Spr("Capsule");

        static Sprite Spr(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesDir}/{name}.png");

        static void BuildSprites()
        {
            const int n = 128;
            MakeShape("Square", n, n, (x, y) => 1f);
            MakeShape("Circle", n, n, (x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(n / 2f, n / 2f));
                return Mathf.Clamp01(n / 2f - d + 0.5f);
            });
            MakeShape("Triangle", n, n, (x, y) =>
            {
                float half = (1f - y / n) * n / 2f;
                return Mathf.Clamp01(half - Mathf.Abs(x - n / 2f) + 0.5f);
            });
            MakeShape("Capsule", n, n * 2, (x, y) =>
            {
                float r = n / 2f;
                float cy = Mathf.Clamp(y, r, n * 2 - r);
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(r, cy));
                return Mathf.Clamp01(r - d + 0.5f);
            });
        }

        static void MakeShape(string name, int w, int h, Func<float, float, float> alpha)
        {
            string path = $"{SpritesDir}/{name}.png";
            if (File.Exists(path) && !_overwrite) return;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[y * w + x] = new Color(1f, 1f, 1f, alpha(x + 0.5f, y + 0.5f));
            tex.SetPixels32(px);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 128; // 128 px = 1 unit (Capsule = 1 x 2)
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.filterMode = FilterMode.Bilinear;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.SaveAndReimport();
            _log.Add("sprite " + path);
        }

        // ================================================================== GameObject helpers
        // All temp objects are built at the world origin, so "center" values are relative to the prefab root.

        static GameObject NewRoot(string name)
        {
            var go = new GameObject(name);
            go.transform.position = Vector3.zero;
            return go;
        }

        static SpriteRenderer AddSprite(Transform parent, string name, Sprite sprite, Color color, Vector2 size, Vector2 center, int order,
            bool flipY = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            sr.flipY = flipY;
            Vector2 ss = sprite.bounds.size;
            Vector3 ps = parent.lossyScale;
            go.transform.localScale = new Vector3(size.x / ss.x / ps.x, size.y / ss.y / ps.y, 1f);
            go.transform.position = parent.root.position + new Vector3(center.x, center.y, 0f);
            return sr;
        }

        /// <summary>The prefab's art: child named "Visual". Optional PaletteTint for per-country colors.</summary>
        static SpriteRenderer Visual(GameObject root, Sprite sprite, Color color, Vector2 size, Vector2 center, int order,
            PaletteRole role = PaletteRole.None, Color? multiply = null)
        {
            var sr = AddSprite(root.transform, "Visual", sprite, color, size, center, order);
            if (role != PaletteRole.None) Tint(sr, role, multiply);
            return sr;
        }

        static SpriteRenderer Part(Component parent, string name, Sprite sprite, Color color, Vector2 size, Vector2 center, int order,
            PaletteRole role = PaletteRole.None, Color? multiply = null, bool flipY = false)
        {
            var sr = AddSprite(parent.transform, name, sprite, color, size, center, order, flipY);
            if (role != PaletteRole.None) Tint(sr, role, multiply);
            return sr;
        }

        static void Tint(SpriteRenderer sr, PaletteRole role, Color? multiply)
        {
            var t = sr.gameObject.AddComponent<PaletteTint>();
            t.role = role;
            t.multiply = multiply ?? Color.white;
        }

        static TextMeshPro Label(Transform parent, string text, Vector2 center, float fontSize, Color color, int order, float width = 10f)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.transform.position = parent.root.position + new Vector3(center.x, center.y, 0f);
            Vector3 ps = parent.lossyScale;
            go.transform.localScale = new Vector3(1f / ps.x, 1f / ps.y, 1f);
            var t = go.AddComponent<TextMeshPro>();
            t.font = Font;
            t.text = text;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.rectTransform.sizeDelta = new Vector2(width, 2f);
            t.sortingOrder = order;
            return t;
        }

        static BoxCollider2D Box(GameObject go, Vector2 size, Vector2 center, bool trigger)
        {
            var b = go.AddComponent<BoxCollider2D>();
            b.size = size;
            b.offset = center;
            b.isTrigger = trigger;
            return b;
        }

        static GameObject Place(GameObject prefab, Transform parent, float x, float y = 0f)
        {
            var go = Nested(prefab, parent);
            go.transform.position = parent.root.position + new Vector3(x, y, 0f);
            return go;
        }

        static void Gated(GameObject go, DensityCategory cat, float chance = 1f)
        {
            var g = go.AddComponent<DensityGated>();
            g.category = cat;
            g.baseChance = chance;
        }

        static void Socket(Transform parent, float x, float y, float mult = 1f)
        {
            var go = new GameObject("PickupSocket");
            go.transform.SetParent(parent, false);
            go.transform.position = parent.root.position + new Vector3(x, y, 0f);
            go.AddComponent<PickupSocket>().chanceMultiplier = mult;
        }

        static void Trigger(Transform parent, FlowTriggerId id, float x)
        {
            var go = new GameObject("Trigger_" + id);
            go.transform.SetParent(parent, false);
            go.transform.position = parent.root.position + new Vector3(x, 0f, 0f);
            Box(go, new Vector2(1f, 12f), new Vector2(0f, 6f), true);
            go.AddComponent<FlowTrigger>().id = id;
        }

        // ================================================================== World prefabs

        static void BuildWorldPrefabs()
        {
            BuildObstacles();
            BuildFestivalsAndPickups();
            BuildChunks();
            BuildPlayerAndCamera();
        }

        static GameObject ObstaclePrefab<T>(string file, Vector2 triggerSize, Action<GameObject> art, Action<T> config = null,
            bool gated = true) where T : Component
        {
            return Prefab($"{PrefabsDir}/Obstacles/{file}.prefab", () =>
            {
                var go = NewRoot(file);
                Box(go, triggerSize, new Vector2(0f, triggerSize.y / 2f), true); // collider first: RequireComponent(Collider2D)
                var comp = go.AddComponent<T>();
                if (gated) Gated(go, typeof(T) == typeof(CrowdZone) ? DensityCategory.Crowd : DensityCategory.Obstacle);
                art(go);
                config?.Invoke(comp);
                return go;
            });
        }

        static void BuildObstacles()
        {
            Color dark = Rgb(0.15f, 0.15f, 0.18f);

            // ---- Generic
            _suitcase = ObstaclePrefab<BlockObstacle>("PF_Obstacle_Suitcase", new Vector2(0.8f, 1.0f), go =>
            {
                var v = Visual(go, SqSprite, Hex("#8B5A2B"), new Vector2(1.0f, 1.0f), new Vector2(0f, 0.5f), 5);
                Part(v, "Handle", SqSprite, dark, new Vector2(0.4f, 0.12f), new Vector2(0f, 1.06f), 5);
                Part(v, "Strap", SqSprite, Hex("#D9B26F"), new Vector2(1.0f, 0.12f), new Vector2(0f, 0.55f), 6);
            });
            _barrier = ObstaclePrefab<BlockObstacle>("PF_Obstacle_Barrier", new Vector2(1.0f, 1.2f), go =>
            {
                var v = Visual(go, SqSprite, Hex("#F2C230"), new Vector2(1.3f, 0.35f), new Vector2(0f, 0.95f), 5);
                Part(v, "LegL", SqSprite, dark, new Vector2(0.12f, 1.2f), new Vector2(-0.5f, 0.6f), 4);
                Part(v, "LegR", SqSprite, dark, new Vector2(0.12f, 1.2f), new Vector2(0.5f, 0.6f), 4);
                Part(v, "Stripe", SqSprite, dark, new Vector2(0.25f, 0.35f), new Vector2(0f, 0.95f), 6);
            });
            _puddle = ObstaclePrefab<SlowObstacle>("PF_Obstacle_Puddle", new Vector2(1.4f, 0.35f), go =>
            {
                Visual(go, CircleSprite, Rgb(0.3f, 0.55f, 0.95f, 0.85f), new Vector2(1.8f, 0.3f), new Vector2(0f, 0.08f), 5);
            });
            _cone = ObstaclePrefab<SlowObstacle>("PF_Obstacle_Cone", new Vector2(0.6f, 0.9f), go =>
            {
                var v = Visual(go, TriSprite, Hex("#FF7A1A"), new Vector2(0.75f, 1.0f), new Vector2(0f, 0.5f), 5);
                Part(v, "Band", SqSprite, Color.white, new Vector2(0.32f, 0.12f), new Vector2(0f, 0.5f), 6);
                Part(v, "Base", SqSprite, Hex("#CC5500"), new Vector2(0.9f, 0.1f), new Vector2(0f, 0.05f), 6);
            });
            _crowdStreet = MakeCrowd("PF_Crowd_Street", 6f, 11, Vector2.zero, false);

            // ---- Airport
            _cart = ObstaclePrefab<BlockObstacle>("PF_Obstacle_LuggageCart", new Vector2(1.2f, 1.1f), go =>
            {
                var v = Visual(go, SqSprite, Hex("#9AA3AD"), new Vector2(1.4f, 0.2f), new Vector2(0f, 0.35f), 5);
                Part(v, "Bag1", SqSprite, Hex("#2E5EAA"), new Vector2(0.6f, 0.6f), new Vector2(-0.3f, 0.75f), 5);
                Part(v, "Bag2", SqSprite, Hex("#B03A2E"), new Vector2(0.5f, 0.45f), new Vector2(0.3f, 0.68f), 5);
                Part(v, "Handle", SqSprite, dark, new Vector2(0.1f, 1.1f), new Vector2(0.7f, 0.75f), 5);
                Part(v, "WheelL", CircleSprite, dark, new Vector2(0.25f, 0.25f), new Vector2(-0.5f, 0.12f), 6);
                Part(v, "WheelR", CircleSprite, dark, new Vector2(0.25f, 0.25f), new Vector2(0.5f, 0.12f), 6);
            });
            _wetFloor = ObstaclePrefab<SlowObstacle>("PF_Obstacle_WetFloor", new Vector2(0.6f, 1.0f), go =>
            {
                var v = Visual(go, TriSprite, Hex("#FFD400"), new Vector2(0.7f, 1.1f), new Vector2(0f, 0.55f), 5);
                Part(v, "Puddle", CircleSprite, Rgb(0.4f, 0.65f, 1f, 0.7f), new Vector2(1.4f, 0.2f), new Vector2(0.3f, 0.05f), 4);
            });
            _crowdAirport = MakeCrowd("PF_Crowd_Airport", 7f, 21, Vector2.zero, true);

            // ---- Spain: the Pamplona bull run nod
            _bull = ObstaclePrefab<SlowObstacle>("PF_Obstacle_Spain_Bull", new Vector2(1.4f, 0.9f), go =>
            {
                var v = Visual(go, CapsuleSprite, Hex("#3B2416"), new Vector2(1.7f, 1.0f), new Vector2(0f, 0.6f), 5);
                Part(v, "Head", CircleSprite, Hex("#2A180E"), new Vector2(0.6f, 0.6f), new Vector2(-0.85f, 0.8f), 6);
                Part(v, "HornT", TriSprite, Hex("#F3E9D2"), new Vector2(0.18f, 0.4f), new Vector2(-0.95f, 1.2f), 6);
                Part(v, "HornB", TriSprite, Hex("#F3E9D2"), new Vector2(0.18f, 0.4f), new Vector2(-0.7f, 1.2f), 6);
                Part(v, "LegF", SqSprite, Hex("#2A180E"), new Vector2(0.15f, 0.35f), new Vector2(-0.5f, 0.17f), 4);
                Part(v, "LegB", SqSprite, Hex("#2A180E"), new Vector2(0.15f, 0.35f), new Vector2(0.5f, 0.17f), 4);
                var m = go.AddComponent<Mover>();
                m.velocity = new Vector2(-4.5f, 0f);
                m.activationDistance = 20f;
                m.bobAmplitude = 0.12f;
                m.bobFrequency = 10f;
            }, o => S(o, "stumbleMultiplier", 0.15f));
            _fence = ObstaclePrefab<BlockObstacle>("PF_Obstacle_Spain_Fence", new Vector2(1.0f, 1.2f), go =>
            {
                var v = Visual(go, SqSprite, Hex("#A0522D"), new Vector2(1.3f, 0.2f), new Vector2(0f, 0.95f), 5);
                Part(v, "Rail2", SqSprite, Hex("#A0522D"), new Vector2(1.3f, 0.2f), new Vector2(0f, 0.5f), 5);
                Part(v, "PostL", SqSprite, Hex("#6B3A1E"), new Vector2(0.18f, 1.25f), new Vector2(-0.55f, 0.62f), 6);
                Part(v, "PostR", SqSprite, Hex("#6B3A1E"), new Vector2(0.18f, 1.25f), new Vector2(0.55f, 0.62f), 6);
            });

            // ---- Japan
            _lantern = ObstaclePrefab<BlockObstacle>("PF_Obstacle_Japan_Lantern", new Vector2(0.7f, 1.3f), go =>
            {
                var v = Visual(go, SqSprite, Hex("#8E8E8E"), new Vector2(0.35f, 0.7f), new Vector2(0f, 0.35f), 5);
                Part(v, "Box", SqSprite, Hex("#B5B5B5"), new Vector2(0.65f, 0.4f), new Vector2(0f, 0.9f), 5);
                Part(v, "Light", SqSprite, Hex("#FFE9A6"), new Vector2(0.3f, 0.2f), new Vector2(0f, 0.9f), 6);
                Part(v, "Roof", TriSprite, Hex("#6E6E6E"), new Vector2(0.95f, 0.35f), new Vector2(0f, 1.25f), 6);
            });
            _commuters = MakeCrowd("PF_Crowd_Japan_Commuters", 5f, 31, new Vector2(-1.6f, 0f), false, suits: true);

            // ---- India
            _cow = ObstaclePrefab<BlockObstacle>("PF_Obstacle_India_Cow", new Vector2(1.3f, 1.1f), go =>
            {
                var v = Visual(go, CapsuleSprite, Hex("#F4F1EA"), new Vector2(1.6f, 0.8f), new Vector2(0f, 0.75f), 5);
                Part(v, "Head", CircleSprite, Hex("#E8E2D5"), new Vector2(0.5f, 0.5f), new Vector2(0.85f, 0.95f), 6);
                Part(v, "Spot", CircleSprite, Hex("#5A4636"), new Vector2(0.4f, 0.3f), new Vector2(-0.2f, 0.85f), 6);
                Part(v, "LegF", SqSprite, Hex("#D9D2C3"), new Vector2(0.15f, 0.45f), new Vector2(0.5f, 0.22f), 4);
                Part(v, "LegB", SqSprite, Hex("#D9D2C3"), new Vector2(0.15f, 0.45f), new Vector2(-0.5f, 0.22f), 4);
                Part(v, "Garland", CircleSprite, Hex("#FF9F1C"), new Vector2(0.25f, 0.25f), new Vector2(0.7f, 0.65f), 7);
            });
            _rickshaw = ObstaclePrefab<SlowObstacle>("PF_Obstacle_India_Rickshaw", new Vector2(1.6f, 1.2f), go =>
            {
                var v = Visual(go, SqSprite, Hex("#2E8B3E"), new Vector2(1.8f, 0.7f), new Vector2(0f, 0.6f), 5);
                Part(v, "Roof", SqSprite, Hex("#FFD23F"), new Vector2(1.6f, 0.5f), new Vector2(0.1f, 1.2f), 5);
                Part(v, "WheelF", CircleSprite, Rgb(0.1f, 0.1f, 0.1f), new Vector2(0.4f, 0.4f), new Vector2(-0.7f, 0.2f), 6);
                Part(v, "WheelB", CircleSprite, Rgb(0.1f, 0.1f, 0.1f), new Vector2(0.4f, 0.4f), new Vector2(0.6f, 0.2f), 6);
                var m = go.AddComponent<Mover>();
                m.velocity = new Vector2(-2.5f, 0f);
                m.activationDistance = 18f;
                m.bobAmplitude = 0.05f;
                m.bobFrequency = 14f;
            });
        }

        static readonly Color[] ClothesColors =
        {
            Hex("#E63946"), Hex("#457B9D"), Hex("#2A9D8F"), Hex("#E9C46A"), Hex("#F4A261"), Hex("#8E7DBE"), Hex("#6D6875"), Hex("#90BE6D"),
        };
        static readonly Color[] SkinColors = { Hex("#F1C27D"), Hex("#E0AC69"), Hex("#C68642"), Hex("#8D5524"), Hex("#FFDBAC") };

        static GameObject MakeCrowd(string file, float width, int seed, Vector2 velocity, bool luggage, bool suits = false)
        {
            return ObstaclePrefab<CrowdZone>(file, new Vector2(width, 2.4f), go =>
            {
                var rng = new System.Random(seed);
                var v = Visual(go, CircleSprite, Rgb(0f, 0f, 0f, 0.25f), new Vector2(width, 0.35f), new Vector2(0f, 0.05f), 3);
                int count = Mathf.Max(2, Mathf.RoundToInt(width / 0.65f));
                float step = width / count;
                for (int i = 0; i < count; i++)
                {
                    float x = -width / 2f + step * (i + 0.5f) + (float)(rng.NextDouble() - 0.5) * 0.25f;
                    float h = 1.3f + (float)rng.NextDouble() * 0.45f;
                    int order = 3 + (i % 2) * 2;
                    Color body = suits ? (i % 3 == 0 ? Hex("#2B2D42") : Hex("#3D405B")) : ClothesColors[rng.Next(ClothesColors.Length)];
                    Part(v, "Person" + i, CapsuleSprite, body, new Vector2(0.5f, h), new Vector2(x, h / 2f), order);
                    Part(v, "Head" + i, CircleSprite, SkinColors[rng.Next(SkinColors.Length)], new Vector2(0.38f, 0.38f), new Vector2(x, h + 0.12f), order);
                    if (luggage && i % 3 == 1)
                        Part(v, "Bag" + i, SqSprite, ClothesColors[rng.Next(ClothesColors.Length)], new Vector2(0.4f, 0.5f), new Vector2(x + 0.3f, 0.25f), order + 1);
                }
                var m = go.AddComponent<Mover>();
                m.velocity = velocity;
                m.activationDistance = 24f;
                m.bobAmplitude = 0.06f;
                m.bobFrequency = 7f;
            });
        }

        static GameObject MakeFestival(string file, string nameOverride, float width, Color[] flags, Action<SpriteRenderer, GameObject> extra)
        {
            return Prefab($"{PrefabsDir}/Festivals/{file}.prefab", () =>
            {
                var go = NewRoot(file);
                Box(go, new Vector2(width, 5f), new Vector2(0f, 2.5f), true);
                var zone = go.AddComponent<FestivalZone>();
                // Visual = bunting line across the festival
                var v = Visual(go, SqSprite, Color.white, new Vector2(width, 0.08f), new Vector2(0f, 4.2f), 2, PaletteRole.Accent);
                Part(v, "PoleL", SqSprite, Hex("#5C4033"), new Vector2(0.15f, 4.4f), new Vector2(-width / 2f, 2.2f), 2);
                Part(v, "PoleR", SqSprite, Hex("#5C4033"), new Vector2(0.15f, 4.4f), new Vector2(width / 2f, 2.2f), 2);
                int n = Mathf.RoundToInt(width / 0.7f);
                for (int i = 0; i < n; i++)
                {
                    float x = -width / 2f + (i + 0.5f) * width / n;
                    Part(v, "Flag" + i, TriSprite, flags[i % flags.Length], new Vector2(0.45f, 0.55f), new Vector2(x, 3.9f), 2, flipY: true);
                }
                var label = Label(go.transform, "Festival", new Vector2(0f, 4.9f), 5f, Color.white, 8, width);
                label.fontStyle = FontStyles.Bold;
                label.outlineWidth = 0f;
                if (OutlineFontMaterial != null) label.fontSharedMaterial = OutlineFontMaterial;
                extra?.Invoke(v, go);
                S(zone, "festivalNameOverride", nameOverride ?? "");
                S(zone, "label", label);
                return go;
            });
        }

        static void BuildFestivalsAndPickups()
        {
            Color[] rainbow = { Hex("#E63946"), Hex("#F4A261"), Hex("#E9C46A"), Hex("#2A9D8F"), Hex("#457B9D"), Hex("#8E7DBE") };
            _festGeneric = MakeFestival("PF_Festival_Generic", null, 11f, rainbow, (v, go) =>
            {
                Part(v, "Stage", SqSprite, Color.white, new Vector2(4f, 0.6f), new Vector2(0f, 0.3f), 1, PaletteRole.Primary, Rgb(0.8f, 0.8f, 0.8f));
                Part(v, "Banner", SqSprite, Color.white, new Vector2(3f, 1.4f), new Vector2(0f, 2.4f), 1, PaletteRole.Accent);
                for (int i = 0; i < 4; i++)
                    Part(v, "Balloon" + i, CircleSprite, rainbow[i], new Vector2(0.5f, 0.6f), new Vector2(-4f + i * 2.6f, 3.2f + (i % 2) * 0.3f), 2);
            });
            Color[] spain = { Hex("#C60B1E"), Hex("#FFC400") };
            _festTomatina = MakeFestival("PF_Festival_Spain_Tomatina", "La Tomatina", 11f, spain, (v, go) =>
            {
                var rng = new System.Random(5);
                for (int i = 0; i < 14; i++)
                    Part(v, "Tomato" + i, CircleSprite, Hex("#E0201B"), Vector2.one * (0.25f + (float)rng.NextDouble() * 0.35f),
                        new Vector2(-5f + (float)rng.NextDouble() * 10f, 0.15f + (float)rng.NextDouble() * 3f), 2);
                Part(v, "Truck", SqSprite, Hex("#6C757D"), new Vector2(3f, 1.6f), new Vector2(3f, 0.8f), 1);
                Part(v, "Load", CircleSprite, Hex("#E0201B"), new Vector2(2.6f, 1f), new Vector2(3f, 1.7f), 1);
            });
            Color[] japan = { Hex("#FFFFFF"), Hex("#E6002D"), Hex("#FFB7C5") };
            _festHanami = MakeFestival("PF_Festival_Japan_Hanami", "Hanami", 11f, japan, (v, go) =>
            {
                for (int t = 0; t < 3; t++)
                {
                    float x = -3.5f + t * 3.5f;
                    Part(v, "Trunk" + t, SqSprite, Hex("#5C4033"), new Vector2(0.3f, 2.2f), new Vector2(x, 1.1f), 1);
                    Part(v, "Bloom" + t, CircleSprite, Hex("#FFB7C5"), new Vector2(2.4f, 1.8f), new Vector2(x, 2.6f), 1);
                    Part(v, "BloomB" + t, CircleSprite, Hex("#FFD1DC"), new Vector2(1.4f, 1.1f), new Vector2(x + 0.6f, 3.0f), 2);
                }
                Part(v, "Blanket", SqSprite, Hex("#4A90D9"), new Vector2(2.5f, 0.12f), new Vector2(1.7f, 0.06f), 2);
            });
            Color[] holi = { Hex("#FF2E93"), Hex("#FFD23F"), Hex("#3BCEAC"), Hex("#7B2FF7"), Hex("#FF8C42") };
            _festHoli = MakeFestival("PF_Festival_India_Holi", "Holi", 11f, holi, (v, go) =>
            {
                var rng = new System.Random(9);
                for (int i = 0; i < 18; i++)
                    Part(v, "Powder" + i, CircleSprite, holi[i % holi.Length] * new Color(1f, 1f, 1f, 0.7f), Vector2.one * (0.6f + (float)rng.NextDouble() * 1.1f),
                        new Vector2(-5f + (float)rng.NextDouble() * 10f, 0.8f + (float)rng.NextDouble() * 2.6f), 1);
            });

            _pickup = Prefab($"{PrefabsDir}/Pickups/PF_Pickup_Buff.prefab", () =>
            {
                var go = NewRoot("PF_Pickup_Buff");
                var circle = go.AddComponent<CircleCollider2D>();
                circle.isTrigger = true;
                circle.radius = 0.5f;
                var p = go.AddComponent<BuffPickup>();
                var v = Visual(go, CircleSprite, Color.white, new Vector2(0.8f, 0.8f), Vector2.zero, 6);
                Part(v, "Ring", CircleSprite, Rgb(1f, 1f, 1f, 0.35f), new Vector2(1.1f, 1.1f), Vector2.zero, 5);
                var label = Label(go.transform, "?", Vector2.zero, 4f, Rgb(0.1f, 0.1f, 0.1f), 7, 1f);
                label.fontStyle = FontStyles.Bold;
                S(p, "visual", v);
                S(p, "label", label);
                return go;
            });
        }

        // ================================================================== Chunks

        enum Backdrop { Street, Airport, Home }

        static Chunk MakeChunk(string folder, string name, ChunkKind kind, float length, Backdrop backdrop, int seed, Action<GameObject> content)
        {
            return Prefab<Chunk>($"{PrefabsDir}/Chunks/{folder}/{name}.prefab", () =>
            {
                var go = NewRoot(name);
                var chunk = go.AddComponent<Chunk>();
                chunk.kind = kind;
                S(chunk, "length", length);
                Box(go, new Vector2(length, 2f), new Vector2(length / 2f, -1f), false);

                bool indoor = backdrop != Backdrop.Street;
                var v = indoor
                    ? Visual(go, SqSprite, Hex("#D3D6DB"), new Vector2(length, 2f), new Vector2(length / 2f, -1f), 0)
                    : Visual(go, SqSprite, Color.white, new Vector2(length, 2f), new Vector2(length / 2f, -1f), 0, PaletteRole.Ground);
                Part(v, "Edge", SqSprite, Rgb(0f, 0f, 0f, 0.25f), new Vector2(length, 0.12f), new Vector2(length / 2f, -0.06f), 1);

                var bd = new GameObject("Backdrop");
                bd.transform.SetParent(go.transform, false);
                var rng = new System.Random(seed);
                if (backdrop == Backdrop.Street) StreetBackdrop(bd.transform, length, rng);
                else AirportBackdrop(bd.transform, length, backdrop == Backdrop.Home);

                content?.Invoke(go);
                return go;
            });
        }

        static void StreetBackdrop(Transform bd, float length, System.Random rng)
        {
            float x = 0f;
            int i = 0;
            while (x < length)
            {
                float w = 2.2f + (float)rng.NextDouble() * 2.6f;
                float h = 3f + (float)rng.NextDouble() * 5.5f;
                float shade = 0.55f + (float)rng.NextDouble() * 0.35f;
                var b = Part(bd, "Building" + i, SqSprite, Color.white, new Vector2(w - 0.2f, h), new Vector2(x + w / 2f, h / 2f), -15,
                    PaletteRole.Primary, new Color(shade, shade, shade));
                for (float wy = 1.2f; wy < h - 0.6f; wy += 1.3f)
                for (float wx = -w / 2f + 0.6f; wx < w / 2f - 0.4f; wx += 0.9f)
                    if (rng.NextDouble() < 0.7)
                        Part(b, "Window", SqSprite, Color.white, new Vector2(0.4f, 0.55f), new Vector2(x + w / 2f + wx, wy), -14,
                            PaletteRole.Sky, new Color(0.85f, 0.85f, 0.85f));
                if (rng.NextDouble() < 0.4)
                    Part(bd, "Awning" + i, SqSprite, Color.white, new Vector2(w - 0.3f, 0.35f), new Vector2(x + w / 2f, 1.6f), -13, PaletteRole.Accent);
                x += w;
                i++;
            }
        }

        static void AirportBackdrop(Transform bd, float length, bool home)
        {
            Part(bd, "Wall", SqSprite, Color.white, new Vector2(length, 10f), new Vector2(length / 2f, 5f), -20, PaletteRole.Primary, new Color(0.92f, 0.92f, 0.92f));
            Part(bd, "Stripe", SqSprite, Color.white, new Vector2(length, 0.35f), new Vector2(length / 2f, 7.2f), -18, PaletteRole.Accent);
            for (float wx = 4f; wx < length; wx += 8f)
            {
                Part(bd, "Window", SqSprite, Color.white, new Vector2(6f, 4f), new Vector2(wx, 3.6f), -19, PaletteRole.Sky);
                Part(bd, "Mullion", SqSprite, Hex("#7A7F87"), new Vector2(0.12f, 4f), new Vector2(wx, 3.6f), -18);
            }
            if (home)
            {
                for (float px = 2f; px < length; px += 12f)
                {
                    Part(bd, "PalmTrunk", SqSprite, Hex("#8B6B3E"), new Vector2(0.3f, 3.2f), new Vector2(px, 1.6f), -12);
                    Part(bd, "PalmLeaves", CircleSprite, Hex("#2E8B57"), new Vector2(2.2f, 1.1f), new Vector2(px, 3.3f), -11);
                }
            }
        }

        static void Sign(Transform parent, string text, float x, float y = 6.2f, float width = 6f)
        {
            Part(parent, "Sign", SqSprite, Color.white, new Vector2(width, 1.1f), new Vector2(x, y), -10, PaletteRole.Accent, new Color(0.7f, 0.7f, 0.7f));
            var l = Label(parent, text, new Vector2(x, y), 5f, Color.white, -9, width);
            l.fontStyle = FontStyles.Bold;
        }

        static void Bench(Transform parent, float x)
        {
            Part(parent, "Bench", SqSprite, Hex("#4A4E57"), new Vector2(2.4f, 0.25f), new Vector2(x, 0.55f), -8);
            Part(parent, "BenchLegs", SqSprite, Hex("#2E3138"), new Vector2(2f, 0.5f), new Vector2(x, 0.25f), -9);
        }

        static void Doorway(Transform parent, float x, string sign)
        {
            Part(parent, "DoorFrameL", SqSprite, Color.white, new Vector2(0.4f, 5f), new Vector2(x - 2f, 2.5f), -7, PaletteRole.Accent);
            Part(parent, "DoorFrameR", SqSprite, Color.white, new Vector2(0.4f, 5f), new Vector2(x + 2f, 2.5f), -7, PaletteRole.Accent);
            Part(parent, "DoorTop", SqSprite, Color.white, new Vector2(4.4f, 0.5f), new Vector2(x, 5.2f), -7, PaletteRole.Accent);
            Part(parent, "Glass", SqSprite, Rgb(0.75f, 0.9f, 1f, 0.45f), new Vector2(3.6f, 4.8f), new Vector2(x, 2.4f), -8);
            Sign(parent, sign, x, 6.4f, 5f);
        }

        static void BuildChunks()
        {
            const float L = 24f;
            const string G = "Generic", A = "Airport", H = "Home";

            // ---- Generic country chunks (palette from the country)
            ChFlat = MakeChunk(G, "PF_Chunk_Generic_Flat", ChunkKind.Country, L, Backdrop.Street, 1, go => Socket(go.transform, 12f, 1.2f, 1.5f));
            ChSuitcases = MakeChunk(G, "PF_Chunk_Generic_Suitcases", ChunkKind.Country, L, Backdrop.Street, 2, go =>
            {
                Place(_suitcase, go.transform, 8f);
                Place(_puddle, go.transform, 16f);
                Socket(go.transform, 20f, 2.6f);
            });
            ChCones = MakeChunk(G, "PF_Chunk_Generic_Cones", ChunkKind.Country, L, Backdrop.Street, 3, go =>
            {
                Place(_cone, go.transform, 6f);
                Place(_cone, go.transform, 13f);
                Place(_barrier, go.transform, 20f);
            });
            ChCrowd = MakeChunk(G, "PF_Chunk_Generic_Crowd", ChunkKind.Country, L, Backdrop.Street, 4, go =>
            {
                Place(_crowdStreet, go.transform, 10f);
                Place(_suitcase, go.transform, 18.5f);
                Socket(go.transform, 22f, 2.6f);
            });
            ChMixed = MakeChunk(G, "PF_Chunk_Generic_Mixed", ChunkKind.Country, L, Backdrop.Street, 5, go =>
            {
                Place(_puddle, go.transform, 5f);
                Place(_barrier, go.transform, 11f);
                Place(_cone, go.transform, 18f);
            });
            ChFestival = MakeChunk(G, "PF_Chunk_Generic_Festival", ChunkKind.Country, L, Backdrop.Street, 6, go =>
            {
                Place(_festGeneric, go.transform, 12f);
                Socket(go.transform, 21f, 1.2f);
            });

            // ---- Spain
            ChSpainBulls = MakeChunk("Spain", "PF_Chunk_Spain_Bulls", ChunkKind.Country, L, Backdrop.Street, 11, go =>
            {
                Place(_fence, go.transform, 7f);
                Place(_bull, go.transform, 21f);
                Socket(go.transform, 14f, 2.6f);
            });
            ChSpainPlaza = MakeChunk("Spain", "PF_Chunk_Spain_Plaza", ChunkKind.Country, L, Backdrop.Street, 12, go =>
            {
                Place(_puddle, go.transform, 5f);
                Place(_bull, go.transform, 15f);
                Place(_bull, go.transform, 23f);
            });
            ChSpainTomatina = MakeChunk("Spain", "PF_Chunk_Spain_Tomatina", ChunkKind.Country, L, Backdrop.Street, 13, go => Place(_festTomatina, go.transform, 12f));

            // ---- Japan
            ChJapanLanterns = MakeChunk("Japan", "PF_Chunk_Japan_Lanterns", ChunkKind.Country, L, Backdrop.Street, 21, go =>
            {
                var bd = go.transform.Find("Backdrop");
                Part(bd, "ToriiL", SqSprite, Hex("#D7261E"), new Vector2(0.5f, 5.5f), new Vector2(10f, 2.75f), -12);
                Part(bd, "ToriiR", SqSprite, Hex("#D7261E"), new Vector2(0.5f, 5.5f), new Vector2(14f, 2.75f), -12);
                Part(bd, "ToriiTop", SqSprite, Hex("#1F1F1F"), new Vector2(6f, 0.5f), new Vector2(12f, 5.6f), -11);
                Part(bd, "ToriiBeam", SqSprite, Hex("#D7261E"), new Vector2(5f, 0.35f), new Vector2(12f, 4.7f), -11);
                Place(_lantern, go.transform, 7f);
                Place(_lantern, go.transform, 15.5f);
                Place(_cone, go.transform, 21f);
            });
            ChJapanCommuters = MakeChunk("Japan", "PF_Chunk_Japan_Commuters", ChunkKind.Country, L, Backdrop.Street, 22, go =>
            {
                Place(_lantern, go.transform, 5f);
                Place(_commuters, go.transform, 16f);
                Socket(go.transform, 10f, 2.6f);
            });
            ChJapanHanami = MakeChunk("Japan", "PF_Chunk_Japan_Hanami", ChunkKind.Country, L, Backdrop.Street, 23, go => Place(_festHanami, go.transform, 12f));

            // ---- India
            ChIndiaCows = MakeChunk("India", "PF_Chunk_India_Cows", ChunkKind.Country, L, Backdrop.Street, 31, go =>
            {
                Place(_cow, go.transform, 8f);
                Place(_cow, go.transform, 17f);
                Socket(go.transform, 12.5f, 2.6f);
            });
            ChIndiaRickshaw = MakeChunk("India", "PF_Chunk_India_Rickshaw", ChunkKind.Country, L, Backdrop.Street, 32, go =>
            {
                Place(_puddle, go.transform, 7f);
                Place(_rickshaw, go.transform, 21f);
            });
            ChIndiaHoli = MakeChunk("India", "PF_Chunk_India_Holi", ChunkKind.Country, L, Backdrop.Street, 33, go => Place(_festHoli, go.transform, 12f));

            // ---- Airport (shared by every country; palette = the airport's country)
            ChArrival = MakeChunk(A, "PF_Chunk_Airport_Arrival", ChunkKind.Transition, L, Backdrop.Airport, 41, go =>
            {
                Sign(go.transform.Find("Backdrop"), "ARRIVALS", 6f);
                Doorway(go.transform.Find("Backdrop"), 18f, "EXIT  >>");
            });
            ChEntrance = MakeChunk(A, "PF_Chunk_Airport_Entrance", ChunkKind.Transition, L, Backdrop.Street, 42, go =>
            {
                var bd = go.transform.Find("Backdrop");
                Part(bd, "Terminal", SqSprite, Color.white, new Vector2(14f, 8f), new Vector2(17f, 4f), -12, PaletteRole.Primary, new Color(0.95f, 0.95f, 0.95f));
                Doorway(bd, 12f, "DEPARTURES");
                Trigger(go.transform, FlowTriggerId.AirportEntrance, 12f);
            });
            ChLobby = MakeChunk(A, "PF_Chunk_Airport_Lobby", ChunkKind.AirportSafe, L, Backdrop.Airport, 43, go =>
            {
                var bd = go.transform.Find("Backdrop");
                Bench(bd, 5f);
                Bench(bd, 17f);
                Part(bd, "Screen", SqSprite, Hex("#14213D"), new Vector2(3f, 1.6f), new Vector2(11f, 5.5f), -9);
                Label(bd, "DEPARTURES", new Vector2(11f, 5.8f), 3f, Hex("#FCA311"), -8, 3f);
            });
            ChFreezone = MakeChunk(A, "PF_Chunk_Airport_Freezone", ChunkKind.AirportSafe, L, Backdrop.Airport, 44, go =>
            {
                var bd = go.transform.Find("Backdrop");
                Sign(bd, "FREEZONE  DUTY FREE", 12f, 6.4f, 9f);
                for (int i = 0; i < 4; i++)
                {
                    float x = 4f + i * 5.5f;
                    Part(bd, "Shelf" + i, SqSprite, Hex("#6B4F3A"), new Vector2(3.5f, 2.6f), new Vector2(x, 1.3f), -9);
                    Part(bd, "Goods" + i, SqSprite, ClothesColors[i * 2], new Vector2(3f, 0.6f), new Vector2(x, 2f), -8);
                    Part(bd, "Goods2" + i, SqSprite, ClothesColors[i * 2 + 1], new Vector2(3f, 0.6f), new Vector2(x, 1f), -8);
                }
            });
            ChAirCrowd = MakeChunk(A, "PF_Chunk_Airport_Chase_Crowd", ChunkKind.AirportChase, L, Backdrop.Airport, 45, go =>
            {
                Place(_crowdAirport, go.transform, 12f);
            });
            ChAirCarts = MakeChunk(A, "PF_Chunk_Airport_Chase_Carts", ChunkKind.AirportChase, L, Backdrop.Airport, 46, go =>
            {
                Place(_cart, go.transform, 5f);
                Place(_crowdAirport, go.transform, 14f);
                Place(_wetFloor, go.transform, 21f);
            });
            ChAirMixed = MakeChunk(A, "PF_Chunk_Airport_Chase_Mixed", ChunkKind.AirportChase, L, Backdrop.Airport, 47, go =>
            {
                Place(_wetFloor, go.transform, 4f);
                Place(_crowdAirport, go.transform, 12f);
                Place(_cart, go.transform, 20f);
                Socket(go.transform, 8f, 2.6f, 0.5f);
            });
            ChAirClear = MakeChunk(A, "PF_Chunk_Airport_Chase_Clear", ChunkKind.AirportChase, L, Backdrop.Airport, 48, go =>
            {
                Bench(go.transform.Find("Backdrop"), 16f);
                Place(_crowdAirport, go.transform, 7f);
                Place(_suitcase, go.transform, 19f);
            });
            ChGate = MakeChunk(A, "PF_Chunk_Airport_Gate", ChunkKind.Gate, L, Backdrop.Airport, 49, go =>
            {
                var bd = go.transform.Find("Backdrop");
                Sign(bd, "GATE  >>  BOARDING", 9f, 6.4f, 8f);
                Part(bd, "Desk", SqSprite, Hex("#3C4048"), new Vector2(2.4f, 1.3f), new Vector2(6f, 0.65f), -6);
                // Plane parked outside the big window
                Part(bd, "BigWindow", SqSprite, Color.white, new Vector2(12f, 5f), new Vector2(17f, 3.4f), -17, PaletteRole.Sky);
                Part(bd, "PlaneBodyH", SqSprite, Color.white, new Vector2(8f, 1.6f), new Vector2(17f, 3.2f), -16);
                Part(bd, "Nose", CircleSprite, Color.white, new Vector2(1.6f, 1.6f), new Vector2(13f, 3.2f), -16);
                Part(bd, "Tail", TriSprite, Color.white, new Vector2(1.4f, 2.2f), new Vector2(20.4f, 4.6f), -16, PaletteRole.Accent);
                Part(bd, "Wing", SqSprite, Hex("#C8CDD3"), new Vector2(3.5f, 0.4f), new Vector2(17.5f, 2.8f), -15);
                Part(bd, "Stripe", SqSprite, Color.white, new Vector2(8f, 0.25f), new Vector2(17f, 3.4f), -15, PaletteRole.Accent);
                Trigger(go.transform, FlowTriggerId.Gate, 10f);
            });

            // ---- Home airport (UAE)
            ChHomeLobby = MakeChunk(H, "PF_Chunk_Home_Lobby", ChunkKind.Home, L, Backdrop.Home, 51, go =>
            {
                var bd = go.transform.Find("Backdrop");
                Sign(bd, "DXB  HOME AIRPORT", 12f, 6.4f, 8f);
                Bench(bd, 8f);
                Bench(bd, 18f);
            });
            ChHomeDoor = MakeChunk(H, "PF_Chunk_Home_Door", ChunkKind.Transition, L, Backdrop.Home, 52, go =>
            {
                Doorway(go.transform.Find("Backdrop"), 10f, "DEPARTURES");
                Trigger(go.transform, FlowTriggerId.HomeDoor, 10f);
            });
        }

        // ================================================================== Player + camera

        static void BuildPlayerAndCamera()
        {
            string pmPath = $"{PrefabsDir}/Player/PM_Runner_NoFriction.physicsMaterial2D";
            var pm = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(pmPath);
            if (pm == null)
            {
                pm = new PhysicsMaterial2D("PM_Runner_NoFriction") { friction = 0f, bounciness = 0f };
                AssetDatabase.CreateAsset(pm, pmPath);
            }

            PlayerPrefab = Prefab($"{PrefabsDir}/Player/PF_Player.prefab", () =>
            {
                var go = NewRoot("PF_Player");
                var rb = go.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.freezeRotation = true;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
                rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
                rb.gravityScale = 3.5f;
                rb.sharedMaterial = pm;
                var col = go.AddComponent<CapsuleCollider2D>();
                col.size = new Vector2(0.8f, 1.6f);
                col.offset = new Vector2(0f, 0.8f);
                col.sharedMaterial = pm;
                var runner = go.AddComponent<RunnerController>();
                go.AddComponent<PlayerInteractions>();

                var v = Visual(go, CapsuleSprite, Hex("#2F80ED"), new Vector2(0.8f, 1.6f), new Vector2(0f, 0.8f), 10);
                Part(v, "Head", CircleSprite, Hex("#F1C27D"), new Vector2(0.55f, 0.55f), new Vector2(0.05f, 1.45f), 11);
                Part(v, "Eye", SqSprite, Hex("#1B1B1B"), new Vector2(0.09f, 0.12f), new Vector2(0.2f, 1.5f), 12);
                Part(v, "Backpack", SqSprite, Hex("#F2994A"), new Vector2(0.3f, 0.6f), new Vector2(-0.45f, 0.95f), 9);
                Part(v, "Passport", SqSprite, Hex("#7B1E3A"), new Vector2(0.22f, 0.3f), new Vector2(0.38f, 0.8f), 12);
                var shield = AddSprite(go.transform, "ShieldVisual", CircleSprite, Rgb(0.3f, 0.9f, 1f, 0.35f), new Vector2(2.1f, 2.1f), new Vector2(0f, 0.85f), 13);
                S(runner, "visual", v.transform);
                S(runner, "shieldVisual", shield.gameObject);
                return go;
            });

            CameraPrefab = Prefab($"{PrefabsDir}/Core/PF_MainCamera.prefab", () =>
            {
                var go = NewRoot("PF_MainCamera");
                go.tag = "MainCamera";
                go.transform.position = new Vector3(6f, 3f, -10f);
                var cam = go.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = 6f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Hex("#9CC9F0");
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane = 50f;
                go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                go.AddComponent<AudioListener>();
                go.AddComponent<PlayerCamera>();
                var light = new GameObject("GlobalLight2D");
                light.transform.SetParent(go.transform, false);
                var l2d = light.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
                l2d.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Global;
                l2d.intensity = 1f;
                return go;
            });
        }
    }
}
