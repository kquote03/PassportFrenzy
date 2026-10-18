# Passport Frenzy: Handoff (2026-10-18)

> For the next agent or teammate. Read **AGENTS.md** first (rules, architecture, MCP testing notes), then this file,
> then **PROGRESS.md** (full history, decisions, known bugs). Keep all three updated with every commit.

## Where things stand
Everything below is on `origin/main`. The game loop is complete and playable:
- **Home** → booking → (residency) → shop → **chase** → fly → **country run** → airport → …
- **Missed flight** = back to the menu.
- **All 10 passports** = win.

Done in this session, in order:

| Commit | What | Verified |
|---|---|---|
| `0bd6e33` | **Free movement** is the movement method (A/D, ←/→, left stick, D-pad; `GameTuning.autoRun` off = legacy) | User playtested OK |
| `d01fce5` | **Phone while running**: new `Phone` action map (gamepad R-stick + RT; keyboard players use the mouse only, spam left click to mash). Menus overlay the terminal chase; the flight timer is `GameTuning.FlightSeconds` | User playtested OK, Autopilot on both devices |
| `b966244` | **Airport backdrop**: the user's generated tileable terminal image is the wall of the indoor airport chunks | Screenshots |
| `5a46b75`, `84d7a56` | **Player run cycle** from the artist's `Assets/Art/RunningAnimation` via `SpriteFrameAnimator` (art committed unchanged) | Play mode screenshot, user OK |
| `c1ccace` | **Placeholder SFX**: 17 generated clips (`Tools/sfx/generate_sfx.py`) assigned in `SoundLibrary.asset` | Clips resolve in edit mode |
| *(this commit)* | **Airport UI restyle** (UI subagent): rounded, airport palette, boarding-pass booking cards, duty-free tags, departures-board flight board, visible controller selection. Plus 2 small fixes (see below) | Subagent's clean Play session: zero errors; **the last 2 fixes are not compiled yet** |

## ⚠ Verify first (the editor was blocked when this was pushed)
1. **Unity showed a modal "Scene(s) Have Been Modified" dialog** (from an EditMode test run) that blocked all MCP calls.
   Dismiss it in the editor; also close the "Unity Version Control" window if it's open.
2. **`Assets/_Project/Scenes/Game.unity` has local, uncommitted changes on the original machine only**: about 650 lines of
   prefab-instance overrides (UI anchors/sizes zeroed, TMP style hashes). They were deliberately **not** pushed because they
   might override the new UI layout. If you see them, inspect them, and most likely revert them
   (`git checkout -- Assets/_Project/Scenes/Game.unity`). The scene should only hold `PF_GameRoot` + `PF_MainCamera` with no layout overrides.
3. **Not compiled yet** (written while the editor was blocked):
   - `Scripts/UI/FlightBoardView.cs`: the gate countdown shows whenever the spawner is in Chase mode (the chase starts at booking,
     so it now shows during residency/shop too); before booking it reads "Book your flight!".
   - `Scripts/Input/InputPrompts.cs` + `Scripts/UI/MainMenuView.cs`: new `{PhoneControls}` token, so the main menu bar reads
     `[Mouse] Phone` / `[R-Stick]+[RT] Phone`.
   → Run `AssetDatabase.Refresh()` (Auto Refresh is OFF), check `recompile_status` / console, run the 16 EditMode tests
   (`PassportFrenzy.Tests.EditMode`), then a Play-mode smoke test (PlaytestAutopilot, AGENTS.md §11).

## Next tasks (agreed with the user, in order)
The user wants to **playtest after each feature** before the next one starts: implement → verify → stop → wait for feedback → push.

1. **Playtest the UI restyle + SFX + backdrop** with the user; fix the feedback.
2. **Tighter deadlines + miss shake** (planned "Step 3"):
   - `GameTuning` code defaults **and** `Data/GameTuning.asset`:
     - booking window 4–6 → 3–4.5 s
     - residency delay 2 → 1 s
     - mash window 4–6 → 3.5–4.5 s (presses 16–24)
     - photo attach 8 → 5 s
     - shop 7 → 5 s
     - result display 1.6 → 1.2 s
     - flight padding 6 → 4 s
     - chase slack 1.25 → 1.15
   - **Visa slack:** keep `visaDistanceSlack` 0.85 (or loosen). Autopilot visits already overstay sometimes in free movement;
     confirm with the user before tightening.
   - **Miss shake:** add `PhoneView.Miss(toast, strength)`: camera shake (`PlayerCamera.Shake`) + phone-frame shake + `rejected`
     sound + toast. Call it on:
     - booking timeout
     - mash timeout
     - photo timeout / not enough photos
     - shop closing without a choice (Skip doesn't count)
     - a failed purchase (half strength)
3. **Placeholder art strategy** (research done, not yet applied):
   - **Primary:** Kenney CC0 packs (New Platformer Pack, Toon Characters, UI Pack 2.0, Particle Pack, Flag Pack) in
     `Assets/_Project/Placeholder/Kenney/`, with only the `Visual` sprites swapped via a re-runnable "Apply Placeholder Art" menu
     that skips anything under `Assets/Art/`.
   - **Extras:** generated grayscale per-country skylines (tinted by `PaletteTint`), parallax, festival particles
     (Tomatina splats, Hanami petals, Holi powder), light bloom/vignette.
   - **Fallback:** upgrade the procedural builder only.
   - **Cost:** about 5–6 h. Kenney is CC0 (no credit needed); CC-BY packs need credits.
4. **Player readability:** the artist's run frames are uncoloured line art with transparent insides (the backdrop shows through
   the body). Either the artist fills them, or generate a silhouette fill outside `Assets/Art`.
5. **Music:** `music_<countryId>` hooks in `SoundLibrary.asset` are still empty.

## Gotchas
- **Never create or modify anything in `Assets/Art/`** (artist only). Reading and referencing is fine. The run cycle works from whole
  textures because the artist's importer auto-slices frames unevenly.
- **Auto Refresh is OFF**; refresh/recompile explicitly after edits (batch them).
- **Two agents share one editor:** coordinate Play mode, and don't `Refresh()` while another agent has half-done script edits.
- **Builder menus:**
  - `Rebuild ALL` regenerates every prefab from code. That drops the hand-applied airport backdrop and player animation setup
    (the UI is regenerated from code, so it survives).
  - Safe: `Build Missing Assets`, `Rebuild Data Assets (safe for art)`, `Rebuild UI Prefabs (safe for world art)`
    (refuses to run in Play mode), and `Generate UI Sprites`.
- **Dark UI panels must stay fully opaque:** linear colour space makes translucent dark panels look grey over the bright terminal.
- **Unrelated console noise:**
  - "Exceeded budget for maximum input event throughput" comes from a PlayStation VR2 controller pair flooding input on the
    original dev machine.
  - Unity account/AI Assistant token warnings.
- **Save file:** test runs modified the local save (stats include bot runs). Use the debug panel's "clear save" for a clean demo.
- **Leftover screenshots:** UI review screenshots are in `Logs/ui/` (not tracked).
