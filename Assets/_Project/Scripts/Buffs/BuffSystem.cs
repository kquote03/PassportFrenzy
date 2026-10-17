using UnityEngine;

namespace PassportFrenzy
{
    /// <summary>The single item slot + acquire/use rules. Lives on PF_GameRoot.</summary>
    public class BuffSystem : MonoBehaviour
    {
        RunState Run => GameRoot.I.flow.Run;

        public BuffDefinition Held => Run?.HeldItem;

        BuffContext Ctx => new BuffContext(GameRoot.I.flow, GameRoot.I.player, Run, GameRoot.I.tuning);

        public bool CanAcquire(BuffDefinition b, out string reason)
        {
            reason = null;
            if (Run == null || b == null) { reason = "Not in a run"; return false; }
            if (b.isPassive) return b.CanUse(Ctx, out reason);
            if (Held != null) { reason = $"Hands full! (holding {Held.displayName})"; return false; }
            return true;
        }

        public bool Acquire(BuffDefinition b)
        {
            if (!CanAcquire(b, out string reason))
            {
                GameEvents.RaiseToast(reason);
                return false;
            }
            if (b.isPassive) b.Apply(Ctx);
            else
            {
                Run.HeldItem = b;
                GameEvents.RaiseToast($"Got {b.displayName}! {InputPrompts.Format("{Use}")} to use");
            }
            GameEvents.RaiseItemChanged();
            return true;
        }

        public void UseHeld()
        {
            if (Held == null) return;
            if (!Held.CanUse(Ctx, out string reason))
            {
                GameEvents.RaiseToast(reason);
                return;
            }
            var item = Held;
            Run.HeldItem = null;
            item.Apply(Ctx);
            GameRoot.I.audioManager.Play(SoundIds.UseItem);
            GameEvents.RaiseItemChanged();
        }
    }
}
