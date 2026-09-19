using System.Numerics;

namespace ResetyKile.Core;

public static class MathUtil
{
    public static float Approach(float v, float target, float maxDelta)
    {
        if (v < target) return MathF.Min(v + maxDelta, target);
        if (v > target) return MathF.Max(v - maxDelta, target);
        return v;
    }

    public static Vector2 Approach(Vector2 v, Vector2 target, float maxDelta)
    {
        return new Vector2(
            Approach(v.X, target.X, maxDelta),
            Approach(v.Y, target.Y, maxDelta));
    }

    public static float ExpLerp(float a, float b, float speed, float dt)
    {
        float t = 1f - MathF.Exp(-speed * dt);
        return a + (b - a) * t;
    }

    public static Vector2 ExpLerp(Vector2 a, Vector2 b, float speed, float dt)
    {
        return new Vector2(
            ExpLerp(a.X, b.X, speed, dt),
            ExpLerp(a.Y, b.Y, speed, dt));
    }

    public static float Clamp(float v, float min, float max)
        => v < min ? min : (v > max ? max : v);
}