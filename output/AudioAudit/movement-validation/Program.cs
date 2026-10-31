using System;

internal static class Program
{
    private static int checks;
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception(name);
        checks++;
    }

    private static (bool step, bool land) Tick(PlayerMovementAudioState state, bool grounded,
        bool walking, float distance, float time, float interval = 0.12f)
    {
        state.Tick(grounded, walking, distance, time, 2f, interval, 0.08f, out bool step, out bool land);
        return (step, land);
    }

    private static void Main()
    {
        var state = new PlayerMovementAudioState();
        Check(Tick(state, true, true, 10f, 0f) == (false, false), "Initial ground never plays a step or landing");
        Check(!Tick(state, true, true, 1f, 0.1f).step, "Wait for walking distance");
        Check(Tick(state, true, true, 1.1f, 0.2f).step, "Play one step after distance threshold");
        Check(!Tick(state, true, true, 3f, 0.21f).step, "Minimum step interval");
        Check(Tick(state, true, true, 0.1f, 0.4f).step, "Pending distance plays after interval");
        Tick(state, true, false, 10f, 0.5f);
        Check(!Tick(state, true, true, 0.2f, 0.6f).step, "Idle/platform ride clears distance");
        Check(Tick(state, false, true, 10f, 0.7f) == (false, false), "No airborne footsteps");
        Check(Tick(state, true, true, 10f, 0.73f) == (false, false), "Ignore brief grounding flicker");
        Tick(state, false, false, 0f, 1f);
        Check(Tick(state, true, true, 10f, 1.2f) == (false, true), "Landing after real airborne interval, no simultaneous step");
        Check(!Tick(state, true, false, 0f, 1.3f).land, "Landing only once");
        state.Reset();
        Check(Tick(state, true, true, 50f, 2f) == (false, false), "Respawn/teleport reset is silent");
        state.Reset();
        Tick(state, false, false, 0f, 3f);
        Check(!Tick(state, true, false, 0f, 4f).land, "First spawn in air does not synthesize a landing");
        Tick(state, false, false, 0f, 5f);
        Check(Tick(state, true, false, 0f, 5.2f).land, "Later fall after baseline does land");
        Check(MovementNames.MatchesSurfaceName("Tilemap_Grass", new[] { "Tilemap_Grass" }), "Grass tilemap matches");
        Check(MovementNames.MatchesSurfaceName("TIleMap_Platform", new[] { "Tilemap_Platform" }), "Platform name case ignored");
        Check(MovementNames.MatchesSurfaceName("MovingPlatform(Clone)", new[] { "MovingPlatform" }), "Spawned platform clone matches");
        Check(!MovementNames.MatchesSurfaceName("Background_Grass", new[] { "Grass" }), "Background name cannot match partial grass token");
        Check(!MovementNames.MatchesSurfaceName("Ground", null), "Missing matching list is safe");
        Console.WriteLine("MOVEMENT_AUDIO_VALIDATION checks=" + checks + " failures=0");
    }
}
