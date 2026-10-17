# Passport Frenzy — Agent & Team Guide

> Read this first. It's the shared context for humans **and** AI agents (Claude Code loads it through `CLAUDE.md`).
> Current status, known bugs and next tasks are in **PROGRESS.md**. Keep both files up to date with every commit.

## 1. What this is
A 1-day game jam prototype (UAE jam, theme **"Festival of Passports"**).
2D endless **auto-runner rogue-lite**, left → right, loosely inspired by *Power Pamplona* (2007). You fly between countries and
collect **residencies (passports)**. Time spent in a country counts toward residency (**1 in-game year = 1 real minute**;
Jam Demo Mode: 1 year = 6 s). An application also needs **3 photos of festivals** in that country (proof of integration).
**"Menu hell":** timed phone-UI tasks (booking, residency form, duty-free) that you do *while still running*.
The pursuer is the **departing flight**: miss it and the run ends.

### Game loop
1. **Home airport (UAE) = main menu.** Play → the character runs through the airport door.
2. **Booking app** (phone): 3 random unowned destinations, each with a random visa (`5 years (5 min)`). Pick within about 5 s or a random one is booked for you. The **flight timer** starts.
3. **Residency application** (foreign airports only, about 2 s after booking, only if years in this country ≥ required): mash to fill the form, then attach 3 photos (mouse: drag; controller: flick the stick right). Timeout or too few photos = fail, so try again on a later visit. Success = passport (saved forever).
4. **Freezone shop** (phone, every airport): spend miles on one buff. You hold at most one item.
5. **Airport chase** through crowds to the gate. Terminal length and crowd density scale with the destination's tier.
6. Reach the gate → **fly** → earn miles (scaled by how long you stayed) → land → **country run** (obstacles, crowds, festivals, pickups) until the visa expires → the level runs into that country's airport → step 2.
- **Lose:** the flight timer hits 0 → back to the menu. Miles and passports are kept (JSON save).
- **Win:** own all 10 destination passports (lifetime collection across runs) → win screen with stats.

### Controls (auto-detected from the last device used)
| Action | Keyboard / mouse | Gamepad |
|---|---|---|
| Jump (only movement) | Space / W / ↑ | A (South) |
| Photo | F / right mouse | X (West) |
| Use item | E / Q | Y (North) |
| Menu navigate / confirm | Arrows/WASD, Space/Enter, mouse | Stick/D-pad, A |
| Mash (residency form) | Space or click the button | A / RT |
| Attach photo | Drag onto paper | Flick stick right |
| Debug panel | F1 | Select |

While any phone menu is open the **Gameplay** action map is disabled (no jump), and the world spawns only obstacle-free chunks.

## 2. Jam judging rubric (optimize for this)
| Category | Points | Breakdown |
|---|---|---|
| Gameplay | 30 | Fun 10, Controls 10, Challenge 5, Replay 5 |
| Theme relevance | 20 | |
| Creativity | 15 | Original Concept 10, Fresh Mechanics 5 |
| Art & Audio | 15 | Visual Design 10, Sound & Music 5 |
| Technical Execution | 10 | Stability 5, Polish & Completeness 5 |
| Presentation | 10 | Clear Live Demo 5, Team Pitch 5 |

Implications: controls must feel tight (coyote time, jump buffer); every country should look distinct (palette + unique props);
**Jam Demo Mode** keeps the live demo short and reliable; no console errors (stability).

## 3. Tech
- Unity **6000.6.5f1**, URP 2D (`Assets/Settings/Renderer2D.asset`), **Input System only** (new), uGUI + TextMeshPro.
- Enter Play Mode is configured with **no domain reload**. Static state must be reset in a
  `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` method (see `GameEvents`, `InputDeviceTracker`).
- Assemblies: `PassportFrenzy.Runtime` (Scripts/), `PassportFrenzy.Editor` (Editor/), `PassportFrenzy.Tests.EditMode`.
- One scene: `Assets/_Project/Scenes/Game.unity`, containing only `PF_GameRoot` + `PF_MainCamera` prefab instances.

