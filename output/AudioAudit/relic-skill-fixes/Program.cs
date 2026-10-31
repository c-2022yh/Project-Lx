using System;using System.Collections;using System.Collections.Generic;using System.Linq;using UnityEngine;
namespace UnityEngine {
 public class Object {public bool destroyed;}
 public class MonoBehaviour:Object {
  public GameObject gameObject=new GameObject();public Transform transform=>gameObject.transform;
  protected static GameObject Instantiate(GameObject g,Transform p=null)=>new GameObject();
  protected static void Destroy(Object o){if(o!=null)o.destroyed=true;}
 }
 public class GameObject:Object {public Transform transform=new Transform();public T GetComponent<T>() where T:new()=>new T();public T AddComponent<T>() where T:new()=>new T();}
 public class Transform {public Vector3 position,right;}
 public struct Vector3 {public float x,y;public static implicit operator Vector2(Vector3 v)=>new Vector2(v.x,v.y);public static implicit operator Vector3(Vector2 v)=>new Vector3{x=v.x,y=v.y};}
 public struct Vector2 {public float x,y;public Vector2(float a,float b){x=a;y=b;} public static Vector2 operator +(Vector2 a,Vector2 b)=>new Vector2(a.x+b.x,a.y+b.y);public static Vector2 operator *(Vector2 a,float b)=>new Vector2(a.x*b,a.y*b);}
 public struct LayerMask{}
 public static class Mathf {public const float Deg2Rad=(float)(Math.PI/180);public static int Max(int a,int b)=>Math.Max(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static float Lerp(float a,float b,float t)=>a+(b-a)*t;public static float Cos(float a)=>(float)Math.Cos(a);public static float Sin(float a)=>(float)Math.Sin(a);}
 public static class Time {public static float time;}
 public class WaitForSeconds {public float duration;public WaitForSeconds(float d){duration=d;}}
}
public class Player {public bool isFacingRight=true,isActiveAndEnabled=true;public Transform transform=new Transform();public Health Health=new Health();public Awakening Awakening=new Awakening();public State ActionState=new State();}
public class Health {public bool IsDead;}
public class Awakening {public bool IsAwakened=true;public int ends;public void EndAwakening(){IsAwakened=false;ends++;}}
public class State {public bool isSkillActive=true;}
public class Enemy {}
public class SkillAudioSession {public int releases;public void BeginActive(){} public void Release(Vector3 p){releases++;}}
public class ScorchedEarthSkillData {
 public struct PreparedDamage {public object regular;}
 public class BarrageSettings {public int projectileCount=12;public GameObject regularProjectilePrefab=new GameObject(),preparationEffectPrefab;public float targetSearchRadius=12,formationArc=150,formationRadius=1.2f,preparationDuration=.15f,launchInterval=.015f;public Vector2 formationOffset;public LayerMask sightObstacleLayer;}
}
public static class ScorchedEarthTargeting {public static List<Enemy> FindTargets(Vector3 p,float r,LayerMask l,LayerMask o)=>new List<Enemy>{new Enemy()};}
public class ScorchedEarthProjectile:MonoBehaviour {
 public static List<ScorchedEarthProjectile> all=new List<ScorchedEarthProjectile>();public bool launched;public Action callback;
 public ScorchedEarthProjectile(){all.Add(this);}
 public void Launch(ScorchedEarthSkillData.BarrageSettings c,LayerMask l,object d,Enemy e,Vector2 v,Action a){launched=true;callback=a;}
}
class Program {
 static int checks;static void Check(bool ok,string msg){if(!ok)throw new Exception(msg);checks++;}
 static void Step(ref float due,IEnumerator run){Time.time=due;if(run.MoveNext())due+=run.Current is WaitForSeconds w?Math.Max(.02f,w.duration):.02f;else due=-1;}
 static void Main(){
  Check(AwakeningReadyAudioState.IsEnergyReady(100,100,100,false),"normal ready");
  Check(!AwakeningReadyAudioState.IsEnergyReady(99,100,100,false),"normal below threshold");
  Check(!AwakeningReadyAudioState.IsEnergyReady(30,30,100,false),"ordinary cap cannot awaken");
  Check(AwakeningReadyAudioState.IsEnergyReady(100,100,100,true),"FullMoon full notification");
  Check(AwakeningReadyAudioState.IsEnergyReady(30,30,100,true),"DarkMoon capped full notification");
  Check(!AwakeningReadyAudioState.IsEnergyReady(29,30,100,true),"DarkMoon partial silent");
  Check(!AwakeningReadyAudioState.IsEnergyReady(0,0,100,true),"no zero capacity notification");
  var ready=new AwakeningReadyAudioState();Check(!ready.Observe(true,false),"silent equip baseline");Check(!ready.Observe(true,true),"full does not repeat");Check(!ready.Observe(false,true),"spend silent");Check(ready.Observe(true,true),"refill emits once");Check(!ready.Observe(true,true),"duplicate event silent");
  Time.time=0;var p=new Player();var config=new ScorchedEarthSkillData.BarrageSettings();var audio=new SkillAudioSession();var barrage=new ScorchedEarthBarrage();
  barrage.Initialize(p,config,default,default,audio,.3f);Check(ScorchedEarthProjectile.all.Count==12,"12 prepared");
  var routine=barrage.Process();float due=0;int guard=0;while(due>=0&&guard++<100)Step(ref due,routine);
  Check(guard<100&&Time.time<.6f,"cast exits without waiting for projectile lifetime");
  Check(ScorchedEarthProjectile.all.All(x=>x.launched&&!x.gameObject.destroyed),"all launched projectiles survive cast completion");
  Check(audio.releases==12,"per projectile launch sound preserved");Check(p.Awakening.ends==1,"awakening ends once");
  barrage.Cancel();Check(p.Awakening.ends==1,"finalizer cancellation is idempotent");
  Check(ScorchedEarthProjectile.all.All(x=>x.callback==null),"projectile resolution cannot end a later cast");
  ScorchedEarthProjectile.all.Clear();Time.time=0;p=new Player();barrage=new ScorchedEarthBarrage();barrage.Initialize(p,config,default,default,null,1f);routine=barrage.Process();due=0;Step(ref due,routine);Step(ref due,routine);barrage.Cancel();
  Check(ScorchedEarthProjectile.all.Any(x=>x.launched&&!x.gameObject.destroyed),"cancel preserves launched shard");
  Check(ScorchedEarthProjectile.all.Where(x=>!x.launched).All(x=>x.gameObject.destroyed),"cancel clears waiting shards");
  Check(p.Awakening.ends==1,"cancel ends awakening once");
  ScorchedEarthProjectile.all.Clear();Time.time=0;p=new Player();config=new ScorchedEarthSkillData.BarrageSettings{projectileCount=1,preparationDuration=0,launchInterval=0};barrage=new ScorchedEarthBarrage();barrage.Initialize(p,config,default,default,null,.4f);routine=barrage.Process();due=0;guard=0;while(due>=0&&guard++<100)Step(ref due,routine);
  Check(Time.time>=.4f&&Time.time<.45f,"configured active duration honored even with fast launch");
  Console.WriteLine($"PASS: {checks} readiness/barrage checks, real barrage source with Unity stubs");
 }
}
