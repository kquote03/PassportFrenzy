using UnityEngine;

namespace PassportFrenzy
{
    [CreateAssetMenu(menuName = "Passport Frenzy/Buffs/Flight Delay", fileName = "Buff_FlightDelay")]
    public class FlightDelayBuff : BuffDefinition
    {
        public float addSeconds = 60f;

        public override bool CanUse(in BuffContext ctx, out string reason)
        {
            bool ok = ctx.flow.State == GameState.Chase && ctx.run != null && ctx.run.FlightTimerActive;
            reason = ok ? null : "Flight Delay only works during the airport chase";
            return ok;
        }

        public override void Apply(in BuffContext ctx)
        {
            ctx.run.FlightSecondsRemaining += addSeconds;
            ctx.run.FlightSecondsTotal += addSeconds;
            GameEvents.RaiseToast($"Flight delayed +{Mathf.RoundToInt(addSeconds)}s!");
        }
    }
}
