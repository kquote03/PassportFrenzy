using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace PassportFrenzy
{
    /// <summary>
    /// Owns the PassportFrenzyControls asset. The UI map is always on (overlay menus, mouse, debug toggle).
    /// The Gameplay map (move/jump/photo/item) is switched off only for full-screen overlays (<see cref="SetMenuMode"/>).
    /// The phone is used while running (<see cref="SetPhoneMode"/>): the character auto-runs (GameFlow), Move and Jump are
    /// switched off, and the UI module's navigate/submit are swapped to the Phone map, so the movement keys / left stick /
    /// D-pad pick options and Space / Enter / A confirm (right stick + RT and the mouse still work).
    /// </summary>
    public class InputRouter : MonoBehaviour
    {
        [SerializeField] InputActionAsset actions;

        InputActionMap _gameplay, _ui, _phone;
        InputSystemUIInputModule _module;
        InputActionReference _uiMove, _uiSubmit, _phoneMove, _phoneSubmit;

        public InputAction Move { get; private set; }
        public InputAction Jump { get; private set; }
        public InputAction Photo { get; private set; }
        public InputAction UseItem { get; private set; }
        public InputAction Navigate { get; private set; }
        public InputAction Submit { get; private set; }
        public InputAction Mash { get; private set; }
        public InputAction ToggleDebug { get; private set; }
        public InputAction PhoneNavigate { get; private set; }
        public InputAction PhoneSubmit { get; private set; }
        public InputAction PhoneMash { get; private set; }

        public bool MenuMode { get; private set; }
        public bool PhoneMode { get; private set; }
        public InputActionAsset Asset => actions;

        void Awake()
        {
            _gameplay = actions.FindActionMap("Gameplay", true);
            _ui = actions.FindActionMap("UI", true);
            _phone = actions.FindActionMap("Phone", true);
            Move = _gameplay.FindAction("Move", true);
            Jump = _gameplay.FindAction("Jump", true);
            Photo = _gameplay.FindAction("Photo", true);
            UseItem = _gameplay.FindAction("UseItem", true);
            Navigate = _ui.FindAction("Navigate", true);
            Submit = _ui.FindAction("Submit", true);
            Mash = _ui.FindAction("Mash", true);
            ToggleDebug = _ui.FindAction("ToggleDebug", true);
            PhoneNavigate = _phone.FindAction("Navigate", true);
            PhoneSubmit = _phone.FindAction("Submit", true);
            PhoneMash = _phone.FindAction("Mash", true);
            _ui.Enable();
            _gameplay.Enable();
            _phone.Enable();
        }

        void Start()
        {
            // The EventSystem lives under GameRoot (sibling of Systems).
            _module = GetComponentInParent<GameRoot>()?.GetComponentInChildren<InputSystemUIInputModule>(true);
            if (_module == null && EventSystem.current != null) _module = EventSystem.current.GetComponent<InputSystemUIInputModule>();
            if (_module == null) { Debug.LogWarning("[InputRouter] No InputSystemUIInputModule found; phone mode can't swap UI actions."); return; }
            _uiMove = _module.move;
            _uiSubmit = _module.submit;
            _phoneMove = InputActionReference.Create(PhoneNavigate);
            _phoneSubmit = InputActionReference.Create(PhoneSubmit);
        }

        /// <summary>Move and Jump share keys with the phone, so they are off while it is up.</summary>
        void ApplyPhoneLock()
        {
            if (MenuMode) return; // the whole Gameplay map is off anyway
            if (PhoneMode) { Move.Disable(); Jump.Disable(); }
            else { Move.Enable(); Jump.Enable(); }
        }

        void OnDestroy()
        {
            _gameplay?.Disable();
            _ui?.Disable();
            _phone?.Disable();
        }

        /// <summary>Menu mode (full-screen overlays): move/jump/photo/item disabled; only UI controls work.</summary>
        public void SetMenuMode(bool on)
        {
            MenuMode = on;
            if (on) _gameplay.Disable();
            else
            {
                _gameplay.Enable();
                ApplyPhoneLock();
            }
        }

        /// <summary>
        /// Phone mode: the phone takes the movement controls (move keys / left stick / D-pad navigate, Space / Enter / A
        /// confirm and mash) while the character auto-runs. Photo and Use item stay live.
        /// </summary>
        public void SetPhoneMode(bool on)
        {
            PhoneMode = on;
            if (on) SetMenuMode(false);
            ApplyPhoneLock();
            if (_module == null) return;
            _module.move = on ? _phoneMove : _uiMove;
            _module.submit = on ? _phoneSubmit : _uiSubmit;
        }
    }
}
