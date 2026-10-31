using System;

// Shared animation math; independent of Unity objects and relic identity.
internal static class OrbHUDAnimation
{
    public static float Progress(float remaining, float duration)
        => Clamp01(1f - remaining / Math.Max(0.01f, duration));

    public static float BurstScale(float progress, float from, float to)
    {
        float t = Clamp01(progress);
        return Math.Max(0.01f, from) + (Math.Max(0.01f, to) - Math.Max(0.01f, from)) * (1f - (1f - t) * (1f - t));
    }

    public static float BurstAlpha(float progress, float peak)
    {
        float t = Clamp01(progress);
        if (t <= 0f || t >= 1f) return 0f;
        peak = Math.Max(0.01f, Math.Min(0.99f, peak));
        return (float)(t <= peak ? Math.Sin(t / peak * Math.PI * 0.5)
            : Math.Cos((t - peak) / (1f - peak) * Math.PI * 0.5));
    }

    public static float PulseAlpha(float seconds, float period, float minimum, float maximum)
    {
        float low = Clamp01(Math.Min(minimum, maximum));
        float high = Clamp01(Math.Max(minimum, maximum));
        return low + (high - low) * (float)(0.5 - 0.5 * Math.Cos(seconds / Math.Max(0.01f, period) * Math.PI * 2));
    }

    public static int FrameIndex(float seconds, float framesPerSecond, int count)
        => count <= 0 ? 0 : (int)(Math.Floor(Math.Max(0f, seconds) * Math.Max(0.01f, framesPerSecond)) % count);

    public static float FrameBlend(float seconds, float framesPerSecond, float crossFade)
    {
        if (crossFade <= 0f) return 0f;
        double frame = Math.Max(0f, seconds) * Math.Max(0.01f, framesPerSecond);
        return Clamp01((float)((frame - Math.Floor(frame) - (1f - Clamp01(crossFade))) / Clamp01(crossFade)));
    }

    public static bool IsChargeReady(bool hasOrb, bool crescent, float energy, float threshold)
        => hasOrb && !crescent && energy >= Math.Max(0.01f, threshold);

    private static float Clamp01(float value) => Math.Max(0f, Math.Min(1f, value));
}
