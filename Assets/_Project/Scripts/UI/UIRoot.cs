using UnityEngine;
using UnityEngine.EventSystems;

namespace PassportFrenzy
{
    /// <summary>References to every UI view on the PF_UI_Root canvas prefab.</summary>
    public class UIRoot : MonoBehaviour
    {
        public Canvas canvas;
        public HudView hud;
        public FlightBoardView flightBoard;
        public PhoneView phone;
        public MainMenuView mainMenu;
        public FlightTransitionView flightTransition;
        public MissedView missed;
        public WinView win;
        public DebugPanel debugPanel;

        public void HideOverlays()
        {
            mainMenu.Hide();
            flightTransition.Hide();
            missed.Hide();
            win.Hide();
        }

        public static bool NothingSelected =>
            EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null;

        public static void Select(GameObject go)
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(go);
        }
    }
}
