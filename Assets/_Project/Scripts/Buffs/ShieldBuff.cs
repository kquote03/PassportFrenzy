using UnityEngine;

namespace PassportFrenzy
{
    /// <summary>Passive bubble: absorbs the next Slow/Block hit, then breaks. Crowds are not blocked.</summary>
    [CreateAssetMenu(menuName = "Passport Frenzy/Buffs/Shield", fileName = "Buff_Shield")]
    public class ShieldBuff : BuffDefinition
    {
        void Reset() => isPassive = true;

        public override bool CanUse(in BuffContext ctx, out string reason)
        {
            bool ok = ctx.runner != null && !ctx.runner.HasShield;
            reason = ok ? null : "Shield already up";
            return ok;
        }

        public override void Apply(in BuffContext ctx)
        {
            ctx.runner.SetShield(true);
            GameEvents.RaiseToast("Shield up!");
        }
    }
}
