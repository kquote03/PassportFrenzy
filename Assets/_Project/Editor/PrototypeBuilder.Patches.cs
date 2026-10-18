using System;
using UnityEditor;
using UnityEngine;

namespace PassportFrenzy.EditorTools
{
    /// <summary>
    /// Idempotent edits to existing prefabs (no Rebuild ALL, so hand-applied art survives):
    /// the gate chunk gets a glass wall with a SpriteMask and a nested PF_Plane; PF_GameRoot gets the ParallaxBackground.
    /// Menu: Passport Frenzy/Patch Gate Plane + Parallax. MCP: PrototypeBuilder.ApplyPatches().
    /// </summary>
    public static partial class PrototypeBuilder
    {
        internal static GameObject PlanePrefab;

        [MenuItem("Passport Frenzy/Patch Gate Plane + Parallax (safe for art)", priority = 4)]
        static void PatchMenu() => Debug.Log(ApplyPatches());

        public static string ApplyPatches()
        {
            _log.Clear();
            _overwrite = false;
            BuildWorldPrefabs(); // loads the existing prefabs (and builds PF_Plane if missing)
            PatchGateChunk();
            PatchGameRoot();
            AssignCountryBackgrounds(false);
            var db = AssetDatabase.LoadAssetAtPath<CountryDatabase>($"{DataDir}/CountryDatabase.asset");
            if (db != null && db.planePrefab == null && PlanePrefab != null)
            {
                db.planePrefab = PlanePrefab.GetComponent<PlaneDeparture>();
                EditorUtility.SetDirty(db);
                _log.Add("planePrefab assigned");
            }
            AssetDatabase.SaveAssets();
            return "[PrototypeBuilder] patches applied\n" + string.Join("\n", _log);
        }

        /// <summary>Load a prefab, let <paramref name="mutate"/> change it, save only if it says it changed something.</summary>
        static bool PatchPrefab(string path, Func<GameObject, bool> mutate)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (!mutate(root)) return false;
                PrefabUtility.SaveAsPrefabAsset(root, path);
                _log.Add("patched " + path);
                return true;
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        // ================================================================== PF_Plane

        /// <summary>Nose-right airliner from primitives, about 8 x 2.4 units, centred on its root.</summary>
        static void BuildPlane()
        {
            EnsureFolder($"{PrefabsDir}/World");
            PlanePrefab = Prefab($"{PrefabsDir}/World/PF_Plane.prefab", () =>
            {
                var go = NewRoot("PF_Plane");
                var plane = go.AddComponent<PlaneDeparture>();
                var vis = new GameObject("Visual");
                vis.transform.SetParent(go.transform, false);
                S(plane, "visual", vis.transform);
                Color grey = Hex("#C8CDD3"), dark = Hex("#2C3E5C");
                Part(vis.transform, "Body", SqSprite, Color.white, new Vector2(6.4f, 1.5f), new Vector2(0f, 0f), -16);
                Part(vis.transform, "Nose", CircleSprite, Color.white, new Vector2(1.5f, 1.5f), new Vector2(3.2f, 0f), -16);
                Part(vis.transform, "TailCone", CircleSprite, Color.white, new Vector2(1.5f, 1.5f), new Vector2(-3.2f, 0f), -16);
                Part(vis.transform, "Fin", TriSprite, Color.white, new Vector2(1.4f, 2.1f), new Vector2(-3f, 1.3f), -16, PaletteRole.Accent);
                Part(vis.transform, "Tailplane", SqSprite, grey, new Vector2(1.7f, 0.25f), new Vector2(-3.3f, 0.6f), -16);
                Part(vis.transform, "Wing", SqSprite, grey, new Vector2(3.4f, 0.38f), new Vector2(0.3f, -0.5f), -15);
                Part(vis.transform, "Engine", SqSprite, dark, new Vector2(1.3f, 0.5f), new Vector2(0.6f, -0.9f), -15);
                Part(vis.transform, "Stripe", SqSprite, Color.white, new Vector2(6.4f, 0.22f), new Vector2(0f, 0.12f), -15, PaletteRole.Accent);
                for (int i = 0; i < 4; i++)
                    Part(vis.transform, "Window" + i, SqSprite, dark, new Vector2(0.3f, 0.3f), new Vector2(-1.6f + i * 1f, 0.35f), -15);
                Part(vis.transform, "Cockpit", SqSprite, dark, new Vector2(0.6f, 0.3f), new Vector2(2.75f, 0.32f), -15);
                return go;
            });
        }

        // ================================================================== Gate chunk: glass wall + mask + plane

