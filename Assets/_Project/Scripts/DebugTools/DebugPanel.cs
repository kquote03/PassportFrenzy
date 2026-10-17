using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PassportFrenzy
{
    /// <summary>F1 / Select. Cheats for testing and for keeping the live demo on rails.</summary>
    public class DebugPanel : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text infoText;
        [SerializeField] Button skipTime;
        [SerializeField] Button addYear;
        [SerializeField] Button grantMiles;
        [SerializeField] Button spawnFestival;
        [SerializeField] Button forceAirport;
        [SerializeField] Button grantPhotos;
        [SerializeField] Button toggleDemo;
        [SerializeField] Button win;
        [SerializeField] Button clearSave;
        [SerializeField] Button close;

        void Awake()
        {
            var f = (System.Func<GameFlow>)(() => GameRoot.I.flow);
            skipTime.onClick.AddListener(() => f().DebugSkipTime(30f));
            addYear.onClick.AddListener(() => f().DebugAddYear());
            grantMiles.onClick.AddListener(() => SaveSystem.AddMiles(500));
            spawnFestival.onClick.AddListener(() => GameRoot.I.spawner.SpawnFestivalAhead(12f));
            forceAirport.onClick.AddListener(() => f().DebugForceAirport());
            grantPhotos.onClick.AddListener(() => f().DebugGrantPhotos(3));
            toggleDemo.onClick.AddListener(() =>
            {
                var t = GameRoot.I.tuning;
                t.SetDemoOverride(!t.DemoMode);
                GameEvents.RaiseToast($"Jam Demo Mode {(t.DemoMode ? "ON" : "OFF")} (applies to new timers)");
            });
            win.onClick.AddListener(() => { panel.SetActive(false); f().DebugWin(); });
            clearSave.onClick.AddListener(() => { SaveSystem.Clear(); GameEvents.RaiseToast("Save cleared"); });
            close.onClick.AddListener(() => panel.SetActive(false));
            panel.SetActive(false);
        }

        void Update()
        {
            var root = GameRoot.I;
            if (root.input.ToggleDebug.WasPressedThisFrame())
            {
                panel.SetActive(!panel.activeSelf);
                if (panel.activeSelf && InputDeviceTracker.Current == ControlScheme.Gamepad) UIRoot.Select(skipTime.gameObject);
            }
            if (!panel.activeSelf) return;

            var run = root.flow.Run;
            var t = root.tuning;
            string info = $"State: {root.flow.State}   Demo: {(t.DemoMode ? "ON" : "OFF")}   1 yr = {t.SecondsPerYear:0}s\n" +
                          $"Device: {InputDeviceTracker.Current}   Save: {SaveSystem.FilePath}";
            if (run != null)
            {
                info += $"\nCountry: {run.Current.displayName}  years here: {run.YearsIn(run.Current.id, t.SecondsPerYear):0.0}  photos: {run.PhotoCount(run.Current.id)}";
                if (run.FlightTimerActive) info += $"\nFlight: {run.FlightSecondsRemaining:0.0}/{run.FlightSecondsTotal:0.0}s";
            }
            infoText.text = info;
        }
    }
}
