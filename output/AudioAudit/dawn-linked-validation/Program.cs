using System;
using System.Collections.Generic;
using UnityEngine;
namespace UnityEngine {
 public class Transform { public Vector3 position; }
 public struct Vector3 { }
 public struct Vector2 { public float x,y; public static Vector2 one=>new Vector2{x=1,y=1}; }
 public class AudioClip { }
 public static class Random { public static int calls; public static int Range(int a,int b) => a + calls++ % (b-a); }
 [AttributeUsage(AttributeTargets.Field)] public class TooltipAttribute:Attribute {public TooltipAttribute(string x){} }
 [AttributeUsage(AttributeTargets.Field)] public class RangeAttribute:Attribute {public RangeAttribute(float a,float b){} }
 [AttributeUsage(AttributeTargets.Field)] public class MinAttribute:Attribute {public MinAttribute(float a){} }
 [AttributeUsage(AttributeTargets.Field)] public class HeaderAttribute:Attribute {public HeaderAttribute(string a){} }
 public class AudioSource {public bool isPlaying=true,ignoreListenerPause; }
 public static class AudioListener {public static bool pause;}
}
public enum GameAudioChannel {SFX,UI,BGM}
public enum GameSound {UIClick}
internal class SoundVoice { public uint generation; public bool active=true; public AudioSource source=new AudioSource(); public void Stop(){active=false;} }
public static class GameAudio {
 public static readonly List<(SoundCue cue,AudioClip clip,Transform owner)> calls=new();
 public static bool reject;
 public static SoundCue fallback;
 internal static SoundCue ResolveCueForPlayback(SoundCue c)=> c==null ? null : c.HasClip ? c : c.useFallback ? fallback : null;
 public static SoundHandle Play(SoundCue c, Vector3 pos,Transform owner) {var r=ResolveCueForPlayback(c);return PlaySelectedClip(r,r?.PickClip(),pos,owner);}
 internal static SoundHandle PlaySelectedClip(SoundCue c,AudioClip clip,Vector3 pos,Transform owner) {
  if(c==null||clip==null||reject)return default;
  calls.Add((c,clip,owner));return new SoundHandle(new SoundVoice());
 }
}
class Program {
 static int checks;
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;}
 static void Main(){
  var owner=new Transform();var target=new Transform();var other=new Transform();
  var a=new AudioClip();var b=new AudioClip();var cue=new SoundCue{clips=new[]{a,b}};
  var basic=new AttackAudioContext(cue,ImpactSoundMode.OncePerAction,owner);
  var extra=basic.CreateLinkedImpact(ImpactSoundMode.OncePerTarget);
  Check(GameAudio.calls.Count==0,"creation is silent even on a total miss");
  Check(UnityEngine.Random.calls==1,"choose once at attack creation");
  basic.OnDamageApplied(target);extra.OnDamageApplied(target);
  Check(GameAudio.calls.Count==2,"both accepted hits play");
  Check(ReferenceEquals(GameAudio.calls[0].clip,GameAudio.calls[1].clip),"identical clip for paired hits");
  Check(ReferenceEquals(GameAudio.calls[0].cue,cue)&&ReferenceEquals(GameAudio.calls[1].cue,cue),"retain cue identity for voice limits");
  Check(ReferenceEquals(GameAudio.calls[1].owner,owner),"retain source owner");
  extra.OnDamageApplied(target);basic.OnDamageApplied(other);
  Check(GameAudio.calls.Count==2,"policies retain separate state");
  extra.OnDamageApplied(other);Check(GameAudio.calls.Count==3,"extra can hit another target");
  GameAudio.calls.Clear();
  var missed=new AttackAudioContext(cue,ImpactSoundMode.EveryHit,owner);
  var onlyExtra=missed.CreateLinkedImpact(ImpactSoundMode.OncePerTarget);
  onlyExtra.OnDamageApplied(target);
  Check(GameAudio.calls.Count==1,"extra hits while basic misses");
  Check(ReferenceEquals(GameAudio.calls[0].clip,b),"new attack selects a fresh variant");
  GameAudio.calls.Clear();
  var onlyBasic=new AttackAudioContext(cue,ImpactSoundMode.EveryHit,owner);
  onlyBasic.CreateLinkedImpact(ImpactSoundMode.OncePerTarget);
  onlyBasic.OnDamageApplied(target);Check(GameAudio.calls.Count==1,"no scheduled echo when extra misses");
  GameAudio.calls.Clear();
  var ordinary=new AttackAudioContext(cue,ImpactSoundMode.EveryHit,owner);
  ordinary.OnDamageApplied(target);ordinary.OnDamageApplied(target);
  Check(!ReferenceEquals(GameAudio.calls[0].clip,GameAudio.calls[1].clip),"unlinked attacks keep existing randomness");
  GameAudio.calls.Clear();
  var silent=new AttackAudioContext(new SoundCue(),ImpactSoundMode.EveryHit,owner);
  var silentExtra=silent.CreateLinkedImpact(ImpactSoundMode.OncePerTarget);
  silent.OnDamageApplied(target);silentExtra.OnDamageApplied(target);
  Check(GameAudio.calls.Count==0,"empty source stays silent");
  GameAudio.fallback=cue;
  var fallback=new AttackAudioContext(new SoundCue{useFallback=true},ImpactSoundMode.EveryHit,owner);
  var fallbackExtra=fallback.CreateLinkedImpact(ImpactSoundMode.OncePerTarget);
  fallback.OnDamageApplied(target);fallbackExtra.OnDamageApplied(target);
  Check(GameAudio.calls.Count==2&&ReferenceEquals(GameAudio.calls[0].clip,GameAudio.calls[1].clip),"fallback also shares clip");
  GameAudio.calls.Clear();GameAudio.reject=true;
  var retry=new AttackAudioContext(cue,ImpactSoundMode.OncePerTarget,owner).CreateLinkedImpact(ImpactSoundMode.OncePerTarget);
  retry.OnDamageApplied(target);GameAudio.reject=false;retry.OnDamageApplied(target);
  Check(GameAudio.calls.Count==1,"rejected playback does not consume target policy");
  var direct=new AttackAudioContext(new SoundCue{clip=a,clips=new[]{b}},ImpactSoundMode.EveryHit,owner);
  var directExtra=direct.CreateLinkedImpact(ImpactSoundMode.OncePerTarget);
  GameAudio.calls.Clear();direct.OnDamageApplied(target);directExtra.OnDamageApplied(target);
  Check(GameAudio.calls.Count==2&&ReferenceEquals(GameAudio.calls[0].clip,a)&&ReferenceEquals(GameAudio.calls[1].clip,a),"single Clip priority preserved");
  Console.WriteLine($"PASS: {checks} linked-impact checks (actual AttackAudioContext/SoundCue, playback stubbed)");
 }
}
