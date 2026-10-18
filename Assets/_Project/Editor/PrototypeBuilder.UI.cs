using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PassportFrenzy.EditorTools
{
    public static partial class PrototypeBuilder
    {
        internal static GameObject UiRootPrefab;
        static string UiDir => PrefabsDir + "/UI";

        // Airport wayfinding palette (matches the painted terminal backdrop): departure-board navy, signage teal,
        // sand/cream paper, flap-display gold, alert coral.
        static readonly Color Navy = Hex("#13213A");
        static readonly Color NavyDeep = Hex("#0B1628");
        static readonly Color Teal = Hex("#1F7A78");
        static readonly Color TealLight = Hex("#3FB8AF");
        static readonly Color Paper = Hex("#FFF8EA");
        static readonly Color Sand = Hex("#F3E5C8");
        static readonly Color Gold = Hex("#F4B43A");
        static readonly Color Coral = Hex("#E5534B");
        static readonly Color Mint = Hex("#DDF3E4");
        static readonly Color Slate = Hex("#2C3E5C");
        static readonly Color Mist = Hex("#E6EBF1");
        static readonly Color ScreenBg = Hex("#F7F5F0");
        static readonly Color Ink = Hex("#1B2433");
        static readonly Color Muted = Hex("#5E6B7C");

        [MenuItem("Passport Frenzy/Rebuild UI Prefabs (safe for world art)", priority = 3)]
        static void RebuildUiMenu() => Debug.Log(RebuildUi());

        /// <summary>
        /// Regenerates the UI sprites and every UI prefab (PF_UI_*), then relinks PF_GameRoot to the new UI root.
        /// World prefabs, chunks and data are untouched. MCP: PassportFrenzy.EditorTools.PrototypeBuilder.RebuildUi()
        /// </summary>
        public static string RebuildUi()
        {
            // In Play mode AddComponent runs Awake on half-built views; their exceptions would save disabled components.
            if (EditorApplication.isPlayingOrWillChangePlaymode) return "[PrototypeBuilder] Exit Play mode before rebuilding the UI.";
            _log.Clear();
            GenerateUiSprites(true);
            _overwrite = true;
            try { BuildUiPrefabs(); }
            finally { _overwrite = false; }

            var path = PrefabsDir + "/Core/PF_GameRoot.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var gr = root.GetComponent<GameRoot>();
                var ui = root.GetComponentInChildren<UIRoot>(true);
                if (gr.ui != ui) { gr.ui = ui; _log.Add("relinked GameRoot.ui"); }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            return "[PrototypeBuilder] UI rebuilt\n" + string.Join("\n", _log);
        }

        // ================================================================== uGUI helpers

        static RectTransform UI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = (RectTransform)go.transform;
            if (parent != null) rt.SetParent(parent, false);
            return rt;
        }

        static RectTransform Stretch(RectTransform rt, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        /// <summary>Horizontal band pinned to the top of its parent.</summary>
        static RectTransform TopBand(RectTransform rt, float top, float height, float left = 0f, float right = 0f)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(left, -top - height);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        static readonly Vector2 TopLeft = new Vector2(0f, 1f), TopCenter = new Vector2(0.5f, 1f), TopRight = new Vector2(1f, 1f);
        static readonly Vector2 Center = new Vector2(0.5f, 0.5f), BottomCenter = new Vector2(0.5f, 0f), BottomLeft = Vector2.zero;
        static readonly Vector2 BottomRight = new Vector2(1f, 0f), MidLeft = new Vector2(0f, 0.5f), MidRight = new Vector2(1f, 0.5f);

        static Image Img(RectTransform rt, Color c, bool raycast = false, Sprite sprite = null)
        {
            var i = rt.gameObject.AddComponent<Image>();
            i.sprite = sprite != null ? sprite : SqSprite;
            i.color = c;
            i.raycastTarget = raycast;
            return i;
        }

        /// <summary>Use the 9-sliced rounded sprite with the given corner radius (UI units).</summary>
        static Image Round(Image img, float radius)
        {
            img.sprite = RoundSprite;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = BaseRadius / Mathf.Max(1f, radius);
            return img;
        }

        /// <summary>A rounded panel (no sharp corners anywhere in the UI).</summary>
        static Image Panel(RectTransform rt, Color c, float radius, bool raycast = false) => Round(Img(rt, c, raycast), radius);

        /// <summary>Soft drop shadow around a rounded panel (an outer-only shadow, so it can sit on top of the panel's fill).</summary>
        static void Shadow(RectTransform target, float radius, float alpha = 0.5f)
        {
            var rt = Stretch(UI("Shadow", target), -radius, -radius, -radius, -radius);
            var img = Img(rt, new Color(0.03f, 0.06f, 0.12f, alpha), false, ShadowSprite);
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 24f / Mathf.Max(1f, radius);
            rt.SetAsFirstSibling();
        }

        /// <summary>Glowing outline + slight scale-up while selected (controller) or hovered (mouse).</summary>
        static void Highlight(Selectable s, float radius, Color ringColor)
        {
            const float pad = 8f;
            var rt = Stretch(UI("SelectRing", s.transform), -pad, -pad, -pad, -pad);
            var ring = Img(rt, ringColor, false, RingSprite);
            ring.type = Image.Type.Sliced;
            ring.pixelsPerUnitMultiplier = BaseRadius / (radius + pad);
            ring.enabled = false;
            var h = s.gameObject.AddComponent<SelectionHighlight>();
            S(h, "ring", ring);
        }

        static TextMeshProUGUI Txt(Transform parent, string name, string text, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Center, bool bold = false, bool outline = false)
        {
            var rt = UI(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = Font;
            if (outline && OutlineFontMaterial != null) t.fontSharedMaterial = OutlineFontMaterial;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            return t;
        }

        /// <summary>Rounded button with a selection ring. Radius = half the height for a pill.</summary>
        static Button Btn(Transform parent, string name, string label, Color bg, Color fg, float fontSize, out TextMeshProUGUI labelText,
            float radius, Color? ring = null)
        {
            var rt = UI(name, parent);
            var img = Panel(rt, bg, radius, true);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var cb = b.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(0.95f, 0.95f, 0.95f);
            cb.selectedColor = Color.white;                       // the SelectionHighlight ring shows selection
            cb.pressedColor = new Color(0.82f, 0.82f, 0.82f);
            cb.disabledColor = Color.white;                       // booking cards show picked/rejected colors when disabled
            cb.fadeDuration = 0.06f;
            b.colors = cb;
            labelText = Txt(rt, "Label", label, fontSize, fg, TextAlignmentOptions.Center, true);
            Stretch(labelText.rectTransform, 12, 12, 4, 4);
            Highlight(b, radius, ring ?? Gold);
            return b;
        }

        /// <summary>Rounded progress bar: a masked pill track with a Filled image inside (fillAmount keeps working).</summary>
        static Image Bar(Transform parent, string name, Color bg, Color fill, float radius, out Image fillImage,
            Image.FillMethod method = Image.FillMethod.Horizontal, int origin = 0)
        {
            var rt = UI(name, parent);
            var back = Panel(rt, bg, radius);
            rt.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var f = UI("Fill", rt);
            Stretch(f);
            fillImage = Img(f, fill);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = method;
            fillImage.fillOrigin = origin;
            fillImage.fillAmount = 1f;
            return back;
        }

        /// <summary>Rounded pill with a centered label inside (signs, tags).</summary>
        static TextMeshProUGUI Pill(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color bg, string text,
            float fontSize, Color fg)
        {
            var rt = Place(UI(name, parent), anchor, Center, pos, size);
            Panel(rt, bg, size.y * 0.5f);
            var t = Txt(rt, "Text", text, fontSize, fg, TextAlignmentOptions.Center, true);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            Stretch(t.rectTransform, 8f, 8f, 2f, 2f);
            return t;
        }

        /// <summary>Pill that hugs a label another script toggles (toast / prompt); placed just before the label.</summary>
        static void LabelBackdrop(TMP_Text label, Color bg, Vector2 padding)
        {
            var rt = UI(label.name + "Backdrop", label.transform.parent);
            rt.SetSiblingIndex(label.transform.GetSiblingIndex());
            Place(rt, Center, Center, Vector2.zero, new Vector2(100f, 60f));
            var img = Img(rt, bg, false, RoundSprite);
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f; // radius 32 suits ~64 px tall labels
            var bd = rt.gameObject.AddComponent<TextBackdrop>();
            S(bd, "text", label);
            S(bd, "padding", padding);
        }

        static RectTransform ViewRoot(string name) => Stretch(UI(name, null));

        static GameObject UiPrefab(string file, System.Func<RectTransform, GameObject> build) =>
            Prefab($"{UiDir}/{file}.prefab", () => build(ViewRoot(file)));

        // ================================================================== Views

        static void BuildUiPrefabs()
        {
            GenerateUiSprites(false);
            var hud = BuildHud();
            var board = BuildFlightBoard();
            var phone = BuildPhone();
            var menu = BuildMainMenu();
            var flight = BuildFlightTransition();
            var missed = BuildMissed();
            var win = BuildWin();
            var debug = BuildDebugPanel();

            UiRootPrefab = Prefab($"{UiDir}/PF_UI_Root.prefab", () =>
            {
                var go = new GameObject("PF_UI_Root", typeof(RectTransform));
                go.layer = LayerMask.NameToLayer("UI");
                var canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = go.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
                go.AddComponent<GraphicRaycaster>();
                var root = go.AddComponent<UIRoot>();
                root.canvas = canvas;
                root.hud = Nested(hud, go.transform).GetComponent<HudView>();
                root.flightBoard = Nested(board, go.transform).GetComponent<FlightBoardView>();
                root.phone = Nested(phone, go.transform).GetComponent<PhoneView>();
                root.mainMenu = Nested(menu, go.transform).GetComponent<MainMenuView>();
                root.flightTransition = Nested(flight, go.transform).GetComponent<FlightTransitionView>();
                root.missed = Nested(missed, go.transform).GetComponent<MissedView>();
                root.win = Nested(win, go.transform).GetComponent<WinView>();
                root.debugPanel = Nested(debug, go.transform).GetComponent<DebugPanel>();
                return go;
            });
        }

        static GameObject BuildHud() => UiPrefab("PF_UI_HUD", root =>
        {
            var view = root.gameObject.AddComponent<HudView>();
            var stats = Stretch(UI("StatsGroup", root));

            // Visa card (top left)
            var tl = Place(UI("TopLeft", stats), TopLeft, TopLeft, new Vector2(24f, -24f), new Vector2(520f, 214f));
            Panel(tl, Navy, 22f);
            Shadow(tl, 22f, 0.35f);
            var country = Txt(tl, "Country", "COUNTRY", 34, Color.white, TextAlignmentOptions.Left, true);
            Place(country.rectTransform, TopLeft, TopLeft, new Vector2(22f, -14f), new Vector2(360f, 44f));
            Pill(tl, "VisaTag", TopRight, new Vector2(-70f, -34f), new Vector2(104f, 36f), Gold, "VISA", 20, Navy);
            var visa = Txt(tl, "Visa", "Visa left: 0:30", 26, Hex("#FFD98A"), TextAlignmentOptions.Left);
            Place(visa.rectTransform, TopLeft, TopLeft, new Vector2(22f, -60f), new Vector2(480f, 34f));
            var visaBar = Bar(tl, "VisaBar", new Color(1f, 1f, 1f, 0.16f), Gold, 7f, out var visaFill);
            Place(visaBar.rectTransform, TopLeft, TopLeft, new Vector2(22f, -100f), new Vector2(476f, 14f));
            var res = Txt(tl, "Residency", "Residency: 0.0/3 yrs", 24, Color.white, TextAlignmentOptions.Left);
            Place(res.rectTransform, TopLeft, TopLeft, new Vector2(22f, -124f), new Vector2(480f, 34f));
            var photos = Txt(tl, "Photos", "Photos here: 0/3", 24, Hex("#C9D6E6"), TextAlignmentOptions.Left);
            Place(photos.rectTransform, TopLeft, TopLeft, new Vector2(22f, -162f), new Vector2(480f, 34f));

            // Carry-on card (bottom left)
            var bl = Place(UI("BottomLeft", stats), BottomLeft, BottomLeft, new Vector2(24f, 24f), new Vector2(800f, 132f));
            Panel(bl, Navy, 22f);
            Shadow(bl, 22f, 0.35f);
            var item = Txt(bl, "Item", "Item: (empty)", 28, Color.white, TextAlignmentOptions.Left, true);
            Place(item.rectTransform, TopLeft, TopLeft, new Vector2(22f, -12f), new Vector2(760f, 40f));
            var status = Txt(bl, "Status", "", 24, TealLight, TextAlignmentOptions.Left, true);
            Place(status.rectTransform, TopLeft, TopLeft, new Vector2(22f, -52f), new Vector2(760f, 34f));
            var miles = Txt(bl, "Miles", "Miles: 0", 22, Hex("#C9D6E6"), TextAlignmentOptions.Left);
            Place(miles.rectTransform, TopLeft, TopLeft, new Vector2(22f, -90f), new Vector2(760f, 32f));

            var prompt = Txt(root, "FestivalPrompt", "[F] Photograph!", 42, Gold, TextAlignmentOptions.Center, true);
            prompt.textWrappingMode = TextWrappingModes.NoWrap;
            Place(prompt.rectTransform, Center, Center, new Vector2(0f, 150f), new Vector2(1400f, 80f));
            LabelBackdrop(prompt, NavyDeep, new Vector2(40f, 16f));
            var toast = Txt(root, "Toast", "Toast", 30, Color.white, TextAlignmentOptions.Center, true);
            Place(toast.rectTransform, TopCenter, TopCenter, new Vector2(0f, -206f), new Vector2(1400f, 60f));
            LabelBackdrop(toast, Teal, new Vector2(36f, 14f));
            var hint = Txt(root, "DebugHint", "[F1] Debug", 20, Rgb(1f, 1f, 1f, 0.6f), TextAlignmentOptions.Right);
            Place(hint.rectTransform, BottomRight, BottomRight, new Vector2(-20f, 12f), new Vector2(400f, 30f));
            hint.gameObject.AddComponent<InputPromptLabel>().template = "{Debug} Debug";
            var flash = Img(Stretch(UI("PhotoFlash", root)), Rgb(1f, 1f, 1f, 0f)); // full-screen camera flash

            S(view, "statsGroup", stats.gameObject);
            S(view, "countryText", country);
            S(view, "visaText", visa);
            S(view, "visaFill", visaFill);
            S(view, "residencyText", res);
            S(view, "photosText", photos);
            S(view, "milesText", miles);
            S(view, "itemText", item);
            S(view, "statusText", status);
            S(view, "promptText", prompt);
            S(view, "toastText", toast);
            S(view, "photoFlash", flash);
            return root.gameObject;
        });

        static GameObject BuildFlightBoard() => UiPrefab("PF_UI_FlightBoard", root =>
        {
            var view = root.gameObject.AddComponent<FlightBoardView>();
            var board = Place(UI("Board", root), TopCenter, TopCenter, new Vector2(-70f, -14f), new Vector2(660f, 186f));
            var bg = Panel(board, NavyDeep, 24f);
            Shadow(board, 24f, 0.4f);
            var title = Txt(board, "Title", "FLIGHT TO ...", 26, Gold, TextAlignmentOptions.Center, true);
            title.characterSpacing = 6f;
            Place(title.rectTransform, TopCenter, TopCenter, new Vector2(0f, -10f), new Vector2(620f, 36f));
            var count = Txt(board, "Countdown", "Gate closes 0:42", 44, Color.white, TextAlignmentOptions.Center, true);
            Place(count.rectTransform, TopCenter, TopCenter, new Vector2(0f, -46f), new Vector2(620f, 52f));
            var dist = Txt(board, "Distance", "", 21, Hex("#AFC1D6"), TextAlignmentOptions.Center);
            Place(dist.rectTransform, TopCenter, TopCenter, new Vector2(0f, -98f), new Vector2(620f, 30f));
            var track = Place(UI("Track", board), BottomCenter, BottomCenter, new Vector2(-40f, 24f), new Vector2(480f, 8f));
            Panel(track, Rgb(1f, 1f, 1f, 0.22f), 4f);
            Pill(board, "Takeoff", BottomRight, new Vector2(-66f, 28f), new Vector2(112f, 28f), Coral, "TAKEOFF", 14, Color.white);
            var plane = Place(UI("PlaneIcon", track), MidLeft, Center, new Vector2(0f, 17f), new Vector2(30f, 30f));
            Img(plane, Color.white, false, PlaneSprite);
            var runner = Place(UI("RunnerIcon", track), MidLeft, Center, new Vector2(0f, -14f), new Vector2(16f, 30f));
            Img(runner, TealLight, false, CapsuleSprite);

            S(view, "board", board.gameObject);
            S(view, "titleText", title);
            S(view, "countdownText", count);
            S(view, "distanceText", dist);
            S(view, "track", track);
            S(view, "planeIcon", plane);
            S(view, "runnerIcon", runner);
            S(view, "background", bg);
            S(view, "normalColor", NavyDeep);
            S(view, "urgentColor", new Color(0.55f, 0.11f, 0.15f, 1f));
            return root.gameObject;
        });

        // ------------------------------------------------------------------ Phone apps

        static GameObject BuildBookingApp() => UiPrefab("PF_UI_BookingApp", root =>
        {
            var app = root.gameObject.AddComponent<BookingApp>();
            var cards = new BookingCardView[3];
            for (int i = 0; i < 3; i++)
            {
                // Boarding pass: country stripes, details, perforation, stub with the plane pictogram.
                var b = Btn(root, "Card" + i, "", Color.white, Ink, 10, out var unusedLabel, 20f);
                Object.DestroyImmediate(unusedLabel.gameObject);
                var rt = Place((RectTransform)b.transform, TopCenter, TopCenter, new Vector2(0f, -18f - i * 212f), new Vector2(566f, 194f));
                Shadow(rt, 20f, 0.3f);
                var card = rt.gameObject.AddComponent<BookingCardView>();
                var p1 = Place(UI("SwatchPrimary", rt), MidLeft, MidLeft, new Vector2(14f, 0f), new Vector2(16f, 164f));
                var p2 = Place(UI("SwatchAccent", rt), MidLeft, MidLeft, new Vector2(34f, 0f), new Vector2(16f, 164f));
                var name = Txt(rt, "Name", "COUNTRY", 34, Ink, TextAlignmentOptions.Left, true);
                Place(name.rectTransform, TopLeft, TopLeft, new Vector2(66f, -14f), new Vector2(340f, 46f));
                var visa = Txt(rt, "Visa", "Visa: 5 years (5 min)", 24, Teal, TextAlignmentOptions.Left, true);
                Place(visa.rectTransform, TopLeft, TopLeft, new Vector2(66f, -64f), new Vector2(340f, 36f));
                var info = Txt(rt, "Info", "Difficulty *", 20, Muted, TextAlignmentOptions.TopLeft);
                Place(info.rectTransform, TopLeft, TopLeft, new Vector2(66f, -108f), new Vector2(340f, 74f));
                var perf = Place(UI("Perforation", rt), MidLeft, Center, new Vector2(424f, 0f), new Vector2(6f, 148f));
                var dash = Img(perf, Hex("#C9CED6"), false, DashSprite);
                dash.type = Image.Type.Tiled;
                foreach (var y in new[] { 1f, 0f })
                {
                    var notch = Place(UI(y > 0f ? "NotchTop" : "NotchBottom", rt), new Vector2(0f, y), Center, new Vector2(424f, 0f), new Vector2(28f, 28f));
                    Img(notch, ScreenBg, false, DotSprite);
                }
                var icon = Place(UI("StubPlane", rt), MidLeft, Center, new Vector2(497f, 18f), new Vector2(60f, 60f));
                Img(icon, Teal, false, PlaneSprite);
                var stub = Txt(rt, "StubText", "BOARDING", 15, Muted, TextAlignmentOptions.Center, true);
                stub.characterSpacing = 4f;
                Place(stub.rectTransform, MidLeft, Center, new Vector2(497f, -34f), new Vector2(130f, 24f));
                S(card, "button", b);
                S(card, "nameText", name);
                S(card, "visaText", visa);
                S(card, "infoText", info);
                S(card, "swatchPrimary", Panel(p1, Color.white, 8f));
                S(card, "swatchAccent", Panel(p2, Color.red, 8f));
                S(card, "background", b.targetGraphic);
                S(card, "normalColor", Color.white);
                S(card, "pickedColor", Mint);
                S(card, "rejectedColor", Hex("#D9DEE5"));
                rt.Find("SelectRing").SetAsLastSibling();
                cards[i] = card;
            }
            var hint = Txt(root, "Hint", "Pick fast!", 24, Ink, TextAlignmentOptions.Center, true);
            Place(hint.rectTransform, BottomCenter, BottomCenter, new Vector2(0f, 18f), new Vector2(580f, 56f));
            S(app, "cards", cards);
            S(app, "hintText", hint);
            return root.gameObject;
        });

        static GameObject BuildPhotoCard() => Prefab($"{UiDir}/PF_UI_PhotoCard.prefab", () =>
        {
            // Polaroid
            var rt = UI("PF_UI_PhotoCard", null);
            rt.sizeDelta = new Vector2(170f, 124f);
            Panel(rt, Color.white, 10f, true);
            Shadow(rt, 10f, 0.35f);
            rt.gameObject.AddComponent<CanvasGroup>();
            var item = rt.gameObject.AddComponent<PhotoDragItem>();
            var photo = Stretch(UI("Photo", rt), 8f, 8f, 8f, 32f);
            Panel(photo, Hex("#7FB8E6"), 6f);
            photo.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var sun = Place(UI("Sun", photo), TopRight, TopRight, new Vector2(-10f, -8f), new Vector2(22f, 22f));
            Img(sun, Hex("#FFD166"), false, DotSprite);
            var crowd = Place(UI("Crowd", photo), BottomCenter, BottomCenter, new Vector2(0f, -8f), new Vector2(154f, 34f));
            Panel(crowd, Hex("#E76F51"), 10f);
            var cap = Txt(rt, "Caption", "Festival", 16, Ink, TextAlignmentOptions.Center, true);
            cap.textWrappingMode = TextWrappingModes.NoWrap;
            cap.overflowMode = TextOverflowModes.Truncate; // LiberationSans has no ellipsis glyph
            Place(cap.rectTransform, BottomCenter, BottomCenter, new Vector2(0f, 2f), new Vector2(160f, 28f));
            S(item, "caption", cap);
            return rt.gameObject;
        });

        static GameObject BuildResidencyApp(GameObject photoCard) => UiPrefab("PF_UI_ResidencyApp", root =>
        {
            var app = root.gameObject.AddComponent<ResidencyApp>();

            // Mash phase: the application form
            var mash = Stretch(UI("MashGroup", root));
            var paper = Place(UI("Form", mash), TopCenter, TopCenter, new Vector2(0f, -18f), new Vector2(520f, 480f));
            Panel(paper, Paper, 18f);
            Shadow(paper, 18f, 0.3f);
            var band = TopBand(UI("HeaderBand", paper), 0f, 58f);
            Panel(band, Teal, 18f);
            var fh = Txt(band, "Header", "RESIDENCY APPLICATION", 24, Color.white, TextAlignmentOptions.Center, true);
            fh.characterSpacing = 3f;
            Stretch(fh.rectTransform, 10f, 10f);
            for (int i = 0; i < 9; i++)
            {
                var line = Place(UI("Line" + i, paper), TopCenter, TopCenter, new Vector2(0f, -96f - i * 40f), new Vector2(440f, 6f));
                Panel(line, Hex("#E5DAC4"), 3f);
            }
            var inkArea = Stretch(UI("InkArea", paper), 30f, 30f, 76f, 24f);
            Panel(inkArea, Color.white, 12f); // invisible rounded mask so the ink fill has rounded corners
            inkArea.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var inkRt = Stretch(UI("Ink", inkArea));
            var ink = Img(inkRt, new Color(Navy.r, Navy.g, Navy.b, 0.28f));
            ink.type = Image.Type.Filled;
            ink.fillMethod = Image.FillMethod.Vertical;
            ink.fillOrigin = (int)Image.OriginVertical.Top;
            ink.fillAmount = 0f;
            var seal = Place(UI("Seal", paper), BottomRight, Center, new Vector2(-70f, 64f), new Vector2(92f, 92f));
            var sealImg = Img(seal, new Color(Teal.r, Teal.g, Teal.b, 0.35f), false, RingSprite);
            sealImg.type = Image.Type.Sliced;
            sealImg.pixelsPerUnitMultiplier = BaseRadius / 46f;
            var mashText = Txt(mash, "MashText", "Mash!", 26, Ink, TextAlignmentOptions.Center, true);
            Place(mashText.rectTransform, BottomCenter, BottomCenter, new Vector2(0f, 112f), new Vector2(580f, 150f));
            var mashBtn = Btn(mash, "MashButton", "FILL IN!", Teal, Color.white, 32, out _, 42f);
            Place((RectTransform)mashBtn.transform, BottomCenter, BottomCenter, new Vector2(0f, 22f), new Vector2(320f, 84f));

            // Photo phase: tray of polaroids -> proof-of-integration paper
            var ph = Stretch(UI("PhotoGroup", root));
            var trayLabel = Txt(ph, "TrayLabel", "YOUR PHOTOS", 18, Muted, TextAlignmentOptions.Center, true);
            Place(trayLabel.rectTransform, TopLeft, TopLeft, new Vector2(16f, -8f), new Vector2(200f, 30f));
            var tray = Place(UI("Tray", ph), TopLeft, TopLeft, new Vector2(16f, -40f), new Vector2(200f, 640f));
            Panel(tray, Mist, 18f, true);
            var vlg = tray.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(15, 15, 12, 12);
            vlg.spacing = 10f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = vlg.childControlHeight = false;
            vlg.childForceExpandWidth = vlg.childForceExpandHeight = false;
            var form = Place(UI("Paper", ph), TopRight, TopRight, new Vector2(-16f, -8f), new Vector2(360f, 672f));
            Panel(form, Paper, 18f, true);
            Shadow(form, 18f, 0.3f);
            var proof = Txt(form, "Header", "PROOF OF INTEGRATION", 20, Teal, TextAlignmentOptions.Center, true);
            proof.characterSpacing = 2f;
            Place(proof.rectTransform, TopCenter, TopCenter, new Vector2(0f, -12f), new Vector2(340f, 34f));
            var slots = new RectTransform[3];
            for (int i = 0; i < 3; i++)
            {
                slots[i] = Place(UI("Slot" + i, form), TopCenter, TopCenter, new Vector2(0f, -60f - i * 198f), new Vector2(200f, 150f));
                var frame = Img(slots[i], Hex("#CDBF9F"), false, RingSprite);
                frame.type = Image.Type.Sliced;
                frame.pixelsPerUnitMultiplier = BaseRadius / 14f;
                var st = Txt(slots[i], "Hint", "photo " + (i + 1), 20, new Color(Muted.r, Muted.g, Muted.b, 0.55f));
                Stretch(st.rectTransform);
            }
            var photoText = Txt(ph, "PhotoText", "Attach 3 photos", 22, Ink, TextAlignmentOptions.Center, true);
            Place(photoText.rectTransform, BottomCenter, BottomCenter, new Vector2(0f, 4f), new Vector2(600f, 80f));

            // Passport stamp (color + tilt set by ResidencyApp); a hollow rounded frame hugs and follows the ink.
            var stamp = Txt(root, "Stamp", "APPROVED", 64, Color.red, TextAlignmentOptions.Center, true, true);
            Place(stamp.rectTransform, Center, Center, Vector2.zero, new Vector2(580f, 260f));
            var stampFrame = Place(UI("StampFrame", stamp.transform), Center, Center, Vector2.zero, new Vector2(300f, 100f));
            var sf = Img(stampFrame, Color.red, false, RingSprite);
            sf.type = Image.Type.Sliced;
            sf.pixelsPerUnitMultiplier = BaseRadius / 26f;
            var sfb = stampFrame.gameObject.AddComponent<TextBackdrop>();
            S(sfb, "text", stamp);
            S(sfb, "padding", new Vector2(28f, 16f));
            var sfc = stampFrame.gameObject.AddComponent<MatchGraphicColor>();
            S(sfc, "source", stamp);
            S(sfc, "alpha", 0.9f);

            S(app, "mashGroup", mash.gameObject);
            S(app, "mashFill", ink);
            S(app, "mashText", mashText);
            S(app, "mashButton", mashBtn);
            S(app, "photoGroup", ph.gameObject);
            S(app, "tray", tray);
            S(app, "paper", form);
            S(app, "paperSlots", slots);
            S(app, "photoPrefab", photoCard.GetComponent<PhotoDragItem>());
            S(app, "photoText", photoText);
            S(app, "stampText", stamp);
            return root.gameObject;
        });

        static GameObject BuildShopApp() => UiPrefab("PF_UI_ShopApp", root =>
        {
            var app = root.gameObject.AddComponent<ShopApp>();
            var items = new ShopItemView[3];
            for (int i = 0; i < 3; i++)
            {
                // Duty-free price tag
                var b = Btn(root, "Item" + i, "", Color.white, Ink, 10, out var unused, 20f);
                Object.DestroyImmediate(unused.gameObject);
                var rt = Place((RectTransform)b.transform, TopCenter, TopCenter, new Vector2(0f, -16f - i * 178f), new Vector2(566f, 160f));
                Shadow(rt, 20f, 0.3f);
                var view = rt.gameObject.AddComponent<ShopItemView>();
                var hole = Place(UI("TagHole", rt), TopLeft, Center, new Vector2(20f, -20f), new Vector2(14f, 14f));
                Img(hole, Hex("#D5DBE3"), false, DotSprite);
                var sw = Place(UI("Swatch", rt), MidLeft, MidLeft, new Vector2(30f, 0f), new Vector2(88f, 88f));
                var name = Txt(rt, "Name", "Buff", 28, Ink, TextAlignmentOptions.Left, true);
                Place(name.rectTransform, TopLeft, TopLeft, new Vector2(134f, -14f), new Vector2(270f, 40f));
                var desc = Txt(rt, "Desc", "Description", 19, Muted, TextAlignmentOptions.TopLeft);
                Place(desc.rectTransform, TopLeft, TopLeft, new Vector2(134f, -56f), new Vector2(270f, 96f));
                var tag = Place(UI("PriceTag", rt), MidRight, MidRight, new Vector2(-18f, 0f), new Vector2(132f, 56f));
                Panel(tag, Sand, 28f);
                var cost = Txt(tag, "Cost", "100 mi", 26, Hex("#1B5E20"), TextAlignmentOptions.Center, true);
                Stretch(cost.rectTransform, 6f, 6f);
                S(view, "button", b);
                S(view, "nameText", name);
                S(view, "descText", desc);
                S(view, "costText", cost);
                S(view, "swatch", Img(sw, Color.white, false, DotSprite));
                rt.Find("SelectRing").SetAsLastSibling();
                items[i] = view;
            }
            var skip = Btn(root, "Skip", "SKIP", Mist, Ink, 26, out _, 32f);
            Place((RectTransform)skip.transform, BottomCenter, BottomCenter, new Vector2(0f, 104f), new Vector2(300f, 64f));
            var miles = Txt(root, "Miles", "Miles: 0", 28, Ink, TextAlignmentOptions.Center, true);
            Place(miles.rectTransform, BottomCenter, BottomCenter, new Vector2(0f, 54f), new Vector2(580f, 44f));
            var info = Txt(root, "Info", "One item per trip", 20, Muted, TextAlignmentOptions.Center);
            Place(info.rectTransform, BottomCenter, BottomCenter, new Vector2(0f, 16f), new Vector2(580f, 40f));
            S(app, "items", items);
            S(app, "skipButton", skip);
            S(app, "milesText", miles);
            S(app, "infoText", info);
            return root.gameObject;
        });

        static GameObject BuildPhone()
        {
            var booking = BuildBookingApp();
            var card = BuildPhotoCard();
            var residency = BuildResidencyApp(card);
            var shop = BuildShopApp();

            return UiPrefab("PF_UI_Phone", root =>
            {
                var view = root.gameObject.AddComponent<PhoneView>();
                var frame = Place(UI("Frame", root), MidRight, MidRight, new Vector2(760f, 0f), new Vector2(640f, 940f));
                Panel(frame, Hex("#0F1319"), 64f, true);
                Shadow(frame, 40f, 0.55f);
                var screen = Stretch(UI("Screen", frame), 16f, 16f, 16f, 16f);
                Panel(screen, ScreenBg, 50f);
                screen.gameObject.AddComponent<Mask>().showMaskGraphic = true;

                // Status bar with a notch
                var status = TopBand(UI("StatusBar", screen), 0f, 36f);
                var time = Txt(status, "Time", "12:00", 18, Ink, TextAlignmentOptions.Left, true);
                Stretch(time.rectTransform, 40f, 40f);
                var net = Txt(status, "Network", "UAE 5G  100%", 18, Ink, TextAlignmentOptions.Right, true);
                Stretch(net.rectTransform, 40f, 40f);
                var notch = Place(UI("Notch", status), TopCenter, TopCenter, new Vector2(0f, -6f), new Vector2(150f, 28f));
                Panel(notch, Hex("#0F1319"), 14f);

                var header = TopBand(UI("Header", screen), 36f, 72f);
                Img(header, Teal); // full-width app bar, clipped to the rounded screen
                var title = Txt(header, "Title", "App", 30, Color.white, TextAlignmentOptions.Center, true);
                Stretch(title.rectTransform, 10f, 10f);
                var timerBg = Bar(screen, "Timer", Hex("#DCE2EA"), Teal, 10f, out var timerFill);
                TopBand(timerBg.rectTransform, 120f, 20f, 20f, 116f);
                var timerText = Txt(screen, "TimerText", "5.0s", 24, Ink, TextAlignmentOptions.Right, true);
                Place(timerText.rectTransform, TopRight, TopRight, new Vector2(-18f, -112f), new Vector2(92f, 36f));
                var content = Stretch(UI("Content", screen), 0f, 0f, 150f, 0f);
                var b = Nested(booking, content).GetComponent<BookingApp>();
                var r = Nested(residency, content).GetComponent<ResidencyApp>();
                var s = Nested(shop, content).GetComponent<ShopApp>();
                var waiting = Txt(content, "Waiting", "Please wait...", 30, Muted, TextAlignmentOptions.Center, true);
                Stretch(waiting.rectTransform, 30f, 30f);
                var homeBar = Place(UI("HomeIndicator", screen), BottomCenter, BottomCenter, new Vector2(0f, 8f), new Vector2(150f, 6f));
                Panel(homeBar, new Color(Ink.r, Ink.g, Ink.b, 0.35f), 3f);
                S(b, "phone", view);
                S(r, "phone", view);
                S(s, "phone", view);
                view.booking = b;
                view.residency = r;
                view.shop = s;
                S(view, "frame", frame);
                S(view, "titleText", title);
                S(view, "timerText", timerText);
                S(view, "timerFill", timerFill);
                S(view, "waitingText", waiting);
                S(view, "timerColor", Teal);
                S(view, "timerUrgentColor", Coral);
                b.gameObject.SetActive(false);
                r.gameObject.SetActive(false);
                s.gameObject.SetActive(false);
                return root.gameObject;
            });
        }

        // ------------------------------------------------------------------ Full-screen views

        static GameObject BuildMainMenu() => UiPrefab("PF_UI_MainMenu", root =>
        {
            var view = root.gameObject.AddComponent<MainMenuView>();
            var panel = Stretch(UI("Panel", root));
            Img(panel, new Color(NavyDeep.r, NavyDeep.g, NavyDeep.b, 0.35f)); // full-screen dim, lets the terminal show

            // Departure-board card
            var board = Place(UI("Board", panel), Center, Center, new Vector2(0f, 60f), new Vector2(1320f, 700f));
            Panel(board, Navy, 40f);
            Shadow(board, 40f, 0.5f);
            var title = Txt(board, "Title", "PASSPORT FRENZY", 112, Color.white, TextAlignmentOptions.Center, true);
            title.characterSpacing = 4f;
            Place(title.rectTransform, TopCenter, TopCenter, new Vector2(0f, -40f), new Vector2(1260f, 140f));
            var subPill = Place(UI("SubtitleSign", board), TopCenter, TopCenter, new Vector2(0f, -192f), new Vector2(940f, 60f));
            Panel(subPill, Teal, 30f);
            var sub = Txt(subPill, "Subtitle", "Festival of Passports  -  Home airport: Dubai, UAE", 28, Color.white, TextAlignmentOptions.Center, true);
            Stretch(sub.rectTransform, 20f, 20f);
            var tag = Txt(board, "Tagline", "Collect all 10 residencies. Snap festival photos. Survive menu hell. DON'T MISS YOUR FLIGHT.", 26, Hex("#D8E1EC"), TextAlignmentOptions.Center);
            Place(tag.rectTransform, TopCenter, TopCenter, new Vector2(0f, -272f), new Vector2(1240f, 44f));
            var play = Btn(board, "Play", "PLAY", Gold, Navy, 56, out var playLabel, 60f, Color.white);
            Place((RectTransform)play.transform, TopCenter, TopCenter, new Vector2(0f, -340f), new Vector2(460f, 120f));
            var playIcon = Place(UI("Icon", play.transform), MidLeft, Center, new Vector2(78f, 0f), new Vector2(64f, 64f));
            Img(playIcon, Navy, false, PlaneSprite);
            playLabel.rectTransform.offsetMin = new Vector2(70f, 4f);
            play.transform.Find("SelectRing").SetAsLastSibling();
            var passport = Place(UI("PassportCard", board), BottomCenter, BottomCenter, new Vector2(0f, 34f), new Vector2(1180f, 170f));
            Panel(passport, Paper, 24f);
            var stats = Txt(passport, "Stats", "Passports 0/10", 30, Ink, TextAlignmentOptions.Center);
            Stretch(stats.rectTransform, 24f, 24f, 12f, 12f);

            var controlsPill = Place(UI("ControlsBar", panel), BottomCenter, BottomCenter, new Vector2(0f, 34f), new Vector2(1640f, 58f));
            Panel(controlsPill, NavyDeep, 29f);
            var controls = Txt(controlsPill, "Controls", "[Space] Jump", 23, Hex("#E3EAF3"), TextAlignmentOptions.Center);
            Stretch(controls.rectTransform, 24f, 24f);
            S(view, "panel", panel.gameObject);
            S(view, "playButton", play);
            S(view, "statsText", stats);
            S(view, "controlsText", controls);
            return root.gameObject;
        });

        /// <summary>Airport-signage plane pictogram (the views animate its anchoredPosition).</summary>
        static RectTransform MiniPlane(Transform parent, string name, float size, Color color)
        {
            var plane = Place(UI(name, parent), Center, Center, Vector2.zero, new Vector2(size, size));
            Img(plane, color, false, PlaneSprite);
            return plane;
        }

        static GameObject BuildFlightTransition() => UiPrefab("PF_UI_FlightTransition", root =>
        {
            var view = root.gameObject.AddComponent<FlightTransitionView>();
            var panel = Stretch(UI("Panel", root));
            Img(panel, Hex("#6FB1E8"), true); // full-screen sky
            var rng = new System.Random(3);
            for (int i = 0; i < 9; i++)
            {
                var c = Place(UI("Cloud" + i, panel), Center, Center,
                    new Vector2(-860f + (float)rng.NextDouble() * 1720f, -440f + (float)rng.NextDouble() * 880f),
                    new Vector2(200f + (float)rng.NextDouble() * 200f, 80f + (float)rng.NextDouble() * 50f));
                Img(c, Rgb(1f, 1f, 1f, 0.8f), false, CircleSprite);
            }
            var plane = MiniPlane(panel, "Plane", 240f, Color.white);
            var titleSign = Place(UI("TitleSign", panel), Center, Center, new Vector2(0f, 236f), new Vector2(1300f, 116f));
            Panel(titleSign, NavyDeep, 58f);
            var title = Txt(titleSign, "Title", "UAE  >>>  JAPAN", 60, Color.white, TextAlignmentOptions.Center, true);
            Stretch(title.rectTransform, 30f, 30f);
            var milesPill = Place(UI("MilesPill", panel), Center, Center, new Vector2(0f, -236f), new Vector2(900f, 84f));
            Panel(milesPill, Gold, 42f);
            var miles = Txt(milesPill, "Miles", "+100 miles", 40, Navy, TextAlignmentOptions.Center, true);
            Stretch(miles.rectTransform, 20f, 20f);
            S(view, "panel", panel.gameObject);
            S(view, "titleText", title);
            S(view, "milesText", miles);
            S(view, "plane", plane);
            panel.gameObject.SetActive(false);
            return root.gameObject;
        });

        static GameObject BuildMissed() => UiPrefab("PF_UI_FlightMissed", root =>
        {
            var view = root.gameObject.AddComponent<MissedView>();
            var panel = Stretch(UI("Panel", root));
            Img(panel, new Color(0.13f, 0.04f, 0.07f, 0.985f), true); // full-screen (near-opaque: linear blending washes out lower alphas)
            var plane = MiniPlane(panel, "Plane", 170f, Coral);
            var title = Txt(panel, "Title", "FLIGHT MISSED!", 104, Coral, TextAlignmentOptions.Center, true);
            title.characterSpacing = 3f;
            Place(title.rectTransform, Center, Center, new Vector2(0f, 250f), new Vector2(1700f, 140f));
            var card = Place(UI("Card", panel), Center, Center, new Vector2(0f, -100f), new Vector2(1300f, 420f));
            Panel(card, NavyDeep, 32f);
            Shadow(card, 32f, 0.5f);
            Pill(card, "GateClosed", TopCenter, new Vector2(0f, 0f), new Vector2(300f, 54f), Coral, "GATE CLOSED", 24, Color.white);
            var body = Txt(card, "Body", "", 30, Color.white, TextAlignmentOptions.Center);
            Stretch(body.rectTransform, 40f, 40f, 44f, 24f);
            S(view, "panel", panel.gameObject);
            S(view, "titleText", title);
            S(view, "bodyText", body);
            S(view, "plane", plane);
            panel.gameObject.SetActive(false);
            return root.gameObject;
        });

        static GameObject BuildWin() => UiPrefab("PF_UI_Win", root =>
        {
            var view = root.gameObject.AddComponent<WinView>();
            var panel = Stretch(UI("Panel", root));
            Img(panel, NavyDeep, true); // full-screen
            var title = Txt(panel, "Title", "PASSPORT COLLECTOR!", 96, Gold, TextAlignmentOptions.Center, true);
            title.characterSpacing = 3f;
            Place(title.rectTransform, TopCenter, TopCenter, new Vector2(0f, -64f), new Vector2(1700f, 130f));
            var sub = Txt(panel, "Subtitle", "Every residency collected. Welcome, citizen of the world.", 34, Color.white, TextAlignmentOptions.Center);
            Place(sub.rectTransform, TopCenter, TopCenter, new Vector2(0f, -194f), new Vector2(1700f, 56f));
            var card = Place(UI("PassportCard", panel), Center, Center, new Vector2(0f, -20f), new Vector2(1100f, 380f));
            Panel(card, Paper, 32f);
            Shadow(card, 32f, 0.5f);
            Pill(card, "StampTag", TopCenter, new Vector2(0f, 0f), new Vector2(280f, 50f), Teal, "ALL STAMPS", 22, Color.white);
            var stats = Txt(card, "Stats", "", 32, Ink, TextAlignmentOptions.Center, true);
            Stretch(stats.rectTransform, 40f, 40f, 40f, 20f);
            var home = Btn(panel, "Home", "HOME AIRPORT", Gold, Navy, 32, out _, 48f, Color.white);
            Place((RectTransform)home.transform, Center, Center, new Vector2(-240f, -330f), new Vector2(420f, 96f));
            var reset = Btn(panel, "Reset", "NEW COLLECTION", Coral, Color.white, 32, out _, 48f, Color.white);
            Place((RectTransform)reset.transform, Center, Center, new Vector2(240f, -330f), new Vector2(420f, 96f));
            S(view, "panel", panel.gameObject);
            S(view, "statsText", stats);
            S(view, "homeButton", home);
            S(view, "resetButton", reset);
            panel.gameObject.SetActive(false);
            return root.gameObject;
        });

        static GameObject BuildDebugPanel() => UiPrefab("PF_UI_DebugPanel", root =>
        {
            var view = root.gameObject.AddComponent<DebugPanel>();
            var panel = Place(UI("Panel", root), MidLeft, MidLeft, new Vector2(20f, 0f), new Vector2(580f, 990f));
            Panel(panel, NavyDeep, 26f, true);
            var head = Txt(panel, "Header", "DEBUG  (F1 / Select)", 26, Gold, TextAlignmentOptions.Center, true);
            Place(head.rectTransform, TopCenter, TopCenter, new Vector2(0f, -12f), new Vector2(560f, 40f));
            var info = Txt(panel, "Info", "", 17, Color.white, TextAlignmentOptions.TopLeft);
            Place(info.rectTransform, TopCenter, TopCenter, new Vector2(0f, -56f), new Vector2(540f, 150f));
            var list = Place(UI("Buttons", panel), TopCenter, TopCenter, new Vector2(0f, -214f), new Vector2(530f, 760f));
            var vlg = list.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10f;
            vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            Button B(string field, string label, Color bg)
            {
                var b = Btn(list, "Btn_" + field, label, bg, Color.white, 22, out _, 29f);
                ((RectTransform)b.transform).sizeDelta = new Vector2(530f, 58f);
                S(view, field, b);
                return b;
            }
            B("skipTime", "Skip 30 s (visa + residency)", Slate);
            B("addYear", "+1 year in this country", Slate);
            B("grantMiles", "+500 miles", Slate);
            B("spawnFestival", "Spawn festival ahead", Slate);
            B("forceAirport", "Force airport transition", Slate);
            B("grantPhotos", "+3 photos (this country)", Slate);
            B("toggleDemo", "Toggle Jam Demo Mode", Teal);
            B("win", "WIN (grant all passports)", Teal);
            B("clearSave", "Clear save", Coral);
            B("close", "Close", Hex("#46566F"));
            S(view, "panel", panel.gameObject);
            S(view, "infoText", info);
            return root.gameObject;
        });
    }
}
