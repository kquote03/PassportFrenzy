using UnityEngine;

namespace PassportFrenzy
{
    /// <summary>
    /// Pure motion curves for the plane (no scene state, unit-tested). Poses are offsets from the parked position
    /// for a sprite whose nose points right; angle = nose-up degrees. Takeoff: taxi right, pitch up, climb out.
    /// FlyPast: an airborne plane crossing the screen right-to-left (the flight that left without you).
    /// </summary>
    public static class PlaneFlightCurve
    {
        public enum Kind { Takeoff, FlyPast }

        public struct Pose
        {
            public float dx, dy, angleDeg, scale;
        }

        public const float TaxiFraction = 0.35f;
        public const float TaxiDistance = 2.5f;
        public const float TakeoffDistance = 14f;
        public const float ClimbHeight = 7f;
        public const float PitchDegrees = 18f;

        /// <param name="t01">Normalized time 0..1.</param>
        /// <param name="flyPastDistance">Horizontal distance covered by a FlyPast (screen width + margins).</param>
        public static Pose Evaluate(Kind kind, float t01, float flyPastDistance = 34f)
        {
            float t = Mathf.Clamp01(t01);
            return kind == Kind.Takeoff ? Takeoff(t) : FlyPast(t, flyPastDistance);
        }

        static Pose Takeoff(float t)
        {
            if (t <= TaxiFraction)
            {
                float u = t / TaxiFraction;
                return new Pose { dx = TaxiDistance * u * u, dy = 0f, angleDeg = 0f, scale = 1f }; // ease-in roll
            }
            float v = (t - TaxiFraction) / (1f - TaxiFraction);
            return new Pose
            {
                dx = TaxiDistance + (TakeoffDistance - TaxiDistance) * v,
                dy = ClimbHeight * v * v,
                angleDeg = PitchDegrees * Mathf.Clamp01(v * 2.5f), // rotates up during the first 40% of the climb
                scale = 1f - 0.2f * v,
            };
        }

        static Pose FlyPast(float t, float distance)
        {
            return new Pose
            {
                dx = -distance * t,
                dy = 0.4f * Mathf.Sin(t * Mathf.PI) + 1.5f * t * t,
                angleDeg = 6f,
                scale = 1f - 0.1f * t,
            };
        }
    }
}
