internal sealed class PlayerMovementAudioState
{
    private bool initialized, wasGrounded, hasGroundedBaseline;
    private float airborneSince, travel, lastStepTime;

    public void Reset()
    {
        initialized = wasGrounded = hasGroundedBaseline = false;
        travel = 0f;
        lastStepTime = float.NegativeInfinity;
    }

    public void Tick(bool grounded, bool canStep, float distance, float time,
        float stepDistance, float stepInterval, float landingAirTime, out bool step, out bool land)
    {
        step = land = false;
        if (!initialized)
        {
            initialized = true;
            wasGrounded = hasGroundedBaseline = grounded;
            airborneSince = time;
            travel = 0f;
            lastStepTime = float.NegativeInfinity;
            return;
        }
        if (!grounded)
        {
            if (wasGrounded) airborneSince = time;
            wasGrounded = false;
            travel = 0f;
            return;
        }
        if (!wasGrounded)
        {
            land = hasGroundedBaseline && time - airborneSince >= System.Math.Max(0f, landingAirTime);
            hasGroundedBaseline = wasGrounded = true;
            travel = 0f;
            return;
        }
        hasGroundedBaseline = true;
        if (!canStep) { travel = 0f; return; }
        travel += System.Math.Max(0f, distance);
        if (travel < System.Math.Max(0.05f, stepDistance) || time - lastStepTime < System.Math.Max(0f, stepInterval)) return;
        step = true;
        travel = 0f;
        lastStepTime = time;
    }
}

internal static class MovementNames
{
    internal static bool MatchesSurfaceName(string objectName, string[] names)
    {
        if (names == null) return false;
        const string cloneSuffix = "(Clone)";
        if (objectName.EndsWith(cloneSuffix, System.StringComparison.Ordinal))
            objectName = objectName.Substring(0, objectName.Length - cloneSuffix.Length).TrimEnd();
        foreach (string candidate in names)
            if (!string.IsNullOrWhiteSpace(candidate)
                && string.Equals(objectName, candidate.Trim(), System.StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }
}
