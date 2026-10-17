namespace PassportFrenzy
{
    /// <summary>Wall: the runner stops dead until the player jumps. Keep it lower than the jump apex (~2.4 units).</summary>
    public class BlockObstacle : ObstacleBase
    {
        protected override bool ApplyHit(RunnerController runner) => runner.TryHit(HitKind.Block);
    }
}
