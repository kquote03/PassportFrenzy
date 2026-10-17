using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace PassportFrenzy.EditorTools
{
    public static partial class PrototypeBuilder
    {
        const string InputAssetPath = Root + "/Input/PassportFrenzyControls.inputactions";
        const string ScenePath = Root + "/Scenes/Game.unity";

        class CoreData
        {
            public GameTuning tuning;
            public SoundLibrary sounds;
            public BuffDefinition delay, shield, boost;
            public CountryDatabase db;
        }

        // ================================================================== Tuning, sounds, buffs

        static CoreData BuildCoreData()
        {
            var d = new CoreData();
            d.tuning = Asset<GameTuning>($"{DataDir}/GameTuning.asset", t => { });
            d.delay = Asset<FlightDelayBuff>($"{DataDir}/Buffs/Buff_FlightDelay.asset", b =>
            {
                b.id = "flight_delay";
                b.displayName = "Flight Delay";
                b.description = "Bribe the gate agent: +60 s on the flight timer. Use during the airport chase.";
                b.cost = 300;
                b.color = Hex("#FF9F1C");
                b.isPassive = false;
                b.addSeconds = 60f;
            });
            d.shield = Asset<ShieldBuff>($"{DataDir}/Buffs/Buff_Shield.asset", b =>
            {
                b.id = "shield";
                b.displayName = "Shield";
                b.description = "Bubble that absorbs your next stumble or block. Activates instantly.";
                b.cost = 250;
                b.color = Hex("#4DD0E1");
                b.isPassive = true;
            });
            d.boost = Asset<SpeedBoostBuff>($"{DataDir}/Buffs/Buff_SpeedBoost.asset", b =>
            {
                b.id = "speed_boost";
                b.displayName = "Speed Boost";
                b.description = "Energy drink! 10 s of sprinting. Use any time.";
                b.cost = 200;
                b.color = Hex("#FFE14D");
                b.isPassive = false;
                b.seconds = 10f;
            });
            return d;
        }

        // ================================================================== Countries

        struct CountrySpec
        {
            public string id, name;
            public int tier, years, visaMin, visaMax;
            public string sky, ground, primary, accent;
            public string[] festivals;
        }

        static readonly CountrySpec Home = new CountrySpec
        {
            id = "ae", name = "UAE", tier = 1, years = 99, visaMin = 1, visaMax = 1,
            sky = "#F6D8A8", ground = "#D9C29A", primary = "#F4F1EA", accent = "#007A3D",
            festivals = new[] { "Dubai Shopping Festival" },
        };

        static readonly CountrySpec[] Destinations =
        {
            new CountrySpec { id = "in", name = "India", tier = 2, years = 3, visaMin = 2, visaMax = 5,
                sky = "#FFCB8E", ground = "#B8794A", primary = "#FF9933", accent = "#138808",
                festivals = new[] { "Holi", "Diwali", "Ganesh Chaturthi", "Pushkar Camel Fair" } },
            new CountrySpec { id = "cn", name = "China", tier = 2, years = 4, visaMin = 2, visaMax = 5,
                sky = "#F4C2B4", ground = "#6E5A4E", primary = "#DE2910", accent = "#FFDE00",
                festivals = new[] { "Lunar New Year", "Lantern Festival", "Dragon Boat Festival", "Mid-Autumn Festival" } },
            new CountrySpec { id = "us", name = "USA", tier = 3, years = 5, visaMin = 2, visaMax = 6,
                sky = "#8EC5FF", ground = "#55575E", primary = "#3C3B6E", accent = "#B22234",
                festivals = new[] { "Mardi Gras", "Fourth of July", "Coachella", "Thanksgiving Parade" } },
            new CountrySpec { id = "gb", name = "UK", tier = 2, years = 4, visaMin = 2, visaMax = 4,
                sky = "#B4BEC8", ground = "#4F6B4F", primary = "#012169", accent = "#C8102E",
                festivals = new[] { "Notting Hill Carnival", "Glastonbury", "Bonfire Night", "Edinburgh Fringe" } },
            new CountrySpec { id = "de", name = "Germany", tier = 2, years = 3, visaMin = 2, visaMax = 4,
                sky = "#BFCAD6", ground = "#5B5045", primary = "#3A3A3A", accent = "#FFCE00",
                festivals = new[] { "Oktoberfest", "Karneval", "Christmas Market", "Berlinale" } },
            new CountrySpec { id = "fr", name = "France", tier = 1, years = 3, visaMin = 2, visaMax = 5,
                sky = "#A9CCF0", ground = "#8C7F66", primary = "#0055A4", accent = "#EF4135",
                festivals = new[] { "Bastille Day", "Fête de la Musique", "Cannes Film Festival", "Nice Carnival" } },
            new CountrySpec { id = "jp", name = "Japan", tier = 3, years = 4, visaMin = 2, visaMax = 5,
                sky = "#FFE3EA", ground = "#6A5D5D", primary = "#F2F2F2", accent = "#BC002D",
                festivals = new[] { "Hanami", "Gion Matsuri", "Tanabata", "Sapporo Snow Festival" } },
            new CountrySpec { id = "th", name = "Thailand", tier = 1, years = 2, visaMin = 2, visaMax = 4,
                sky = "#9ED9E6", ground = "#A38450", primary = "#E8B931", accent = "#2D2A4A",
                festivals = new[] { "Songkran", "Loy Krathong", "Yi Peng Lantern Festival" } },
            new CountrySpec { id = "ru", name = "Russia", tier = 3, years = 5, visaMin = 2, visaMax = 5,
                sky = "#DCE6F0", ground = "#E8EDF2", primary = "#B23A3A", accent = "#1C57A5",
                festivals = new[] { "Maslenitsa", "White Nights", "Scarlet Sails" } },
            new CountrySpec { id = "es", name = "Spain", tier = 1, years = 3, visaMin = 2, visaMax = 5,
                sky = "#F7CF83", ground = "#C08A57", primary = "#F1BF00", accent = "#AA151B",
                festivals = new[] { "La Tomatina", "San Fermín", "Las Fallas", "Feria de Abril" } },
        };

        static WeightedChunk W(Chunk c, float w) => new WeightedChunk { prefab = c, weight = w };

        static WeightedChunk[] GenericPool() => new[]
        {
            // Festivals are not in the random pools: FestivalPlanner places 0/1/2 per visit.
            W(ChFlat, 0.6f), W(ChSuitcases, 1f), W(ChCones, 1f), W(ChCrowd, 0.8f), W(ChMixed, 1f),
        };

        static WeightedChunk[] PoolFor(string id)
        {
            switch (id)
            {
                case "es": return new[] { W(ChSpainBulls, 1.3f), W(ChSpainPlaza, 1f), W(ChCones, 0.6f), W(ChMixed, 0.6f), W(ChFlat, 0.4f) };
                case "jp": return new[] { W(ChJapanLanterns, 1.2f), W(ChJapanCommuters, 1.2f), W(ChSuitcases, 0.6f), W(ChMixed, 0.6f), W(ChFlat, 0.4f) };
                case "in": return new[] { W(ChIndiaCows, 1.2f), W(ChIndiaRickshaw, 1.2f), W(ChSuitcases, 0.6f), W(ChCrowd, 0.6f), W(ChFlat, 0.4f) };
                default: return System.Array.Empty<WeightedChunk>(); // falls back to the database's generic pool
            }
        }

        static Chunk SignatureFestivalFor(string id) => id switch
        {
            "es" => ChSpainTomatina,
            "jp" => ChJapanHanami,
            "in" => ChIndiaHoli,
            _ => null,
        };

        static FestivalZone FestivalFor(string id) => id switch
        {
            "es" => Load<FestivalZone>("Festivals/PF_Festival_Spain_Tomatina"),
            "jp" => Load<FestivalZone>("Festivals/PF_Festival_Japan_Hanami"),
            "in" => Load<FestivalZone>("Festivals/PF_Festival_India_Holi"),
            _ => null,
        };

        static T Load<T>(string relPrefab) where T : Component =>
            AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/{relPrefab}.prefab")?.GetComponent<T>();

        static CountryDefinition MakeCountry(CountrySpec s, bool home)
        {
            string file = "Country_" + s.name.Replace(" ", "");
            return Asset<CountryDefinition>($"{DataDir}/Countries/{file}.asset", c =>
            {
                c.id = s.id;
                c.displayName = s.name;
                c.isHome = home;
                c.difficultyTier = s.tier;
                c.requiredResidencyYears = s.years;
                c.visaYearsRange = new Vector2Int(s.visaMin, s.visaMax);
                c.skyColor = Hex(s.sky);
                c.groundColor = Hex(s.ground);
                c.primaryColor = Hex(s.primary);
                c.accentColor = Hex(s.accent);
                c.festivalNames = s.festivals;
                c.chunkPool = home ? System.Array.Empty<WeightedChunk>() : PoolFor(s.id);
                c.airportChunkPool = System.Array.Empty<WeightedChunk>();
                c.festivalZonePrefab = FestivalFor(s.id);
                c.signatureFestivalChunk = home ? null : SignatureFestivalFor(s.id);
            });
        }

        static void BuildCountries(CoreData d)
        {
            var home = MakeCountry(Home, true);
            var dest = Destinations.Select(s => MakeCountry(s, false)).ToList();
            d.db = Asset<CountryDatabase>($"{DataDir}/CountryDatabase.asset", db =>
            {
                db.home = home;
                db.destinations = dest;
                db.genericChunkPool = GenericPool();
                db.defaultAirportChasePool = new[] { W(ChAirCrowd, 1.2f), W(ChAirCarts, 1f), W(ChAirMixed, 1f), W(ChAirClear, 0.6f) };
                db.genericFestivalChunk = ChFestival;
                db.fillerChunk = ChFiller;
                db.arrivalChunk = ChArrival;
                db.airportEntranceChunk = ChEntrance;
                db.airportSafeChunk = ChLobby;
                db.freezoneChunk = ChFreezone;
                db.gateChunk = ChGate;
                db.homeLobbyChunk = ChHomeLobby;
                db.homeDoorChunk = ChHomeDoor;
                db.pickupPrefab = Load<BuffPickup>("Pickups/PF_Pickup_Buff");
                db.defaultFestivalZonePrefab = Load<FestivalZone>("Festivals/PF_Festival_Generic");
            });

            d.sounds = Asset<SoundLibrary>($"{DataDir}/SoundLibrary.asset", lib =>
            {
                var ids = new List<string>(SoundIds.All);
                ids.Add(SoundIds.MusicPrefix + home.id);
                ids.AddRange(dest.Select(c => SoundIds.MusicPrefix + c.id));
                var old = lib.entries.ToDictionary(e => e.id, e => e);
                lib.entries = ids.Select(id => old.TryGetValue(id, out var e) ? e : new SoundLibrary.Entry { id = id, volume = 1f }).ToList();
            });
        }

        // ================================================================== GameRoot + scene

        static InputActionReference ActionRef(string map, string action)
        {
            return AssetDatabase.LoadAllAssetsAtPath(InputAssetPath).OfType<InputActionReference>()
                .FirstOrDefault(r => r.action != null && r.action.actionMap.name == map && r.action.name == action);
        }

        static void BuildGameRoot(CoreData d)
        {
            var inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
            Prefab($"{PrefabsDir}/Core/PF_GameRoot.prefab", () =>
            {
                var go = NewRoot("PF_GameRoot");
                var root = go.AddComponent<GameRoot>();
                root.tuning = d.tuning;
                root.countries = d.db;
                root.sounds = d.sounds;
                root.shopBuffs = new List<BuffDefinition> { d.boost, d.shield, d.delay };
                root.pickupBuffs = new List<BuffDefinition> { d.boost, d.shield, d.delay };
                root.flow = go.AddComponent<GameFlow>();
                root.spawner = go.AddComponent<ChunkSpawner>();
                root.input = go.AddComponent<InputRouter>();
                root.buffs = go.AddComponent<BuffSystem>();
                root.audioManager = go.AddComponent<AudioManager>();
                S(root.input, "actions", inputAsset);

                var chunks = new GameObject("Chunks");
                chunks.transform.SetParent(go.transform, false);
                S(root.spawner, "chunkParent", chunks.transform);

                var audio = new GameObject("Audio");
                audio.transform.SetParent(go.transform, false);
                var sfx = audio.AddComponent<AudioSource>();
                sfx.playOnAwake = false;
                var music = audio.AddComponent<AudioSource>();
                music.playOnAwake = false;
                music.loop = true;
                music.volume = 0.6f;
                S(root.audioManager, "sfxSource", sfx);
                S(root.audioManager, "musicSource", music);

                var es = new GameObject("EventSystem");
                es.transform.SetParent(go.transform, false);
                es.AddComponent<EventSystem>();
                var module = es.AddComponent<InputSystemUIInputModule>();
                module.actionsAsset = inputAsset;
                module.point = ActionRef("UI", "Point");
                module.leftClick = ActionRef("UI", "Click");
                module.scrollWheel = ActionRef("UI", "ScrollWheel");
                module.move = ActionRef("UI", "Navigate");
                module.submit = ActionRef("UI", "Submit");
                module.cancel = ActionRef("UI", "Cancel");
                module.rightClick = null;
                module.middleClick = null;
                module.trackedDevicePosition = null;
                module.trackedDeviceOrientation = null;

                var player = Nested(PlayerPrefab, go.transform);
                player.transform.position = new Vector3(0f, 1f, 0f);
                root.player = player.GetComponent<RunnerController>();

                root.ui = Nested(UiRootPrefab, go.transform).GetComponent<UIRoot>();
                return go;
            });
        }

        static void BuildScene()
        {
            bool exists = System.IO.File.Exists(ScenePath);
            if (!exists || _overwrite)
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                PrefabUtility.InstantiatePrefab(CameraPrefab);
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/Core/PF_GameRoot.prefab"));
                EditorSceneManager.SaveScene(scene, ScenePath);
                _log.Add((exists ? "rebuilt " : "created ") + ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }
    }
}
