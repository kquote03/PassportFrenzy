# Passport Frenzy — Agent & Team Guide

> Read this first. It's the shared context for humans **and** AI agents (Claude Code loads it through `CLAUDE.md`).
> Current status, known bugs and next tasks are in **PROGRESS.md**. Keep both files up to date with every commit.
> Picking up mid-stream? Read **HANDOFF.md** first (what to verify, next tasks in order).

## 1. What this is
A 1-day game jam prototype (UAE jam, theme **"Festival of Passports"**).
2D side-scrolling **runner rogue-lite** with free movement (you control the pace), left → right, loosely inspired by *Power Pamplona* (2007). You fly between countries and
collect **residencies (passports)**. Time spent *in* a country (not its airport) counts toward residency (**1 in-game year = 1 real minute**;
Jam Demo Mode: 1 year = 6 s). An application also needs **3 photos of festivals** in that country (proof of integration).
**"Menu hell":** timed phone-UI tasks (booking, residency form, duty-free) that you do *while still running*.
The pursuer is the **departing flight**: miss it and the run ends.

### Game loop
1. **Home airport (UAE) = main menu.** Play → **intro**: the camera holds still while the character runs in from the left edge
   to the centre, then the camera starts following and the phone comes up (`GameTuning.playIntro`; off = the old start where
   the phone opens at the home airport door).
   **The phone is used while running** (you keep moving and jumping). Booking happens in the obstacle-free lobby; right after
   booking the **terminal chase starts** and steps 3–4 pop up on the phone while you dodge crowds and obstacles.
2. **Booking app** (phone): 3 random unowned destinations, each with a random visa (`5 years (5 min)`). Pick within about 5 s or a random one is booked for you. The **flight timer** starts and the terminal begins.
   At every booking an airport chime (`announcement`) plays and a banner reads "FINAL CALL FOR JAPAN! TERMINAL CLOSES IN 0:45".
3. **Residency application** (foreign airports only, about 2 s after booking, only if years in this country ≥ required): mash to fill the form (spam left click / RT), then attach 3 photos (mouse: drag; controller: flick the right stick right). Timeout or too few photos = fail, so try again on a later visit. Success = passport (saved forever).
4. **Freezone shop** (phone, every airport): spend miles on one buff. You hold at most one item.
5. **Airport chase** through crowds to the gate (already running since booking). Terminal length and crowd density scale with the destination's tier.
   Reaching the gate with the phone still open boards anyway (the app is abandoned).
   **Boarding:** the character runs up to the plane parked behind the gate's glass wall and vanishes into the jet bridge; the
   plane taxis, pitches up and climbs out of the window (`takeoffSeconds`, clipped by a SpriteMask), then the flight card shows.
   **Missed flight:** the camera holds while a plane flies past along the window band without you (`flyPastY`), the sad
   trombone plays, then the MISSED card.
6. Reach the gate → **fly** → earn miles (scaled by how long you stayed) → land → **country run** (obstacles, crowds, pickups,
   usually **one festival**: 90% one, 5% none, 5% two). The run has a fixed length ending at the country's airport:
   `visa time × run speed × visaDistanceSlack (0.85)`. The **visa timer is the deadline**. If it hits 0 before you reach the
   airport, you've **overstayed: all miles are confiscated** (the run continues). Residency time only accrues while in the
   country on a valid visa, and pauses at airports. → step 2.
- **Lose:** the flight timer hits 0 → back to the menu. Miles and passports are kept (JSON save).
- **Win:** own every destination passport in the rotation (lifetime collection across runs) → win screen with stats.
  **Demo rotation:** only **USA, Japan and India** can be booked for now (`CountryDatabase.demoRotationOnly` +
  `demoDestinations`), so the win is 3 passports. Untick `demoRotationOnly` to bring back all 10.

