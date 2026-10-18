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

## Free movement (adopted on main)
- [x] Free movement (no auto-scroll); `GameTuning.autoRun` is off and kept only as a legacy fallback: A/D, ←/→, left stick, D-pad.
  Blocks re-arm when you back off; 18 m backtrack limit; facing flip; smaller camera lead; `{Move}` prompt on the menu.
- Verified via MCP: idle = no movement; D → right (to 44.6), A → back to the 18 m limit (26.4) with the sprite flipped; device prompt
  switches to [L-Stick]. Autopilot loops from the home menu on keyboard and gamepad: door → booking → shop → chase → land →
  country run → airport → residency. One China visit overstayed (obstacles in a tight window); Germany/UK were made on time.
- Merged into `main` after the user's playtest (the branch stays on the remote for reference).

## Airport backdrop (placeholder art, awaiting user playtest)
- [x] The user's generated, horizontally tileable `Placeholder/airport-main-section-scrollable.png` (1536×1024, imported at
  64 PPU = 24 × 16 units, one copy per 24-unit chunk) is the `Backdrop/Wall` sprite of the indoor airport chunks:
  Airport Arrival/Lobby/Freezone/Gate, the 4 Chase chunks, and Home Door/Lobby. Its painted floor line sits on the ground (y = 0).
- In those chunks:
  - Wall PaletteTint removed (full-colour art).
  - Placeholder Window/Mullion/Stripe, Bench/Palm props and the flat ground strip disabled (not deleted; the ground collider is
    unchanged).
  - Signs, door frames, gate window/plane and duty-free shelves kept.
  - The street-side `Airport_Entrance` chunk is unchanged.
- Note: `Rebuild ALL` would regenerate these prefabs and drop the backdrop (art-swap contract §7).

## Phone while running (done, playtested OK)
- [x] Gameplay stays live while the phone is open (`InputRouter.SetPhoneMode`). New **Phone** action map: right stick navigate,
  RT select/mash; keyboard players use the mouse only (click, drag, spam left click to mash). Space/Enter/A never touch the phone.
- [x] Menus overlay the chase: booking in the lobby, then the terminal starts immediately; residency + shop pop up during it.
  Reaching the gate with the phone open boards anyway. Flight timer = `GameTuning.FlightSeconds` (phone time × 0.4 overlap allowance).
- Verified via MCP Autopilot:
  - **Gamepad:** R-stick + RT booked, RT mashed, R-stick flicks attached photos, RT bought in the shop.
    Mid-shop, the player ran at 8 m/s with jump enabled, the UI module was on Phone/Navigate + Phone/Submit, and the spawner was in Chase mode.
  - **Keyboard + mouse:** a click booked, left-click spam filled the form, a drag attached 3 photos (India APPROVED), a click bought an item.
  - Space/Enter taps during booking did not book (it timed out).
  - Gate trigger mid-shop → Flying with the phone closed.
  - 16 EditMode tests pass.
- Risk seen in testing: two bot visits (Japan, Germany) overstayed the visa by a fraction of a second while holding right; free
  movement plus obstacles leaves little spare time at `visaDistanceSlack` 0.85. To decide in the deadline-tuning step.

## Post-prototype fixes (after M10)
- [x] Residency time pauses at airports (and after a visa overstay). The HUD shows "(paused at airport)".
- [x] Festivals per visit are planned by `FestivalPlanner`: 90% one, 5% none, 5% two (two only on runs ≥ 20 s with distinct
  names; the signature festival goes first). Festival chunks were removed from the random pools.
- [x] Visa deadline: each country run is a fixed length ending at the airport (visa × speed × 0.85). Overstaying sets miles to 0
  (toast, shake, `visa_overstay` sound hook); the run continues. HUD: "Visa left 0:12 - Airport in 64 m" (red when you can't
  make it any more).
- Verified via MCP Autopilot: residency frozen through booking/shop/chase (10.900 s before and after). China/UK visits had
  1 festival each, and reached the airport with 1.1 s of visa left. Blocking the player made the visa expire → "VISA OVERSTAYED!
  1320 miles confiscated", miles 0, residency capped at the visa length, and the run still reached the airport.
  15 EditMode tests pass (6 new planner/visa tests).

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
- Residency time accrues only during the country run on a valid visa (paused at airports and after an overstay), never at home.
- Photos required stays 3 (user decision), so with about 1 festival per visit a residency typically takes about 3 visits.
- Airport at a fixed distance (user decision): visa = deadline. Overstay = lose all lifetime miles; the run continues (user decision).
- The airport entrance trigger lands exactly at the planned length: whole chunks only while they fit, plus 6 m filler chunks.
  If a planned festival would no longer fit, it's placed early.
- **Free movement is the movement method** (user decision after playtesting the experiment branch).
- Block obstacles are trigger logic (speed 0 until jump), not physical walls.
- Run In Background enabled. Input settings asset: background behavior Ignore Focus (needed for MCP-driven tests).
- Auto Refresh disabled in the editor at the user's request (agents refresh manually).
- Removed template `Example.cs` and `SampleScene`. Domain reload on play is disabled in this project, so statics reset via SubsystemRegistration hooks.
- Phone controls (user decision): keyboard players use the mouse only (spam left click to mash, drag photos); gamepad uses the
  right stick + RT. Movement/jump keys never touch the phone, so menus can be used while running (menus overlay the chase).
- Removed the duplicate Gameplay/ToggleDebug and the unused Flick action. Space added to UI/Submit.
- Flight timer (starts after booking) = lobby run + expected chase (terminal / speed × crowd factor × slack)
  + `menuOverlapAllowance` × phone time after booking + padding.
- Booking excludes the country you're in, unless it's the last unowned one.
- Fixed: the player Visual used to drift to scale 1 (the feedback lerp targeted Vector3.one). It now returns to its authored scale.
- Booking and shop wait 0.1 s before auto-selecting the first entry for the controller, so a held RT can't book or buy by
  accident (mouse works immediately).
- Menus re-select their first entry whenever selection is lost (a stray mouse click no longer strands keyboard/controller users).
- When the collection is complete, Play leads straight to the win screen ("New Collection" resets the save).
- In demo mode a single 30 s visa (5 years) already satisfies most residency requirements, which keeps the demo loop short.

## Next 5 tasks
1. Polish: juice (landing dust, stamp animation, plane taking off on the flight board), camera framing.
2. Playtest feel at real speed with a human: jump tuning, obstacle density, chase timer slack.
3. Look into the one unexplained missed chase (see Known bugs).
4. Audio: drop clips into SoundLibrary (all hooks are wired and silent).
5. Art: the artist swaps the Visual sprites per AGENTS.md §7.
