using UnityEngine;

namespace PassportFrenzy
{
    [CreateAssetMenu(menuName = "Passport Frenzy/Buffs/Speed Boost", fileName = "Buff_SpeedBoost")]
    public class SpeedBoostBuff : BuffDefinition
    {
        public float seconds = 10f;
        [Tooltip("-1 = GameTuning.speedBoostMultiplier")]
        public float multiplier = -1f;

        public override bool CanUse(in BuffContext ctx, out string reason)
        {
            bool ok = ctx.runner != null && ctx.runner.Running;
            reason = ok ? null : "Can't boost right now";
            return ok;
        }

        public override void Apply(in BuffContext ctx)
        {
            ctx.runner.StartBoost(seconds, multiplier > 0f ? multiplier : ctx.tuning.speedBoostMultiplier);
            GameEvents.RaiseToast($"Speed boost for {Mathf.RoundToInt(seconds)}s!");
        }
    }
}
