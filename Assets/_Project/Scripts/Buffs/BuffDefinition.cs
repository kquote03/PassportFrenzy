using UnityEngine;

namespace PassportFrenzy
{
    public readonly struct BuffContext
    {
        public readonly GameFlow flow;
        public readonly RunnerController runner;
        public readonly RunState run;
        public readonly GameTuning tuning;

        public BuffContext(GameFlow flow, RunnerController runner, RunState run, GameTuning tuning)
        {
            this.flow = flow;
            this.runner = runner;
            this.run = run;
            this.tuning = tuning;
        }
    }

    /// <summary>
    /// A buff (shop item and in-run pickup share the same assets). Subclass to add a new buff type.
    /// Passive buffs apply immediately on pickup/purchase and never occupy the item slot.
    /// </summary>
    public abstract class BuffDefinition : ScriptableObject
    {
        public string id = "buff";
        public string displayName = "Buff";
        [TextArea] public string description;
        [Min(0)] public int cost = 100;
        public Color color = Color.white;
        [Tooltip("Applied immediately on acquire instead of going into the item slot.")]
        public bool isPassive;

        /// <summary>Can it be used (or, if passive, acquired) right now?</summary>
        public virtual bool CanUse(in BuffContext ctx, out string reason)
        {
            reason = null;
            return true;
        }

        public abstract void Apply(in BuffContext ctx);
    }
}
