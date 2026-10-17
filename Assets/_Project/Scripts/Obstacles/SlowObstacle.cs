using UnityEngine;

namespace PassportFrenzy
{
    /// <summary>Stumble: big speed loss, recovers over a couple of seconds.</summary>
    public class SlowObstacle : ObstacleBase
    {
        [Tooltip("Speed multiplier right after the hit. -1 = GameTuning default.")]
        [SerializeField] float stumbleMultiplier = -1f;
        [Tooltip("Recovery seconds. -1 = GameTuning default.")]
        [SerializeField] float recoverSeconds = -1f;

        protected override bool ApplyHit(RunnerController runner) =>
            runner.TryHit(HitKind.Slow, stumbleMultiplier, recoverSeconds);
    }
}
