using System.Collections.Generic;
using UnityEngine;

namespace PassportFrenzy
{
    /// <summary>
    /// Service locator on the PF_GameRoot prefab. The only object in Game.unity (besides the camera).
    /// Everything else reaches shared systems and data through <see cref="I"/>.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class GameRoot : MonoBehaviour
    {
        public static GameRoot I { get; private set; }

        [Header("Data")]
        public GameTuning tuning;
        public CountryDatabase countries;
        public SoundLibrary sounds;
        [Tooltip("Buffs sold in the freezone shop.")]
        public List<BuffDefinition> shopBuffs = new List<BuffDefinition>();
        [Tooltip("Buffs that can appear as in-run pickups.")]
        public List<BuffDefinition> pickupBuffs = new List<BuffDefinition>();

        [Header("Systems (on this prefab)")]
        public GameFlow flow;
        public ChunkSpawner spawner;
        public InputRouter input;
        public AudioManager audioManager;
        public BuffSystem buffs;
        public UIRoot ui;
        [Tooltip("Country parallax backdrop (optional).")]
        public ParallaxBackground background;

        [Header("Player (nested prefab)")]
        public RunnerController player;

        public PlayerCamera Cam { get; private set; }

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            tuning.ClearRuntimeOverrides();
            SaveSystem.Load();
            Cam = FindAnyObjectByType<PlayerCamera>();
        }

        void OnDestroy()
        {
            if (I == this) I = null;
        }
    }
}