### Controls (auto-detected from the last device used)
| Action | Keyboard / mouse | Gamepad |
|---|---|---|
| Move | A/D or ←/→ | Left stick / D-pad |
| Jump | Space / W / ↑ | A (South) |
| Photo | F / right mouse | X (West) |
| Use item | E / Q | Y (North) |
| Phone navigate / select (while running) | Mouse only (click) | Right stick, RT |
| Mash (residency form) | Spam left click (anywhere) | RT |
| Attach photo | Drag onto paper | Flick right stick right |
| Overlay menus (home, missed, win) navigate / confirm | Arrows/WASD, Space/Enter, mouse | Stick/D-pad, A |
| Debug panel | F1 | Select |

The phone never uses the movement/jump controls, so you can run and use it at the same time. Full-screen overlays (home,
flying, missed, won) still disable the Gameplay map.

### Movement
No auto-scroll: **move with A/D or ←/→, the left stick, or the D-pad** (W/↑/Space/A jump). Adopted on `main` after the
`experiment/free-movement` playtest. `GameTuning.autoRun` (off) is kept only as a legacy fallback to the old auto-runner.
- `Gameplay/Move` action (Vector2, only x used). `RunnerController` accelerates toward `Move.x × speed`
  (`moveAcceleration`); stumble, crowd and boost multipliers still apply. The sprite faces the direction of travel.
- Blocks stop only forward motion. Back out of a wall and it re-arms (you still have to jump it).
- You can walk back up to `maxBacktrackDistance` (18 m) from your furthest point; chunks behind that are despawned.
- The camera lead is smaller (`PlayerCamera.freeMoveLeadX` = 3).
- Timers (visa deadline, flight) assume `baseRunSpeed`, so holding right is how you make them.
- `PlaytestAutopilot` holds right (D / stick) during running states.

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
  Input/              PassportFrenzyControls.inputactions (maps: Gameplay, UI, Phone; schemes: KeyboardMouse, Gamepad)
  Placeholder/Sprites Generated white shapes (Square, Circle, Triangle, Capsule), tinted per object
  Placeholder/Backgrounds  Generated parallax silhouettes: bg_<id>_far/_mid per country, bg_clouds, bg_sky (32 x 10 units, 24 PPU)
  Prefabs/            Core, Player, World (PF_Plane), Chunks/{Generic,Airport,Home,Spain,Japan,India}, Obstacles, Festivals, Pickups, UI
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
  (weighted country pool), `AirportSafe` (obstacle-free airport lobby, e.g. during booking), `Chase` (airport pool for the terminal
  length, then the gate). One-off chunks (door, airport entrance, freezone, gate) go through `EnqueueOnce`.
- **Chunk children opt into behaviour** with components: `PaletteTint` (country colors), `DensityGated` (tier thins obstacles/crowds),
  `PickupSocket` (rare buff), `FestivalZone` (photo op), `FlowTrigger` (door / airport entrance / gate).
