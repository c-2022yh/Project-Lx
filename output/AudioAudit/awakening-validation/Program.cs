using System;
static class Program {
 static int count;
 static void Check(bool ok, string label) { if(!ok) throw new Exception(label); count++; }
 static void Main() {
  var state = new AwakeningReadyAudioState();
  Check(!state.Observe(false,true), "initial not ready");
  Check(state.Observe(true,true), "threshold reached");
  Check(!state.Observe(true,true), "full gauge repeated event");
  Check(!state.Observe(true,true), "capacity event still ready");
  Check(!state.Observe(false,true), "spent energy");
  Check(state.Observe(true,true), "refill after spend");
  state.Reset();
  Check(!state.Observe(true,true), "initial full load silent");
  Check(!state.Observe(false,false), "intro entry prime");
  Check(!state.Observe(true,false), "intro cancellation prime");
  Check(!state.Observe(true,true), "cancel does not repeat ready");
  Check(!state.Observe(false,false), "blocked prime");
  Check(!state.Observe(true,false), "unblock primes silently");
  Check(!state.Observe(true,true), "no repeat after unblock");
  state.Reset();
  Check(!state.Observe(true,true), "reenable baseline silent");
  Console.WriteLine("AWAKENING_AUDIO_VALIDATION checks="+count+" failures=0");
 }
}