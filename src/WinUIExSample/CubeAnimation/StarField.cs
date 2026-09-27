using System;
using System.Numerics;

namespace WinUIExSample.CubeAnimation;

internal static class StarField
{
    public const int Count = 220;
    public readonly record struct Star(Vector2 Position, float Depth, float Radius, float Opacity);

    public static Star Sample(int index, float time)
    {
        float speed = 0.24f + Frac(index * 0.137f) * 0.12f;
        float phase = Frac(index * 0.381966f - time * speed / 6.6f);
        float depth = 1.4f + phase * 6.6f;
        float worldX = (Frac(index * 0.75487766f) - 0.5f) * 18;
        float worldY = (Frac(index * 0.56984029f) - 0.5f) * 12;
        float cameraX = MathF.Sin(time * 0.09f) * 0.9f;
        float cameraY = MathF.Sin(time * 0.065f) * 0.28f;
        Vector2 position = new Vector2(800, 470) +
            new Vector2(worldX - cameraX, worldY - cameraY) * (700 / depth);
        float proximity = 1 - phase;
        float away = Math.Clamp(Vector2.Distance(position, new Vector2(800, 470)) / 450 - 0.7f, 0, 1);
        float edge = Math.Clamp(Math.Min(Math.Min(position.X, 1600 - position.X),
            Math.Min(position.Y, 1000 - position.Y)) / 35, 0, 1);
        float recycle = Smooth(phase / 0.08f) * Smooth((1 - phase) / 0.12f);
        float opacity = (0.36f + proximity * 0.6f) * away * edge * recycle;
        return new Star(position, depth, 0.5f + 3.7f / depth, opacity);
    }

    private static float Frac(float value) => value - MathF.Floor(value);

    private static float Smooth(float value)
    {
        value = Math.Clamp(value, 0, 1);
        return value * value * (3 - 2 * value);
    }
}