- **Obstacles** are trigger-based (`ObstacleBase`), with low triggers you jump over. `SlowObstacle` = stumble (speed dip, recovers
  over about 2 s). `BlockObstacle` = speed 0 until you jump. `CrowdZone` = continuous slow while overlapping (unavoidable; the shield doesn't help).
- **Buffs** are ScriptableObjects (`BuffDefinition` subclasses). Passive ones (Shield) apply on acquire; others go into the single item slot.
  The same assets serve the shop and pickups.
- **UI** uses uGUI prefabs. HUD and FlightBoard poll state each frame. Phone apps are coroutines that GameFlow `yield return`s on.
- **Input**: `InputRouter` exposes actions. Maps: Gameplay, UI (overlay menus), **Phone** (`Navigate` = right stick,
  `Submit` = RT, `Mash` = RT / left mouse). `SetMenuMode(true)` disables the Gameplay map (full-screen overlays only).
  `SetPhoneMode(true)` keeps Gameplay live and swaps the EventSystem's `InputSystemUIInputModule.move/submit` to the Phone
  actions (mouse point/click are untouched). `InputDeviceTracker` + `InputPrompts` produce device-correct prompt text
  (`"{Jump} Jump"` → `[Space] Jump` / `[A] Jump`; phone tokens `{Submit}` `{Mash}` `{PhoneNav}` `{Attach}`; overlays use `{Confirm}`).
- **Audio**: `AudioManager.Play(SoundIds.X)` and `PlayMusic(country)`. Every hook is a no-op until a clip is assigned in
  `Data/SoundLibrary.asset` (or `CountryDefinition.music`).
- **ParallaxBackground** (component on `PF_GameRoot`, renderers under its `Background` child; `GameRoot.background`): sky and
  ground haze glued to the camera plus tiled silhouette layers (clouds 0.05, far 0.15, mid 0.4 of world motion; tile width 32)
  that connect across chunk seams. `SetCountry(c)` (from `GameFlow.Land`/`EnterHome`) swaps `CountryDefinition.backgroundFar/Mid`
  and tints them from the palette (`ParallaxMath.LayerTint`). Pure math in `ParallaxMath` (tested).
- **PlaneDeparture** (`PF_Plane`, logic on the root, primitives under `Visual`, nose right): `Play(kind, seconds)` drives the
  `PlaneFlightCurve` (`Takeoff` for boarding, `FlyPast` for a missed flight; tested). The gate chunk nests one under its
  `WindowMask` (SpriteMask, range -17..-14); `ChunkSpawner.GateChunk` exposes it. `CountryDatabase.planePrefab` is spawned for the fly-past.
- **Events**: `GameEvents` (static) for UI/audio feedback only: toast, photo taken, hit, item changed, passport, miles.

## 6. Prefab & naming conventions
- Prefabs: `PF_<Category>_<Name>`, e.g. `PF_Chunk_Spain_Bulls`, `PF_Obstacle_Suitcase`, `PF_UI_Phone`.
- Data: `Country_<Name>`, `Buff_<Name>`, `GameTuning`, `CountryDatabase`, `SoundLibrary`.
- **Logic on the root, visuals on a child named `Visual`** (SpriteRenderer). A Visual may have its own child sprites for compound
  placeholder shapes. Extra visual-only children: `ShieldVisual` (player), `Label` (world TMP text).
- Chunk pivot = left edge at ground height. Ground top is y = 0. Default chunk length is 24 units. Player jump apex ≈ 2.4 units,
  jump distance ≈ 6 units at base speed. Keep block obstacles ≤ 1.3 high and ≤ 1.5 wide.
- Sorting orders: parallax -40…-30 (haze -40, clouds -38, far -35, mid -30), chunk background -20…-10, ground 0, props 1–4,
  obstacles/festivals 5, pickups 6, player 10. The gate's `WindowMask` clips -17..-14 (the plane at -16/-15).
- Tune values in the Inspector (prefab fields or `GameTuning`), not in code.

## 7. Art-swap contract (for the artist / artist's agent)
1. Open any prefab in `Assets/_Project/Prefabs/` and select its `Visual` child.
2. Put your sprite (from `Assets/Art/...`) in the SpriteRenderer, and adjust the Visual transform scale/offset to fit. Leave the root,
   its colliders and its scripts alone.
3. If the Visual has a **PaletteTint** component, it recolors the sprite per country at runtime. For full-color art, delete
   PaletteTint (or set Role = None) and set the SpriteRenderer color to white.
4. Compound placeholder shapes (e.g. the plane or a crowd of capsules) are children of `Visual`. Delete or replace them freely.
   Chunks keep their decorative sprites (buildings, windows, signs) under a `Backdrop` child. Also art, also free to replace.
5. The player's `RunnerController` animates the Visual transform (squash on jump, run bob/lean, hit flash, boost tint).
   When you add a real Animator, untick **Procedural Animation** on `PF_Player` (and, if needed, animate a child *under* Visual).
   Current state: `PF_Player/Visual` has a `SpriteFrameAnimator` playing `Assets/Art/RunningAnimation`, `IdleAnimation` and
   `JumpingAnimation` (whole textures, so how you slice them in the importer doesn't matter). Add or replace frames in its
   `frames` / `idleFrames` / `jumpFrames` lists; Procedural Animation is off.
   Obstacles with artist art: `PF_Obstacle_Suitcase` (`Assets/Art/SuitCases`, a random colour per spawn through
   `RandomSpriteVariant`), `PF_Obstacle_Cone`, `PF_Obstacle_Barrier`, `PF_Obstacle_LuggageCart`, `PF_Obstacle_Puddle` and
   `PF_Obstacle_WetFloor` (`Assets/Art/TrafficCone`, `Barrier`, `LuggageCart`, `Puddle`, `WetFloorSign`). These reference the
   importer's sliced sprites (`<Name>_0`), so commit the art's `.meta` files with it.
6. UI: phone/menu prefabs are in `Prefabs/UI`. Swap Image sprites and TMP fonts freely. Don't rename objects that scripts
   reference through serialized fields (re-link them in the Inspector if you do).
7. **Never run `Passport Frenzy/Rebuild ALL (overwrites)`** after you've started swapping art: it regenerates every prefab from code.
   `Build Missing Assets` is safe (it only creates what doesn't exist).
8. Sounds: placeholder SFX are generated by `Tools/sfx/generate_sfx.py` into `Placeholder/Audio/SFX/`; replace them by
   assigning your clips in `Data/SoundLibrary.asset` (ids: jump, hit, block, shield_break, photo, pickup, use_item, booking,
   phone_open, mash, stamp, rejected, buy, takeoff, flight_missed, win, music_<countryId>).

## 8. How to add a country (no code)
1. Duplicate a `Data/Countries/Country_*.asset`. Set `id` (unique, e.g. `br`), name, palette, tier (1–3),
   required residency years, visa range and festival names.
2. Optional: set `signatureFestivalChunk` to a unique festival chunk (its FestivalZone name override is the festival name).
   Optional: make unique chunks by duplicating a chunk prefab in `Prefabs/Chunks/` (e.g. `PF_Chunk_Spain_Bulls`) into a new
   folder, swapping obstacles, and adding them (with weights) to the country's `chunkPool`. Leave `chunkPool` empty to use the
   generic pool, and `airportChunkPool` empty to use the default airport.
3. Optional: give it its own `festivalZonePrefab` and `music`.
4. Add the asset to `Data/CountryDatabase.asset → destinations`. Done (the win condition = owning all destinations).
   While `demoRotationOnly` is on, also add it to `demoDestinations` (only that list is bookable). Code reads
   `CountryDatabase.ActiveDestinations` / `CountOwned`, never `destinations` directly.

## 9. How to tune
- `Data/GameTuning.asset` holds every number: run speed, jump, stumble/crowd multipliers, booking/mash/photo/shop windows, flight timer
  slack, miles, tier tables (obstacle density, crowd density, terminal length), spawn distances and pickup chance.
- Festivals per visit: `noFestivalChance`, `twoFestivalChance`, `minRunSecondsForTwoFestivals`. They're placed by
  `FestivalPlanner` (not the random chunk pools): signature chunk first (e.g. La Tomatina), distinct names.
- Flight timer: `GameTuning.FlightSeconds(tier, residencyAhead)` = lobby run + expected chase + `menuOverlapAllowance` (0.4) ×
  phone time after booking + padding (the phone overlaps the chase, so only part of its time is added).
- Boarding / missed flight: `takeoffSeconds` (2.2, the clip length), `boardingOffsetX`, `boardingRunMaxSeconds`, `flyPastY`,
  `flyingScreenSeconds` (1.5 now that the plane carries the moment).
- Parallax: `parallaxFarBlend` / `parallaxMidBlend` (how far the silhouettes move from the sky colour toward the country's
  primary colour); layer motion/orders on the `ParallaxBackground` component; regenerate the silhouettes with
  `Passport Frenzy/Generate Country Backgrounds (overwrite)` after editing the recipes in `Editor/PrototypeBuilder.Backgrounds.cs`.
- Visa deadline: `visaDistanceSlack` (lower = more spare time). `CountryDatabase.fillerChunk` (6 m) lands the airport
  entrance exactly at the end.
- **Jam Demo Mode** (`jamDemoMode`, also toggled in the debug panel at runtime): 1 year = 6 s, country run ≤ 30 s, chase ≈ 30 s.
- **Intro** (`playIntro`, `introOffscreenMargin`, `introFollowSeconds`) and the final-call banner (`announcementSeconds`;
  position and size on `HudView`: `announcementY`, `announcementFontScale`).
- Per-obstacle overrides are on the prefab (`SlowObstacle.stumbleMultiplier`, `CrowdZone.speedMultiplier`, `DensityGated.baseChance`).
- Per-country values are in its `Country_*` asset.

## 10. Debug panel (F1 / Select)
Skip 30 s, +1 year here, +500 miles, spawn festival ahead, force airport transition, +3 photos, toggle demo mode, win (grant all
passports), clear save. The header shows the state, device, years in country and the flight timer.

## 11. Agent testing notes (Unity MCP)
- **Auto Refresh is OFF** in this editor (user preference): after editing scripts or assets, run `AssetDatabase.Refresh()`
  via MCP `eval` (or MCP `recompile`), then check `recompile_status` / `console`. Batch your edits first.
- **Never use MCP `run_tests` here**: it has hung the editor's main thread every time (all later commands time out; only a
  force-close + relaunch recovers). Run EditMode tests from the Test Runner window, or via `eval` with
  `UnityEditor.TestTools.TestRunner.Api.TestRunnerApi` and callbacks (asynchronous).
- Play mode only runs at full speed while the Unity window is the **foreground app**. When unfocused it crawls at about 2 fps
  (`Application.runInBackground` is on, but the editor still throttles). Bring Unity to the front before timing-sensitive tests.
- `InputSystemSettings.inputsettings.asset` sets background behavior = *Ignore Focus* and editor input = *all devices go to
  the Game view*, so injected input works while unfocused.
- **Use `PassportFrenzy.EditorTools.PlaytestDriver`** (MCP eval) to press keys and buttons, e.g.
  `PlaytestDriver.TapKey(UnityEngine.InputSystem.Key.Space)`, `PlaytestDriver.TapPad(UnityEngine.InputSystem.LowLevel.GamepadButton.South)`,
  `PlaytestDriver.FlickStick(new UnityEngine.Vector2(1,0))`, `FlickRightStick(...)`, `TapTrigger()` (RT), `ClickMouse(screenPos)`,
  `DragMouse(from, to)`. It injects on virtual devices inside the game's own input update.
  The Autopilot uses the phone like a player would: gamepad = right stick + RT; keyboard mode = mouse clicks, left-click mashing, drags.
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
- `Passport Frenzy/Patch Gate Plane + Parallax (safe for art)` re-applies the idempotent prefab patches (gate glass wall + mask +
  nested `PF_Plane`, `ParallaxBackground` on `PF_GameRoot`, country background sprites, `planePrefab`). `Rebuild ALL` drops
  both the painted airport wall and these patches (the patches are re-run by `Build`, the wall is not).
- Regenerating assets: `Passport Frenzy/Build Missing Assets` (safe). `Passport Frenzy/Rebuild Data Assets (safe for art)`
  refills countries, the database and the sound library from code without touching prefabs. `Rebuild ALL` only before art exists, or with the artist's agreement.
