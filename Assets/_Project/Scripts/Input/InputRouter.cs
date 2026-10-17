using UnityEngine;
using UnityEngine.InputSystem;

namespace PassportFrenzy
{
    /// <summary>
    /// Owns the PassportFrenzyControls asset. The UI map is always on (menus, mouse, debug toggle).
    /// The Gameplay map (jump/photo/item) is switched off while any phone menu is open.
    /// </summary>
    public class InputRouter : MonoBehaviour
    {
        [SerializeField] InputActionAsset actions;

        InputActionMap _gameplay, _ui;

        public InputAction Move { get; private set; }
        public InputAction Jump { get; private set; }
        public InputAction Photo { get; private set; }
        public InputAction UseItem { get; private set; }
        public InputAction Navigate { get; private set; }
        public InputAction Submit { get; private set; }
        public InputAction Mash { get; private set; }
        public InputAction ToggleDebug { get; private set; }

        public bool MenuMode { get; private set; }
        public InputActionAsset Asset => actions;

        void Awake()
        {
            _gameplay = actions.FindActionMap("Gameplay", true);
            _ui = actions.FindActionMap("UI", true);
            Move = _gameplay.FindAction("Move", true);
            Jump = _gameplay.FindAction("Jump", true);
            Photo = _gameplay.FindAction("Photo", true);
            UseItem = _gameplay.FindAction("UseItem", true);
            Navigate = _ui.FindAction("Navigate", true);
            Submit = _ui.FindAction("Submit", true);
            Mash = _ui.FindAction("Mash", true);
            ToggleDebug = _ui.FindAction("ToggleDebug", true);
            _ui.Enable();
            _gameplay.Enable();
        }

        void OnDestroy()
        {
            _gameplay?.Disable();
            _ui?.Disable();
        }

        /// <summary>Menu mode: jump/photo/item disabled; only UI controls work.</summary>
        public void SetMenuMode(bool on)
        {
            MenuMode = on;
            if (on) _gameplay.Disable();
            else _gameplay.Enable();
        }
    }
}
