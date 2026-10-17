using UnityEngine;

namespace PassportFrenzy
{
    /// <summary>Wall: the runner stops dead until the player jumps. Keep it lower than the jump apex (~2.4 units).</summary>
    public class BlockObstacle : ObstacleBase
    {
        RunnerController _blocked;

        protected override bool ApplyHit(RunnerController runner)
        {
            bool hit = runner.TryHit(HitKind.Block);
            if (hit && runner.Blocked) _blocked = runner;
            return hit;
        }

        // Free movement: backing out of the wall clears the block and re-arms the wall (you still have to jump it).
        void OnTriggerExit2D(Collider2D other)
        {
            if (_blocked == null || other.attachedRigidbody == null || other.attachedRigidbody.gameObject != _blocked.gameObject) return;
            if (_blocked.Blocked)
            {
                _blocked.ClearBlocked();
                Rearm();
            }
            _blocked = null;
        }
    }
}
