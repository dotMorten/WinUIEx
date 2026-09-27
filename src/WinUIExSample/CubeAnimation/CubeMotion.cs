using System;
using System.Numerics;

namespace WinUIExSample.CubeAnimation;

internal static class CubeMotion
{
    public const double Duration = 5;

    public static double Angle(double time)
    {
        double remaining = 1 - Math.Clamp(time / Duration, 0, 1);
        return Math.Tau * 2 * remaining * remaining * remaining;
    }

    public static Matrix4x4 Rotation(double time)
    {
        float angle = (float)Angle(time);
        float tilt = MathF.Sin(angle * 0.5f);
        return Matrix4x4.CreateRotationY(angle) *
            Matrix4x4.CreateRotationX(tilt * 0.18f) *
            Matrix4x4.CreateRotationZ(tilt * 0.06f);
    }

    public static float SettlingProgress(double time)
    {
        float x = (float)Math.Clamp((time - (Duration - 0.75)) / 0.75, 0, 1);
        return x * x * (3 - 2 * x);
    }
}
