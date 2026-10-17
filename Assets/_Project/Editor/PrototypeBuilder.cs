using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PassportFrenzy.EditorTools
{
    /// <summary>
    /// Generates the whole placeholder prototype from code: shape sprites, data assets, prefabs and Game.unity.
    ///  - "Build Missing Assets": only creates what doesn't exist yet. Safe after the artist has started.
    ///  - "Rebuild ALL (overwrites)": regenerates every prefab/asset. Destroys manual prefab edits and art swaps!
    /// Split into partial files: World (sprites, obstacles, chunks, player), UI, Data (countries, buffs, scene).
    /// </summary>
    public static partial class PrototypeBuilder
    {
        public const string Root = "Assets/_Project";
        const string SpritesDir = Root + "/Placeholder/Sprites";
        const string PrefabsDir = Root + "/Prefabs";
        const string DataDir = Root + "/Data";

        static bool _overwrite;
        static readonly List<string> _log = new List<string>();

        [MenuItem("Passport Frenzy/Build Missing Assets", priority = 0)]
        static void BuildMissingMenu() => Debug.Log(Build(false));

        [MenuItem("Passport Frenzy/Rebuild ALL (overwrites)", priority = 1)]
        static void RebuildAllMenu()
        {
            if (!EditorUtility.DisplayDialog("Rebuild ALL",
                    "This regenerates EVERY prototype prefab and data asset from code.\nManual prefab edits and art swaps will be lost.",
                    "Rebuild", "Cancel")) return;
            Debug.Log(Build(true));
        }

        [MenuItem("Passport Frenzy/Rebuild Data Assets (safe for art)", priority = 2)]
        static void RebuildDataMenu() => Debug.Log(RebuildData());

        /// <summary>
        /// Refills only data assets (buffs, countries, CountryDatabase, SoundLibrary; GameTuning values are kept) from
        /// code, without regenerating any prefab. Safe after the artist has swapped sprites.
        /// MCP: PassportFrenzy.EditorTools.PrototypeBuilder.RebuildData()
        /// </summary>
        public static string RebuildData()
        {
            _log.Clear();
            _overwrite = false;
            BuildWorldPrefabs();   // only loads the existing prefabs (nothing is overwritten)
            var data = BuildCoreData();
            _overwrite = true;
            try { BuildCountries(data); }
            finally { _overwrite = false; }
            AssetDatabase.SaveAssets();
            return "[PrototypeBuilder] data rebuilt\n" + string.Join("\n", _log);
        }

        /// <summary>Entry point for MCP/eval: PassportFrenzy.EditorTools.PrototypeBuilder.Build(false).</summary>
        public static string Build(bool overwrite)
        {
            _overwrite = overwrite;
            _log.Clear();
            try
            {
                AssetDatabase.StartAssetEditing();
                EnsureFolders();
            }
            finally { AssetDatabase.StopAssetEditing(); }

            BuildSprites();
            AssetDatabase.Refresh();

            var data = BuildCoreData();          // tuning, sounds, buffs
            BuildWorldPrefabs();                 // obstacles, festivals, pickups, chunks, player, camera
            BuildCountries(data);                // countries + database (needs chunks)
            BuildUiPrefabs();                    // UI views + root
            BuildGameRoot(data);                 // systems prefab (needs everything)
            BuildScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return $"[PrototypeBuilder] overwrite={overwrite}\n" + string.Join("\n", _log);
        }

        static void EnsureFolders()
        {
            string[] dirs =
            {
                "Assets/Art", SpritesDir, DataDir + "/Countries", DataDir + "/Buffs",
                PrefabsDir + "/Core", PrefabsDir + "/Player", PrefabsDir + "/Obstacles", PrefabsDir + "/Festivals",
                PrefabsDir + "/Pickups", PrefabsDir + "/UI", PrefabsDir + "/Chunks/Generic", PrefabsDir + "/Chunks/Airport",
                PrefabsDir + "/Chunks/Home", PrefabsDir + "/Chunks/Spain", PrefabsDir + "/Chunks/Japan", PrefabsDir + "/Chunks/India",
                Root + "/Scenes",
            };
            foreach (var d in dirs) EnsureFolder(d);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        // ------------------------------------------------------------------ Asset helpers

        /// <summary>Create (or, when overwriting, refill) a ScriptableObject asset.</summary>
        static T Asset<T>(string path, Action<T> fill) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null && !_overwrite) return existing;
            if (existing == null)
            {
                existing = ScriptableObject.CreateInstance<T>();
                fill(existing);
                AssetDatabase.CreateAsset(existing, path);
                _log.Add("created " + path);
            }
            else
            {
                fill(existing);
                EditorUtility.SetDirty(existing);
                _log.Add("refilled " + path);
            }
            return existing;
        }

        /// <summary>Build a prefab from a temporary GameObject unless it already exists (and we're not overwriting).</summary>
        static GameObject Prefab(string path, Func<GameObject> build)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null && !_overwrite) return existing;
            var go = build();
            var saved = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            _log.Add((existing != null ? "rebuilt " : "created ") + path);
            return saved;
        }

        static T Prefab<T>(string path, Func<GameObject> build) where T : Component => Prefab(path, build).GetComponent<T>();

        static GameObject Nested(GameObject prefab, Transform parent)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            return go;
        }

        // ------------------------------------------------------------------ SerializedObject helpers (private [SerializeField]s)

        static void S(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) throw new Exception($"{target.GetType().Name} has no serialized field '{field}'");
            switch (value)
            {
                case null: p.objectReferenceValue = null; break;
                case Object o: p.objectReferenceValue = o; break;
                case float f: p.floatValue = f; break;
                case int i when p.propertyType == SerializedPropertyType.Enum: p.enumValueIndex = i; break;
                case int i: p.intValue = i; break;
                case bool b: p.boolValue = b; break;
                case string s: p.stringValue = s; break;
                case Color c: p.colorValue = c; break;
                case Vector2 v: p.vector2Value = v; break;
                case Vector3 v3: p.vector3Value = v3; break;
                case Object[] arr:
                    p.arraySize = arr.Length;
                    for (int k = 0; k < arr.Length; k++) p.GetArrayElementAtIndex(k).objectReferenceValue = arr[k];
                    break;
                default: throw new Exception($"Unsupported value type {value.GetType()} for {field}");
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------ Misc

        static TMP_FontAsset Font => TMP_Settings.defaultFontAsset;

        static Material OutlineFontMaterial => AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Outline.mat");

        static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        static Color Rgb(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);
    }
}
