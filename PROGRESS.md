# Passport Frenzy — Progress

_Updated with every commit. If you're an agent resuming work: read AGENTS.md, then continue at the first unchecked milestone._

## Milestones
- [x] M1 Project setup + docs skeleton (folders, asmdefs, input actions, TMP essentials, AGENTS/CLAUDE/PROGRESS, runtime scripts + tests compiling)
- [x] M2 Runner + input + chunks (placeholder sprites, PrototypeBuilder, GameRoot, player, camera, spawner, generic chunks)
- [x] M3 Obstacles / crowds (slow, block, crowd, moving; all prefabs built)
- [x] M4 Country run: 10 countries, visa timer, HUD, festivals/photos, Spain/Japan/India prefabs
- [x] M5 Airport + booking app + flight timer + chase + gate + flight transition
- [x] M6 Residency application (mash, drag/flick photos, both failure paths)
- [x] M7 Freezone shop + buffs + pickups
- [x] M8 Home menu + lose/win + save
- [x] M9 Debug panel + Jam Demo Mode
- [x] M10 Polish pass (procedural run bob/lean, boost tint, crowd status, menu focus, scale fix). More polish welcome; see Next tasks

## Done
- Folder layout, asmdefs (Runtime / Editor / Tests.EditMode), TMP Essentials imported.
- `Input/PassportFrenzyControls.inputactions`: Gameplay (Jump, Photo, UseItem) and UI (Navigate, Submit, Cancel, Point, Click,
  ScrollWheel, Mash, ToggleDebug); schemes KeyboardMouse / Gamepad.
- All runtime scripts written and compiling; 9 EditMode tests pass.
- `Editor/PrototypeBuilder*.cs` generates: 4 placeholder sprites, GameTuning, 3 buffs, 11 countries, CountryDatabase,
  SoundLibrary, 14 obstacle/crowd prefabs, 4 festival prefabs, pickup, 28 chunk prefabs (generic, Spain, Japan, India, airport, home),
  player, camera, 13 UI prefabs, PF_GameRoot and Game.unity (the only build scene).
- Playtested via MCP: home menu → Play → door → booking (auto-pick) → shop (timeout) → chase (blocked by luggage cart) →
  flight missed → back to the home menu. No console errors.
- `Editor/PlaytestDriver.cs` + `Editor/PlaytestAutopilot.cs`: input injection and a bot for MCP end-to-end tests (AGENTS.md §11).
- Verified with the Autopilot (demo mode):
  - Gamepad: booking (D-pad + A) → mash (A) → stick-flick 3 photos → **APPROVED** (Japan passport saved) → bought Speed Boost →
    used it in the chase → gate → flew → France run with 3 auto festival photos → airport → France APPROVED.
  - Keyboard: residency with 1 photo → **REJECTED "NOT ENOUGH PHOTOS 1/3"**; shop skipped.
  - Keyboard: no mashing → **REJECTED "TOO SLOW! Form incomplete"** → caught the flight, landed in the next country.
  - Missed flight → "FLIGHT MISSED" screen → home menu, with miles/passports kept in the JSON save.
  - Keyboard + **mouse drag-and-drop** photos → Spain APPROVED.
  - Normal mode (demo override off): 2-year China visa = 120 s; debug +years/+photos/force airport → full airport loop.
  - Debug panel: F1 opens it; mouse clicks on "Toggle Jam Demo Mode" (flipped ON→OFF) and "WIN" → win screen
    (10/10, stats) → "Home Airport" → home menu.
  - Gamepad from the home menu: Play (A) → booking → shop → chase. Prompts switch to [A]/[X]/[Y]/[Select] automatically.

## Stubbed / placeholder
- All art is generated primitive shapes. All audio hooks are silent (null clips); music per country is wired the same way.
- No real animation: the player uses procedural squash/bob/lean (toggle on PF_Player).
- Rogue-like upgrades that bias visa RNG (mentioned in the original pitch) aren't implemented; the visa range lives in each Country asset.

## Known bugs
- One Autopilot chase (Germany → ?) ended in a missed flight even though it jumped when blocked. Cause not confirmed: maybe
  bad luck with obstacles, maybe a seam snag. `[Flow] Flight missed ... m short` is now logged to diagnose it.
- Editor-only noise unrelated to the game: "UnityConnectWebRequestException: Token Exchange failed" and
  "Account API did not become accessible" (Unity account/AI Assistant packages, offline or unfocused editor).
- `simulate_key` (MCP) doesn't drive gameplay. Use PlaytestDriver instead (see AGENTS.md).

## Decisions log
- **Passports are a lifetime collection** (user decision). Booking hides countries owned from any run; win = all 10 owned.
  Residency years and photos are per run.
- Kept Unity 6000.6.5f1 + URP 2D + Input System 1.20 already in the template. No new packages.
- VersionControlSettings switched back to *Visible Meta Files* (collab-proxy had flipped it to Unity Version Control; we use git).
- Prefabs/SOs come from an editor **PrototypeBuilder** script run through the MCP (reproducible). `Build Missing Assets` is safe; `Rebuild ALL` overwrites.
- Jam Demo Mode: 1 year = 6 s; country run clamped to 30 s; terminal length clamped so the expected chase is about 30 s.
  Time labels are built from seconds-per-year, so they always show the real duration ("5 years (30 s)").
- Demo mode toggle in the debug panel is a runtime override (doesn't dirty the asset).
- Shield applies immediately and doesn't use the item slot; it absorbs one Slow/Block hit, not crowds.
- Pickups are ignored ("Hands full!") when the item slot is occupied.
- Residency time accrues during the country run and that country's airport sequence (until takeoff), never at home.
- Block obstacles are trigger logic (speed 0 until jump), not physical walls.
- Run In Background enabled. Input settings asset: background behavior Ignore Focus (needed for MCP-driven tests).
- Auto Refresh disabled in the editor at the user's request (agents refresh manually).
- Removed template `Example.cs` and `SampleScene`. Domain reload on play is disabled in this project, so statics reset via SubsystemRegistration hooks.
- Mash is Space / A / RT, or clicking the on-screen button (mouse users). Photo attach on keyboard also works with → (Navigate).
- Removed the duplicate Gameplay/ToggleDebug and the unused Flick action. Space added to UI/Submit.
- Flight timer = shop window + (residency budget if eligible) + filler run + expected chase (terminal / speed × crowd factor × slack) + padding.
  Finishing menus early banks time.
- Booking excludes the country you're in, unless it's the last unowned one.
- Fixed: the player Visual used to drift to scale 1 (the feedback lerp targeted Vector3.one). It now returns to its authored scale.
- Booking and shop wait 0.35 s / 0.5 s before auto-selecting the first entry for keyboard/controller, so leftover
  jump or mash presses can't book or buy by accident (mouse works immediately).
- Menus re-select their first entry whenever selection is lost (a stray mouse click no longer strands keyboard/controller users).
- When the collection is complete, Play leads straight to the win screen ("New Collection" resets the save).
- In demo mode a single 30 s visa (5 years) already satisfies most residency requirements, which keeps the demo loop short.

## Next 5 tasks
1. Polish: juice (landing dust, stamp animation, plane taking off on the flight board), camera framing.
2. Playtest feel at real speed with a human: jump tuning, obstacle density, chase timer slack.
3. Look into the one unexplained missed chase (see Known bugs).
4. Audio: drop clips into SoundLibrary (all hooks are wired and silent).
5. Art: the artist swaps the Visual sprites per AGENTS.md §7.