## 4. Folder layout
```
Assets/Art/                         ARTIST ONLY. Code/agents never create or modify anything here.
Assets/_Project/
  Scripts/Core        GameRoot (service locator), GameEvents, TimeFormat, WeightedPicker
  Scripts/Data        GameTuning, CountryDefinition, CountryDatabase, SoundLibrary (ScriptableObject types)
  Scripts/Flow        GameFlow (state machine), RunState, BookingRules
  Scripts/Player      RunnerController, PlayerInteractions (photo/item), PlayerCamera
  Scripts/Input       InputRouter, InputDeviceTracker, InputPrompts, InputPromptLabel
  Scripts/World       Chunk, ChunkSpawner, PaletteTint, DensityGated, PickupSocket, FlowTrigger, Mover
  Scripts/Obstacles   ObstacleBase, SlowObstacle, BlockObstacle, CrowdZone
  Scripts/Festivals   FestivalZone
  Scripts/Buffs       BuffDefinition (+ FlightDelay/Shield/SpeedBoost), BuffSystem, BuffPickup
  Scripts/UI          UIRoot, HudView, FlightBoardView, PhoneView, BookingApp, ResidencyApp, ShopApp, menus...
  Scripts/Audio       AudioManager
  Scripts/Save        SaveSystem (JSON in Application.persistentDataPath)
  Scripts/DebugTools  DebugPanel
  Editor/             PrototypeBuilder (generates placeholder sprites, data assets, prefabs, scene)
  Tests/EditMode/     Pure-logic tests (RunState, booking, time format, save, weighted pick, tuning)
  Data/               GameTuning.asset, CountryDatabase.asset, SoundLibrary.asset, Countries/, Buffs/
  Input/              PassportFrenzyControls.inputactions (maps: Gameplay, UI; schemes: KeyboardMouse, Gamepad)
  Placeholder/Sprites Generated white shapes (Square, Circle, Triangle, Capsule), tinted per object
  Prefabs/            Core, Player, Chunks/{Generic,Airport,Home,Spain,Japan,India}, Obstacles, Festivals, Pickups, UI
  Scenes/Game.unity
```

## 5. Architecture
- **GameRoot** (`PF_GameRoot` prefab) holds references to data and systems: `GameRoot.I.tuning / countries / flow / spawner /
  input / audioManager / buffs / ui / player`. Its children: Systems, EventSystem, Player (nested `PF_Player`), UI (nested `PF_UI_Root`).
- **GameFlow** is the state machine: `Home → Departing → Booking → Residency? → Shop → Chase → Flying → CountryRun → ToAirport → Booking …`,
  plus `Missed` and `Won`. The airport part is a single readable coroutine (`AirportSequence`). It owns per-frame timers
  (visa, flight, residency time).
- **RunState** is plain C# data for one run (current/destination country, seconds per country, photos, held item, timers, stats).
- **SaveSystem / SaveData** hold lifetime data: miles, owned passports, counters. Saved on every change.
- **ChunkSpawner** streams `Chunk` prefabs ahead of the player and destroys them behind. Modes: `Home`, `Country`
  (weighted country pool), `AirportSafe` (obstacle-free filler while phones are open), `Chase` (airport pool for the terminal
  length, then the gate). One-off chunks (door, airport entrance, freezone, gate) go through `EnqueueOnce`.
- **Chunk children opt into behaviour** with components: `PaletteTint` (country colors), `DensityGated` (tier thins obstacles/crowds),
  `PickupSocket` (rare buff), `FestivalZone` (photo op), `FlowTrigger` (door / airport entrance / gate).
