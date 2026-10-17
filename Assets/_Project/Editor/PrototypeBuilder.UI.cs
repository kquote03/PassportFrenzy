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

        static readonly Color Ink = Hex("#1F2430");
        static readonly Color Amber = Hex("#FCA311");
        static readonly Color PhoneBlue = Hex("#1E88E5");
        static readonly Color Paper = Hex("#FFF8E7");

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

        static Button Btn(Transform parent, string name, string label, Color bg, Color fg, float fontSize, out TextMeshProUGUI labelText)
        {
            var rt = UI(name, parent);
            var img = Img(rt, bg, true);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var cb = b.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(0.85f, 0.93f, 1f);
            cb.selectedColor = new Color(1f, 0.85f, 0.35f);
            cb.pressedColor = new Color(0.7f, 0.7f, 0.7f);
            cb.disabledColor = new Color(0.8f, 0.8f, 0.8f);
            cb.fadeDuration = 0.05f;
            b.colors = cb;
            labelText = Txt(rt, "Label", label, fontSize, fg, TextAlignmentOptions.Center, true);
            Stretch(labelText.rectTransform, 8, 8, 4, 4);
            return b;
        }

        static Image Bar(Transform parent, string name, Color bg, Color fill, out Image fillImage,
            Image.FillMethod method = Image.FillMethod.Horizontal, int origin = 0)
        {
            var rt = UI(name, parent);
            var back = Img(rt, bg);
            var f = UI("Fill", rt);
            Stretch(f);
            fillImage = Img(f, fill);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = method;
            fillImage.fillOrigin = origin;
            fillImage.fillAmount = 1f;
            return back;
        }

        static RectTransform ViewRoot(string name) => Stretch(UI(name, null));

        static GameObject UiPrefab(string file, System.Func<RectTransform, GameObject> build) =>
            Prefab($"{UiDir}/{file}.prefab", () => build(ViewRoot(file)));

        // ================================================================== Views

        static void BuildUiPrefabs()
        {
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

            var tl = Place(UI("TopLeft", stats), TopLeft, TopLeft, new Vector2(24f, -24f), new Vector2(660f, 210f));
            Img(tl, Rgb(0f, 0f, 0f, 0.5f));
            var country = Txt(tl, "Country", "COUNTRY", 36, Color.white, TextAlignmentOptions.Left, true);
            Place(country.rectTransform, TopLeft, TopLeft, new Vector2(18f, -10f), new Vector2(620f, 46f));
            var visa = Txt(tl, "Visa", "Visa left: 0:30", 28, Hex("#FFE08A"), TextAlignmentOptions.Left);
            Place(visa.rectTransform, TopLeft, TopLeft, new Vector2(18f, -58f), new Vector2(620f, 36f));
            var visaBar = Bar(tl, "VisaBar", Rgb(1f, 1f, 1f, 0.2f), Hex("#FFD166"), out var visaFill);
            Place(visaBar.rectTransform, TopLeft, TopLeft, new Vector2(18f, -98f), new Vector2(620f, 12f));
            var res = Txt(tl, "Residency", "Residency: 0.0/3 yrs", 26, Color.white, TextAlignmentOptions.Left);
            Place(res.rectTransform, TopLeft, TopLeft, new Vector2(18f, -118f), new Vector2(620f, 36f));
            var photos = Txt(tl, "Photos", "Photos here: 0/3", 26, Color.white, TextAlignmentOptions.Left);
            Place(photos.rectTransform, TopLeft, TopLeft, new Vector2(18f, -158f), new Vector2(620f, 36f));

            var bl = Place(UI("BottomLeft", stats), BottomLeft, BottomLeft, new Vector2(24f, 24f), new Vector2(820f, 132f));
            Img(bl, Rgb(0f, 0f, 0f, 0.5f));
            var item = Txt(bl, "Item", "Item: (empty)", 30, Color.white, TextAlignmentOptions.Left, true);
            Place(item.rectTransform, TopLeft, TopLeft, new Vector2(18f, -10f), new Vector2(780f, 40f));
            var status = Txt(bl, "Status", "", 26, Hex("#7FDBFF"), TextAlignmentOptions.Left, true);
            Place(status.rectTransform, TopLeft, TopLeft, new Vector2(18f, -50f), new Vector2(780f, 36f));
            var miles = Txt(bl, "Miles", "Miles: 0", 24, Hex("#DDDDDD"), TextAlignmentOptions.Left);
            Place(miles.rectTransform, TopLeft, TopLeft, new Vector2(18f, -88f), new Vector2(780f, 34f));

            var prompt = Txt(root, "FestivalPrompt", "[F] Photograph!", 46, Hex("#FFE600"), TextAlignmentOptions.Center, true, true);
            Place(prompt.rectTransform, Center, Center, new Vector2(0f, 150f), new Vector2(1400f, 80f));
            var toast = Txt(root, "Toast", "Toast", 34, Color.white, TextAlignmentOptions.Center, true, true);
            Place(toast.rectTransform, TopCenter, TopCenter, new Vector2(0f, -196f), new Vector2(1400f, 60f));
            var hint = Txt(root, "DebugHint", "[F1] Debug", 20, Rgb(1f, 1f, 1f, 0.6f), TextAlignmentOptions.Right);
            Place(hint.rectTransform, BottomRight, BottomRight, new Vector2(-20f, 12f), new Vector2(400f, 30f));
            hint.gameObject.AddComponent<InputPromptLabel>().template = "{Debug} Debug";
            var flash = Img(Stretch(UI("PhotoFlash", root)), Rgb(1f, 1f, 1f, 0f));

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
            var board = Place(UI("Board", root), TopCenter, TopCenter, new Vector2(0f, -14f), new Vector2(800f, 166f));
            var bg = Img(board, Rgb(0.08f, 0.1f, 0.18f, 0.92f));
            var title = Txt(board, "Title", "FLIGHT TO ...", 28, Amber, TextAlignmentOptions.Center, true);
            Place(title.rectTransform, TopCenter, TopCenter, new Vector2(0f, -8f), new Vector2(760f, 38f));
            var count = Txt(board, "Countdown", "Gate closes 0:42", 44, Color.white, TextAlignmentOptions.Center, true);
            Place(count.rectTransform, TopCenter, TopCenter, new Vector2(0f, -44f), new Vector2(760f, 52f));
            var dist = Txt(board, "Distance", "", 22, Hex("#B0BEC5"), TextAlignmentOptions.Center);
            Place(dist.rectTransform, TopCenter, TopCenter, new Vector2(0f, -96f), new Vector2(760f, 30f));
            var track = Place(UI("Track", board), BottomCenter, BottomCenter, new Vector2(-30f, 20f), new Vector2(660f, 8f));
            Img(track, Rgb(1f, 1f, 1f, 0.3f));
            var takeoff = Txt(board, "Takeoff", "TAKEOFF", 16, Hex("#FF8A80"), TextAlignmentOptions.Left, true);
            Place(takeoff.rectTransform, BottomRight, BottomRight, new Vector2(-6f, 12f), new Vector2(80f, 24f));
            var plane = Place(UI("PlaneIcon", track), MidLeft, Center, new Vector2(0f, 16f), new Vector2(40f, 40f));
            Img(plane, Color.white, false, TriSprite);
            plane.localRotation = Quaternion.Euler(0f, 0f, -90f);
            var runner = Place(UI("RunnerIcon", track), MidLeft, Center, new Vector2(0f, -14f), new Vector2(16f, 30f));
            Img(runner, Hex("#2F80ED"), false, CapsuleSprite);

            S(view, "board", board.gameObject);
            S(view, "titleText", title);
            S(view, "countdownText", count);
            S(view, "distanceText", dist);
            S(view, "track", track);
            S(view, "planeIcon", plane);
            S(view, "runnerIcon", runner);
            S(view, "background", bg);
            return root.gameObject;
        });

        // ------------------------------------------------------------------ Phone apps

        static GameObject BuildBookingApp() => UiPrefab("PF_UI_BookingApp", root =>
        {
            var app = root.gameObject.AddComponent<BookingApp>();
            var cards = new BookingCardView[3];
            for (int i = 0; i < 3; i++)
            {
                var b = Btn(root, "Card" + i, "", Color.white, Ink, 10, out var unusedLabel);
                Object.DestroyImmediate(unusedLabel.gameObject);
                var rt = Place((RectTransform)b.transform, TopCenter, TopCenter, new Vector2(0f, -18f - i * 214f), new Vector2(570f, 200f));
                var card = rt.gameObject.AddComponent<BookingCardView>();
                var p1 = Place(UI("SwatchPrimary", rt), MidLeft, MidLeft, new Vector2(12f, 0f), new Vector2(22f, 172f));
                var p2 = Place(UI("SwatchAccent", rt), MidLeft, MidLeft, new Vector2(38f, 0f), new Vector2(22f, 172f));
                var name = Txt(rt, "Name", "COUNTRY", 38, Ink, TextAlignmentOptions.Left, true);
                Place(name.rectTransform, TopLeft, TopLeft, new Vector2(76f, -12f), new Vector2(480f, 50f));
                var visa = Txt(rt, "Visa", "Visa: 5 years (5 min)", 28, Hex("#1565C0"), TextAlignmentOptions.Left, true);
                Place(visa.rectTransform, TopLeft, TopLeft, new Vector2(76f, -68f), new Vector2(480f, 40f));
                var info = Txt(rt, "Info", "Difficulty *", 22, Hex("#5F6B7A"), TextAlignmentOptions.Left);
                Place(info.rectTransform, TopLeft, TopLeft, new Vector2(76f, -116f), new Vector2(480f, 64f));
                S(card, "button", b);
                S(card, "nameText", name);
                S(card, "visaText", visa);
                S(card, "infoText", info);
                S(card, "swatchPrimary", Img(p1, Color.white));
                S(card, "swatchAccent", Img(p2, Color.red));
                S(card, "background", b.targetGraphic);
                cards[i] = card;
            }
            var hint = Txt(root, "Hint", "Pick fast!", 26, Ink, TextAlignmentOptions.Center, true);
            Place(hint.rectTransform, BottomCenter, BottomCenter, new Vector2(0f, 14f), new Vector2(580f, 60f));
            S(app, "cards", cards);
            S(app, "hintText", hint);
            return root.gameObject;
        });

        static GameObject BuildPhotoCard() => Prefab($"{UiDir}/PF_UI_PhotoCard.prefab", () =>
        {
            var rt = UI("PF_UI_PhotoCard", null);
            rt.sizeDelta = new Vector2(170f, 120f);
            Img(rt, Color.white, true);
            rt.gameObject.AddComponent<CanvasGroup>();
            var item = rt.gameObject.AddComponent<PhotoDragItem>();
            var photo = Stretch(UI("Photo", rt), 8f, 8f, 8f, 32f);
            Img(photo, Hex("#7FB8E6"));
            var sun = Place(UI("Sun", photo), TopRight, TopRight, new Vector2(-10f, -8f), new Vector2(22f, 22f));
            Img(sun, Hex("#FFD166"), false, CircleSprite);
            var crowd = Place(UI("Crowd", photo), BottomCenter, BottomCenter, Vector2.zero, new Vector2(154f, 26f));
            Img(crowd, Hex("#E76F51"));
            var cap = Txt(rt, "Caption", "Festival", 16, Ink, TextAlignmentOptions.Center, true);
            cap.textWrappingMode = TextWrappingModes.NoWrap;
            cap.overflowMode = TextOverflowModes.Ellipsis;
            Place(cap.rectTransform, BottomCenter, BottomCenter, new Vector2(0f, 2f), new Vector2(160f, 28f));
            S(item, "caption", cap);
            return rt.gameObject;
        });

        static GameObject BuildResidencyApp(GameObject photoCard) => UiPrefab("PF_UI_ResidencyApp", root =>
        {
            var app = root.gameObject.AddComponent<ResidencyApp>();

            // Mash phase
            var mash = Stretch(UI("MashGroup", root));
            var paper = Place(UI("Form", mash), TopCenter, TopCenter, new Vector2(0f, -16f), new Vector2(520f, 500f));
            Img(paper, Paper);
            var fh = Txt(paper, "Header", "RESIDENCY APPLICATION", 28, Ink, TextAlignmentOptions.Center, true);
            Place(fh.rectTransform, TopCenter, TopCenter, new Vector2(0f, -14f), new Vector2(500f, 40f));
            for (int i = 0; i < 9; i++)
            {
                var line = Place(UI("Line" + i, paper), TopCenter, TopCenter, new Vector2(0f, -86f - i * 44f), new Vector2(440f, 5f));
                Img(line, Hex("#C9C2B0"));
            }
            var inkRt = Stretch(UI("Ink", paper), 30f, 30f, 70f, 24f);
            var ink = Img(inkRt, Rgb(0.15f, 0.3f, 0.75f, 0.35f));
            ink.type = Image.Type.Filled;
            ink.fillMethod = Image.FillMethod.Vertical;
            ink.fillOrigin = (int)Image.OriginVertical.Top;
            ink.fillAmount = 0f;
            var mashText = Txt(mash, "MashText", "Mash!", 28, Ink, TextAlignmentOptions.Center, true);
            Place(mashText.rectTransform, BottomCenter, BottomCenter, new Vector2(0f, 112f), new Vector2(580f, 150f));
            var mashBtn = Btn(mash, "MashButton", "FILL IN!", PhoneBlue, Color.white, 34, out _);
            Place((RectTransform)mashBtn.transform, BottomCenter, BottomCenter, new Vector2(0f, 20f), new Vector2(320f, 84f));

            // Photo phase
            var ph = Stretch(UI("PhotoGroup", root));
            var trayLabel = Txt(ph, "TrayLabel", "YOUR PHOTOS", 20, Ink, TextAlignmentOptions.Center, true);
            Place(trayLabel.rectTransform, TopLeft, TopLeft, new Vector2(16f, -8f), new Vector2(200f, 30f));
            var tray = Place(UI("Tray", ph), TopLeft, TopLeft, new Vector2(16f, -40f), new Vector2(200f, 640f));
            Img(tray, Hex("#DCE3EA"), true);
            var vlg = tray.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(15, 15, 12, 12);
            vlg.spacing = 10f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = vlg.childControlHeight = false;
            vlg.childForceExpandWidth = vlg.childForceExpandHeight = false;
            var form = Place(UI("Paper", ph), TopRight, TopRight, new Vector2(-16f, -8f), new Vector2(360f, 672f));
            Img(form, Paper, true);
            var proof = Txt(form, "Header", "PROOF OF INTEGRATION", 22, Ink, TextAlignmentOptions.Center, true);
            Place(proof.rectTransform, TopCenter, TopCenter, new Vector2(0f, -10f), new Vector2(340f, 34f));
            var slots = new RectTransform[3];
            for (int i = 0; i < 3; i++)
            {
                slots[i] = Place(UI("Slot" + i, form), TopCenter, TopCenter, new Vector2(0f, -60f - i * 198f), new Vector2(200f, 150f));
                Img(slots[i], Rgb(0.2f, 0.2f, 0.2f, 0.12f));
                var st = Txt(slots[i], "Hint", "photo " + (i + 1), 20, Rgb(0.2f, 0.2f, 0.2f, 0.4f));
                Stretch(st.rectTransform);
            }
            var photoText = Txt(ph, "PhotoText", "Attach 3 photos", 22, Ink, TextAlignmentOptions.Center, true);
            Place(photoText.rectTransform, BottomCenter, BottomCenter, new Vector2(0f, 4f), new Vector2(600f, 80f));

            var stamp = Txt(root, "Stamp", "APPROVED", 64, Color.red, TextAlignmentOptions.Center, true, true);
            Place(stamp.rectTransform, Center, Center, Vector2.zero, new Vector2(580f, 260f));

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
                var b = Btn(root, "Item" + i, "", Color.white, Ink, 10, out var unused);
                Object.DestroyImmediate(unused.gameObject);
                var rt = Place((RectTransform)b.transform, TopCenter, TopCenter, new Vector2(0f, -14f - i * 180f), new Vector2(574f, 166f));
                var view = rt.gameObject.AddComponent<ShopItemView>();
                var sw = Place(UI("Swatch", rt), MidLeft, MidLeft, new Vector2(16f, 0f), new Vector2(96f, 96f));
                var name = Txt(rt, "Name", "Buff", 30, Ink, TextAlignmentOptions.Left, true);
                Place(name.rectTransform, TopLeft, TopLeft, new Vector2(128f, -12f), new Vector2(300f, 42f));
                var desc = Txt(rt, "Desc", "Description", 19, Hex("#5F6B7A"), TextAlignmentOptions.TopLeft);
                Place(desc.rectTransform, TopLeft, TopLeft, new Vector2(128f, -56f), new Vector2(300f, 100f));
                var cost = Txt(rt, "Cost", "100 mi", 30, Hex("#1B5E20"), TextAlignmentOptions.Right, true);
                Place(cost.rectTransform, MidRight, MidRight, new Vector2(-16f, 0f), new Vector2(130f, 50f));
                S(view, "button", b);
                S(view, "nameText", name);
                S(view, "descText", desc);
                S(view, "costText", cost);
                S(view, "swatch", Img(sw, Color.white, false, CircleSprite));
                items[i] = view;
            }
            var skip = Btn(root, "Skip", "SKIP", Hex("#B0BEC5"), Ink, 30, out _);
            Place((RectTransform)skip.transform, BottomCenter, BottomCenter, new Vector2(0f, 104f), new Vector2(340f, 70f));
            var miles = Txt(root, "Miles", "Miles: 0", 30, Ink, TextAlignmentOptions.Center, true);
            Place(miles.rectTransform, BottomCenter, BottomCenter, new Vector2(0f, 54f), new Vector2(580f, 44f));
            var info = Txt(root, "Info", "One item per trip", 22, Hex("#5F6B7A"), TextAlignmentOptions.Center);
            Place(info.rectTransform, BottomCenter, BottomCenter, new Vector2(0f, 14f), new Vector2(580f, 40f));
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
                Img(frame, Hex("#15171C"), true);
                var screen = Stretch(UI("Screen", frame), 18f, 18f, 18f, 18f);
                Img(screen, Hex("#F5F7FB"));
                var status = TopBand(UI("StatusBar", screen), 0f, 34f);
                Img(status, Hex("#E3E7EE"));
                var st = Txt(status, "Text", "12:00              UAE Mobile  5G  100%", 18, Ink);
                Stretch(st.rectTransform, 12f, 12f);
                var header = TopBand(UI("Header", screen), 34f, 70f);
                Img(header, PhoneBlue);
                var title = Txt(header, "Title", "App", 30, Color.white, TextAlignmentOptions.Center, true);
                Stretch(title.rectTransform, 10f, 10f);
                var timerBg = Bar(screen, "Timer", Hex("#CFD8DC"), Hex("#33BFFF"), out var timerFill);
                TopBand(timerBg.rectTransform, 116f, 22f, 16f, 110f);
                var timerText = Txt(screen, "TimerText", "5.0s", 24, Ink, TextAlignmentOptions.Right, true);
                Place(timerText.rectTransform, TopRight, TopRight, new Vector2(-14f, -108f), new Vector2(92f, 36f));
                var content = Stretch(UI("Content", screen), 0f, 0f, 150f, 0f);
                var b = Nested(booking, content).GetComponent<BookingApp>();
                var r = Nested(residency, content).GetComponent<ResidencyApp>();
                var s = Nested(shop, content).GetComponent<ShopApp>();
                var waiting = Txt(content, "Waiting", "Please wait...", 32, Hex("#5F6B7A"), TextAlignmentOptions.Center, true);
                Stretch(waiting.rectTransform, 30f, 30f);
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
            Img(panel, Rgb(0f, 0f, 0f, 0.35f));
            var title = Txt(panel, "Title", "PASSPORT FRENZY", 124, Color.white, TextAlignmentOptions.Center, true, true);
            Place(title.rectTransform, TopCenter, TopCenter, new Vector2(0f, -90f), new Vector2(1700f, 160f));
            var sub = Txt(panel, "Subtitle", "Festival of Passports  -  Home airport: Dubai, UAE", 38, Amber, TextAlignmentOptions.Center, true, true);
            Place(sub.rectTransform, TopCenter, TopCenter, new Vector2(0f, -250f), new Vector2(1700f, 60f));
            var tag = Txt(panel, "Tagline", "Collect all 10 residencies. Snap festival photos. Survive menu hell. DON'T MISS YOUR FLIGHT.", 28, Color.white, TextAlignmentOptions.Center, false, true);
            Place(tag.rectTransform, TopCenter, TopCenter, new Vector2(0f, -320f), new Vector2(1700f, 50f));
            var play = Btn(panel, "Play", "PLAY", Amber, Ink, 60, out _);
            Place((RectTransform)play.transform, Center, Center, new Vector2(0f, -20f), new Vector2(460f, 124f));
            var stats = Txt(panel, "Stats", "Passports 0/10", 30, Color.white, TextAlignmentOptions.Center, false, true);
            Place(stats.rectTransform, Center, Center, new Vector2(0f, -190f), new Vector2(1700f, 130f));
            var controls = Txt(panel, "Controls", "[Space] Jump", 26, Hex("#E0E0E0"), TextAlignmentOptions.Center, false, true);
            Place(controls.rectTransform, BottomCenter, BottomCenter, new Vector2(0f, 40f), new Vector2(1700f, 50f));
            S(view, "panel", panel.gameObject);
            S(view, "playButton", play);
            S(view, "statsText", stats);
            S(view, "controlsText", controls);
            return root.gameObject;
        });

        static RectTransform MiniPlane(Transform parent, string name, Vector2 size, Color accent)
        {
            var plane = Place(UI(name, parent), Center, Center, Vector2.zero, size);
            var body = Stretch(UI("Body", plane), 0f, size.x * 0.1f, size.y * 0.3f, size.y * 0.3f);
            Img(body, Color.white);
            var nose = Place(UI("Nose", plane), MidRight, Center, new Vector2(-size.x * 0.1f, 0f), new Vector2(size.y * 0.4f, size.y * 0.4f));
            Img(nose, Color.white, false, CircleSprite);
            var tail = Place(UI("Tail", plane), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(0f, size.y * 0.15f), new Vector2(size.x * 0.15f, size.y * 0.55f));
            Img(tail, accent, false, TriSprite);
            var wing = Place(UI("Wing", plane), Center, Center, new Vector2(0f, -size.y * 0.1f), new Vector2(size.x * 0.35f, size.y * 0.12f));
            Img(wing, Hex("#C8CDD3"));
            var stripe = Place(UI("Stripe", plane), Center, Center, new Vector2(-size.x * 0.05f, size.y * 0.04f), new Vector2(size.x * 0.75f, size.y * 0.06f));
            Img(stripe, accent);
            return plane;
        }

        static GameObject BuildFlightTransition() => UiPrefab("PF_UI_FlightTransition", root =>
        {
            var view = root.gameObject.AddComponent<FlightTransitionView>();
            var panel = Stretch(UI("Panel", root));
            Img(panel, Hex("#6FB1E8"), true);
            var rng = new System.Random(3);
            for (int i = 0; i < 9; i++)
            {
                var c = Place(UI("Cloud" + i, panel), Center, Center,
                    new Vector2(-860f + (float)rng.NextDouble() * 1720f, -440f + (float)rng.NextDouble() * 880f),
                    new Vector2(200f + (float)rng.NextDouble() * 200f, 80f + (float)rng.NextDouble() * 50f));
                Img(c, Rgb(1f, 1f, 1f, 0.8f), false, CircleSprite);
            }
            var plane = MiniPlane(panel, "Plane", new Vector2(320f, 110f), Hex("#C1121F"));
            var title = Txt(panel, "Title", "UAE  >>>  JAPAN", 68, Color.white, TextAlignmentOptions.Center, true, true);
            Place(title.rectTransform, Center, Center, new Vector2(0f, 230f), new Vector2(1700f, 100f));
            var miles = Txt(panel, "Miles", "+100 miles", 46, Hex("#FFE08A"), TextAlignmentOptions.Center, true, true);
            Place(miles.rectTransform, Center, Center, new Vector2(0f, -230f), new Vector2(1700f, 70f));
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
            Img(panel, Rgb(0.17f, 0.04f, 0.04f, 0.93f), true);
            var plane = MiniPlane(panel, "Plane", new Vector2(240f, 80f), Hex("#FF5252"));
            var title = Txt(panel, "Title", "FLIGHT MISSED!", 112, Hex("#FF5252"), TextAlignmentOptions.Center, true, true);
            Place(title.rectTransform, Center, Center, new Vector2(0f, 230f), new Vector2(1700f, 140f));
            var body = Txt(panel, "Body", "", 32, Color.white, TextAlignmentOptions.Center);
            Place(body.rectTransform, Center, Center, new Vector2(0f, -90f), new Vector2(1400f, 380f));
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
            Img(panel, Rgb(0.04f, 0.11f, 0.23f, 0.96f), true);
            var title = Txt(panel, "Title", "PASSPORT COLLECTOR!", 100, Hex("#FFD166"), TextAlignmentOptions.Center, true, true);
            Place(title.rectTransform, TopCenter, TopCenter, new Vector2(0f, -90f), new Vector2(1700f, 130f));
            var sub = Txt(panel, "Subtitle", "Every residency collected. Welcome, citizen of the world.", 36, Color.white, TextAlignmentOptions.Center, false, true);
            Place(sub.rectTransform, TopCenter, TopCenter, new Vector2(0f, -230f), new Vector2(1700f, 60f));
            var stats = Txt(panel, "Stats", "", 36, Color.white, TextAlignmentOptions.Center);
            Place(stats.rectTransform, Center, Center, new Vector2(0f, 10f), new Vector2(1200f, 380f));
            var home = Btn(panel, "Home", "HOME AIRPORT", Amber, Ink, 36, out _);
            Place((RectTransform)home.transform, Center, Center, new Vector2(-240f, -330f), new Vector2(420f, 96f));
            var reset = Btn(panel, "Reset", "NEW COLLECTION", Hex("#E57373"), Ink, 36, out _);
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
            var panel = Place(UI("Panel", root), MidLeft, MidLeft, new Vector2(20f, 0f), new Vector2(580f, 980f));
            Img(panel, Rgb(0f, 0f, 0f, 0.86f), true);
            var head = Txt(panel, "Header", "DEBUG  (F1 / Select)", 28, Amber, TextAlignmentOptions.Center, true);
            Place(head.rectTransform, TopCenter, TopCenter, new Vector2(0f, -10f), new Vector2(560f, 40f));
            var info = Txt(panel, "Info", "", 17, Color.white, TextAlignmentOptions.TopLeft);
            Place(info.rectTransform, TopCenter, TopCenter, new Vector2(0f, -54f), new Vector2(550f, 150f));
            var list = Place(UI("Buttons", panel), TopCenter, TopCenter, new Vector2(0f, -212f), new Vector2(540f, 760f));
            var vlg = list.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            Button B(string field, string label)
            {
                var b = Btn(list, "Btn_" + field, label, Hex("#37474F"), Color.white, 24, out _);
                ((RectTransform)b.transform).sizeDelta = new Vector2(540f, 62f);
                S(view, field, b);
                return b;
            }
            B("skipTime", "Skip 30 s (visa + residency)");
            B("addYear", "+1 year in this country");
            B("grantMiles", "+500 miles");
            B("spawnFestival", "Spawn festival ahead");
            B("forceAirport", "Force airport transition");
            B("grantPhotos", "+3 photos (this country)");
            B("toggleDemo", "Toggle Jam Demo Mode");
            B("win", "WIN (grant all passports)");
            B("clearSave", "Clear save");
            B("close", "Close");
            S(view, "panel", panel.gameObject);
            S(view, "infoText", info);
            return root.gameObject;
        });
    }
}
