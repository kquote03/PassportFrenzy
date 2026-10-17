# Passport Frenzy — Progress

_Updated with every commit. If you're an agent resuming work: read AGENTS.md, then continue at the first unchecked milestone._

## Milestones
- [x] M1 Project setup + docs skeleton (folders, asmdefs, input actions, TMP essentials, AGENTS/CLAUDE/PROGRESS, runtime scripts + tests compiling)
- [x] M2 Runner + input + chunks (placeholder sprites, PrototypeBuilder, GameRoot, player, camera, spawner, generic chunks)
- [x] M3 Obstacles / crowds (slow, block, crowd, moving; all prefabs built)
- [ ] M4 Country run: 10 countries, visa timer, HUD, festivals/photos, Spain/Japan/India prefabs
- [ ] M5 Airport + booking app + flight timer + chase + gate + flight transition
- [ ] M6 Residency application (mash, drag/flick photos, both failure paths)
- [ ] M7 Freezone shop + buffs + pickups
- [ ] M8 Home menu + lose/win + save
- [ ] M9 Debug panel + Jam Demo Mode
- [ ] M10 Polish pass

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
- `Editor/PlaytestDriver.cs`: input injection helper for agents (see AGENTS.md §11).

## Stubbed / placeholder
- All art is generated primitive shapes. All audio hooks are silent (null clips).

## Known bugs
- (none yet)

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

## Next 5 tasks
1. Playtest the country run: obstacles, jump feel, festival photo, visa timer → airport transition.
2. Playtest booking choice, residency (success, timeout, too few photos) and shop purchase.
3. Controller pass with the virtual gamepad (menus, flick attach).
4. Debug panel and demo-mode toggle verification; win screen.
5. Polish: juice, prompt glyphs, tuning.