- **Obstacles** are trigger-based (`ObstacleBase`), with low triggers you jump over. `SlowObstacle` = stumble (speed dip, recovers
  over about 2 s). `BlockObstacle` = speed 0 until you jump. `CrowdZone` = continuous slow while overlapping (unavoidable; the shield doesn't help).
- **Buffs** are ScriptableObjects (`BuffDefinition` subclasses). Passive ones (Shield) apply on acquire; others go into the single item slot.
  The same assets serve the shop and pickups.
- **UI** uses uGUI prefabs. HUD and FlightBoard poll state each frame. Phone apps are coroutines that GameFlow `yield return`s on.
- **Input**: `InputRouter` exposes actions; `SetMenuMode(true)` disables the Gameplay map. `InputDeviceTracker` + `InputPrompts`
  produce device-correct prompt text (`"{Jump} Jump"` → `[Space] Jump` / `[A] Jump`).
- **Audio**: `AudioManager.Play(SoundIds.X)` and `PlayMusic(country)`. Every hook is a no-op until a clip is assigned in
  `Data/SoundLibrary.asset` (or `CountryDefinition.music`).
- **Events**: `GameEvents` (static) for UI/audio feedback only: toast, photo taken, hit, item changed, passport, miles.

## 6. Prefab & naming conventions
- Prefabs: `PF_<Category>_<Name>`, e.g. `PF_Chunk_Spain_Bulls`, `PF_Obstacle_Suitcase`, `PF_UI_Phone`.
- Data: `Country_<Name>`, `Buff_<Name>`, `GameTuning`, `CountryDatabase`, `SoundLibrary`.
- **Logic on the root, visuals on a child named `Visual`** (SpriteRenderer). A Visual may have its own child sprites for compound
  placeholder shapes. Extra visual-only children: `ShieldVisual` (player), `Label` (world TMP text).
- Chunk pivot = left edge at ground height. Ground top is y = 0. Default chunk length is 24 units. Player jump apex ≈ 2.4 units,
  jump distance ≈ 6 units at base speed. Keep block obstacles ≤ 1.3 high and ≤ 1.5 wide.
- Sorting orders: background -20…-10, ground 0, props 1–4, obstacles/festivals 5, pickups 6, player 10.
- Tune values in the Inspector (prefab fields or `GameTuning`), not in code.

## 7. Art-swap contract (for the artist / artist's agent)
1. Open any prefab in `Assets/_Project/Prefabs/` and select its `Visual` child.
2. Put your sprite (from `Assets/Art/...`) in the SpriteRenderer, and adjust the Visual transform scale/offset to fit. Leave the root,
   its colliders and its scripts alone.
3. If the Visual has a **PaletteTint** component, it recolors the sprite per country at runtime. For full-color art, delete
   PaletteTint (or set Role = None) and set the SpriteRenderer color to white.
4. Compound placeholder shapes (e.g. the plane or a crowd of capsules) are children of `Visual`. Delete or replace them freely.
5. UI: phone/menu prefabs are in `Prefabs/UI`. Swap Image sprites and TMP fonts freely. Don't rename objects that scripts
   reference through serialized fields (re-link them in the Inspector if you do).
6. **Never run `Passport Frenzy/Rebuild ALL (overwrites)`** after you've started swapping art: it regenerates every prefab from code.
   `Build Missing Assets` is safe (it only creates what doesn't exist).
7. Sounds: assign clips in `Data/SoundLibrary.asset` (ids: jump, hit, block, shield_break, photo, pickup, use_item, booking,
   phone_open, mash, stamp, rejected, buy, takeoff, flight_missed, win, music_<countryId>).

## 8. How to add a country (no code)
1. Duplicate a `Data/Countries/Country_*.asset`. Set `id` (unique, e.g. `br`), name, palette, tier (1–3),
   required residency years, visa range and festival names.
2. Optional: make unique chunks by duplicating a chunk prefab in `Prefabs/Chunks/` (e.g. `PF_Chunk_Spain_Bulls`) into a new
   folder, swapping obstacles, and adding them (with weights) to the country's `chunkPool`. Leave `chunkPool` empty to use the
   generic pool, and `airportChunkPool` empty to use the default airport.
3. Optional: give it its own `festivalZonePrefab` and `music`.
4. Add the asset to `Data/CountryDatabase.asset → destinations`. Done (the win condition = owning all destinations).

## 9. How to tune
- `Data/GameTuning.asset` holds every number: run speed, jump, stumble/crowd multipliers, booking/mash/photo/shop windows, flight timer
  slack, miles, tier tables (obstacle density, crowd density, terminal length), spawn distances and pickup chance.
- **Jam Demo Mode** (`jamDemoMode`, also toggled in the debug panel at runtime): 1 year = 6 s, country run ≤ 30 s, chase ≈ 30 s.
- Per-obstacle overrides are on the prefab (`SlowObstacle.stumbleMultiplier`, `CrowdZone.speedMultiplier`, `DensityGated.baseChance`).
- Per-country values are in its `Country_*` asset.

## 10. Debug panel (F1 / Select)
Skip 30 s, +1 year here, +500 miles, spawn festival ahead, force airport transition, +3 photos, toggle demo mode, win (grant all
passports), clear save. The header shows the state, device, years in country and the flight timer.

## 11. Agent testing notes (Unity MCP)
- **Auto Refresh is OFF** in this editor (user preference): after editing scripts or assets, run `AssetDatabase.Refresh()`
  via MCP `eval` (or MCP `recompile`), then check `recompile_status` / `console`. Batch your edits first.
- Play mode only runs at full speed while the Unity window is the **foreground app**. When unfocused it crawls at about 2 fps
  (`Application.runInBackground` is on, but the editor still throttles). Bring Unity to the front before timing-sensitive tests.
- `InputSystemSettings.inputsettings.asset` sets background behavior = *Ignore Focus* and editor input = *all devices go to
  the Game view*, so injected input works while unfocused.
- **Use `PassportFrenzy.EditorTools.PlaytestDriver`** (MCP eval) to press keys and buttons, e.g.
  `PlaytestDriver.TapKey(UnityEngine.InputSystem.Key.Space)`, `PlaytestDriver.TapPad(UnityEngine.InputSystem.LowLevel.GamepadButton.South)`,
  `PlaytestDriver.FlickStick(new UnityEngine.Vector2(1,0))`. It injects on virtual devices inside the game's own input update.
  MCP `simulate_key` (and events queued straight from eval) get processed in the *editor* input update, which gameplay never sees.
- `PlaytestDriver.Prepare()` also sets `Time.captureDeltaTime = 1/30`, so game time advances per frame (deterministic even at
  a few fps while unfocused). It's reset when Play mode exits.
- **`PassportFrenzy.EditorTools.PlaytestAutopilot.Run(gamepad, bookingIndex, mash, photos, shopIndex, ...)`** is a bot that plays
  through the flow (booking pick, mashing, photo attach via flick or arrow, shop buy/skip, auto-jump when blocked, auto-photo,
  item use in the chase). Follow it in `Logs/Editor.log` (or MCP console) via `[Flow]`, `[Residency]` and `[Autopilot]` lines.
  Typical scenario: Autopilot.Run(...) → eval the GameFlow debug hooks (`StartRun`, private `Land` via reflection, `DebugAddYear`,
  `DebugGrantPhotos`, `DebugForceAirport`) → wait for states with MCP `wait_for` (`findType: PassportFrenzy.GameFlow`, member `State`).
- `capture_game_view source=screen` needs an open Game view (Window > General > Game). Use `save_path` without `include_image`
  inside `wait_for` (inline images are huge).
- Drive the flow quickly with GameFlow debug hooks via eval (`GameRoot.I.flow.DebugForceAirport()`, `DebugGrantPhotos(3)`, ...).

## 12. Workflow rules (humans and agents)
- Use the **Unity MCP** for editor work (enter Play mode, read the console, run tests, create assets). After code changes: recompile →
  check the console for errors → run EditMode tests (`PassportFrenzy.Tests.EditMode`) → quick Play-mode smoke test.
- Never commit a non-compiling project. Update **PROGRESS.md** (done / stubbed / bugs / decisions / next tasks) with each commit.
- Keep scenes thin: put new things in prefabs/ScriptableObjects, not in `Game.unity`.
- Don't touch `Assets/Art/`.
- Regenerating assets: `Passport Frenzy/Build Missing Assets` (safe). `Rebuild ALL` only before art exists, or with the artist's agreement.