        static bool PatchGateChunk()
        {
            if (PlanePrefab == null) return false;
            return PatchPrefab($"{PrefabsDir}/Chunks/Airport/PF_Chunk_Airport_Gate.prefab", root =>
            {
                if (root.transform.Find("WindowMask") != null) return false; // already patched
                var bd = root.transform.Find("Backdrop");
                // The old parked plane: the only backdrop parts at -16/-15 (don't match by name: "Stripe" is also the wall stripe).
                foreach (var sr in bd.GetComponentsInChildren<SpriteRenderer>(true))
                    if (sr.sortingOrder == -16 || sr.sortingOrder == -15) UnityEngine.Object.DestroyImmediate(sr.gameObject);

                // Floor-to-ceiling glass so the climb-out has room (the view tops out at y 9).
                var window = bd.Find("BigWindow");
                if (window != null)
                {
                    window.localPosition = new Vector3(17f, 4.9f, 0f);
                    window.localScale = new Vector3(12f, 8f, 1f);
                }

                var maskGo = new GameObject("WindowMask");
                maskGo.transform.SetParent(root.transform, false);
                maskGo.transform.localPosition = new Vector3(17f, 4.9f, 0f);
                var shape = new GameObject("Glass");
                shape.transform.SetParent(maskGo.transform, false);
                shape.transform.localScale = new Vector3(12f, 8f, 1f);
                var mask = shape.AddComponent<SpriteMask>();
                mask.sprite = SqSprite;
                mask.isCustomRangeActive = true;
                mask.backSortingOrder = -17;
                mask.frontSortingOrder = -14;

                var plane = Nested(PlanePrefab, maskGo.transform);
                plane.transform.localPosition = new Vector3(0.3f, -1.7f, 0f); // parked on the apron (world y ~3.2)
                return true;
            });
        }

        // ================================================================== GameRoot: ParallaxBackground

        static bool PatchGameRoot()
        {
            var sky = BgSprite("bg_sky");
            var clouds = BgSprite("bg_clouds");
            var far = BgSprite("bg_generic_far");
            var mid = BgSprite("bg_generic_mid");
            return PatchPrefab($"{PrefabsDir}/Core/PF_GameRoot.prefab", root =>
            {
                var gameRoot = root.GetComponent<GameRoot>();
                var bg = root.GetComponent<ParallaxBackground>();
                bool changed = false;
                if (bg == null) { bg = root.AddComponent<ParallaxBackground>(); changed = true; }
                var parent = root.transform.Find("Background");
                if (parent == null)
                {
                    parent = new GameObject("Background").transform;
                    parent.SetParent(root.transform, false);
                    changed = true;
                }
                if (gameRoot.background != bg) { gameRoot.background = bg; changed = true; }

                var so = new SerializedObject(bg);
                if (so.FindProperty("layerParent").objectReferenceValue != parent) { so.FindProperty("layerParent").objectReferenceValue = parent; changed = true; }
                if (so.FindProperty("skyHazeSprite").objectReferenceValue == null && sky != null) { so.FindProperty("skyHazeSprite").objectReferenceValue = sky; changed = true; }
                if (so.FindProperty("groundHazeSprite").objectReferenceValue == null) { so.FindProperty("groundHazeSprite").objectReferenceValue = SqSprite; changed = true; }
                var layers = so.FindProperty("layers");
                if (layers.arraySize == 0)
                {
                    layers.arraySize = 3;
                    Layer(layers.GetArrayElementAtIndex(0), "Clouds", clouds, 0.05f, -38, 0f, 0.3f, 0.8f, ParallaxBackground.TintSource.White);
                    Layer(layers.GetArrayElementAtIndex(1), "Far", far, 0.15f, -35, 0f, 0f, 1f, ParallaxBackground.TintSource.Far);
                    Layer(layers.GetArrayElementAtIndex(2), "Mid", mid, 0.4f, -30, 0f, 0f, 0.92f, ParallaxBackground.TintSource.Mid);
                    changed = true;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                return changed;
            });
        }

        static void Layer(SerializedProperty e, string name, Sprite sprite, float motion, int order, float baseY, float drift, float alpha,
            ParallaxBackground.TintSource tint)
        {
            e.FindPropertyRelative("name").stringValue = name;
            e.FindPropertyRelative("sprite").objectReferenceValue = sprite;
            e.FindPropertyRelative("motion").floatValue = motion;
            e.FindPropertyRelative("sortingOrder").intValue = order;
            e.FindPropertyRelative("baseY").floatValue = baseY;
            e.FindPropertyRelative("driftSpeed").floatValue = drift;
            e.FindPropertyRelative("alpha").floatValue = alpha;
            e.FindPropertyRelative("tint").enumValueIndex = (int)tint;
        }
    }
}
