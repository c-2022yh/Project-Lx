using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
namespace UnityEngine
{
    public class MonoBehaviour
    {
        public GameObject gameObject=new GameObject(); public Transform transform=>gameObject.transform;
        public T GetComponent<T>() where T:class=>gameObject.components.Values.OfType<T>().FirstOrDefault();
        public static void Destroy(GameObject o){o.destroyed=true;}
    }
    public class GameObject { public int layer; public bool destroyed;public Transform transform=new Transform();public Dictionary<Type,object> components=new Dictionary<Type,object>(); }
    public class Transform { public Vector3 position;public void Rotate(float x,float y,float z){} }
    public struct Vector2
    {
        public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}
        public static implicit operator Vector2(Vector3 v)=>new Vector2(v.x,v.y);
        public static implicit operator Vector3(Vector2 v)=>new Vector3(v.x,v.y,0);
        public static float Distance(Vector2 a,Vector2 b)=>(float)Math.Sqrt((a.x-b.x)*(a.x-b.x)+(a.y-b.y)*(a.y-b.y));
    }
    public struct Vector3
    {
        public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
        public static Vector3 right=>new Vector3(1,0,0);
        public static Vector3 operator *(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);
        public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
    }
    public struct LayerMask {public int value; public static implicit operator LayerMask(int value)=>new LayerMask{value=value};}
    public class Rigidbody2D {public float gravityScale;public Vector2 linearVelocity;}
    public class SpriteRenderer {public bool flipX;}
    public class Collider2D:MonoBehaviour
    {
        public bool isTrigger;
        public T GetComponentInParent<T>() where T:class=>GetComponent<T>();
        public Vector2 ClosestPoint(Vector2 p)=>p;
    }
    public static class Time {public static float deltaTime;}
}
public class SoundCue {public string name;public SoundCue(string n){name=n;}}
public sealed class Voice {public bool active=true;}
public struct SoundHandle {public Voice voice;public bool IsPlaying=>voice!=null&&voice.active;public void Stop(){if(voice!=null)voice.active=false;}}
public static class GameAudio
{
    public static List<(SoundCue cue,bool loop)> calls=new List<(SoundCue,bool)>();
    public static SoundHandle Play(SoundCue cue,Vector3 pos){if(cue!=null)calls.Add((cue,false));return new SoundHandle{voice=new Voice()};}
    public static SoundHandle PlayLoop(SoundCue cue,Transform owner){if(cue!=null)calls.Add((cue,true));return new SoundHandle{voice=new Voice()};}
}
public interface IDamageable {void TakeDamage(DamageInfo d,Vector2 direction);}
public struct DamageInfo {public bool isCritical;}
public class FireBreakableWall:MonoBehaviour {public int hits;public void HitWall(){hits++;}}
public class SecretBreakableWall:MonoBehaviour {public int hits;public void HitWallAtWorldPosition(Vector2 p){hits++;}}
public class FireballVisual:MonoBehaviour {public int impacts;public void PlayImpact(){impacts++;}}
public static class CombatFeedback {public static void OnPlayerHit(IDamageable t,float d,bool c){}}
public class Target:MonoBehaviour,IDamageable {public int hits;public void TakeDamage(DamageInfo d,Vector2 dir){hits++;}}
internal static class Program
{
    static int checks;
    static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;}
    static T Add<T>(GameObject go,T c) where T:MonoBehaviour {c.gameObject=go;go.components[typeof(T)]=c;return c;}
    static void Invoke(object o,string name,params object[] args)=>o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,args);
    static readonly SoundCue terrain=new SoundCue("terrain"),expire=new SoundCue("expire"),flight=new SoundCue("flight");
    static (ProjectileHitbox hitbox,FireballProjectile fire,FireballVisual visual) New(int mask=0)
    {
        var go=new GameObject();var h=Add(go,new ProjectileHitbox());var f=Add(go,new FireballProjectile());var v=Add(go,new FireballVisual());
        h.Init(1,0,1,0,new DamageInfo(),0,true,flight,terrain,expire,mask);GameAudio.calls.Clear();return(h,f,v);
    }
    static Collider2D Collider(MonoBehaviour target=null,int layer=0,bool trigger=false)
    {
        var go=new GameObject{layer=layer};if(target!=null){target.gameObject=go;go.components[target.GetType()]=target;}return Add(go,new Collider2D{isTrigger=trigger});
    }
    static void Main()
    {
        var a=New();Check(a.hitbox.ConsumeOnTerrain(),"First terrain consume accepted");Check(!a.hitbox.ConsumeOnTerrain(),"Duplicate terrain consume rejected");
        Check(GameAudio.calls.Count==1&&GameAudio.calls[0].cue==terrain&&a.visual.impacts==1,"One impact and visual");
        var voice=(SoundHandle)typeof(ProjectileHitbox).GetField("flightAudio",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(a.hitbox);
        Check(!voice.IsPlaying,"Consumed flight loop stopped");
        a=New();var wall=new FireBreakableWall();var col=Collider(wall);Invoke(a.fire,"OnTriggerEnter2D",col);Invoke(a.hitbox,"OnTriggerEnter2D",col);
        Check(wall.hits==1&&GameAudio.calls.Count==1,"Fire component callback first does not duplicate");
        a=New();wall=new FireBreakableWall();col=Collider(wall);Invoke(a.hitbox,"OnTriggerEnter2D",col);Invoke(a.fire,"OnTriggerEnter2D",col);
        Check(wall.hits==1&&GameAudio.calls.Count==1&&a.visual.impacts==1,"Hitbox callback first does not duplicate");
        a=New();var secret=new SecretBreakableWall();col=Collider(secret);Invoke(a.hitbox,"OnTriggerEnter2D",col);Invoke(a.hitbox,"OnTriggerEnter2D",col);
        Check(secret.hits==1&&GameAudio.calls.Count==1,"Secret wall contact processed once");
        Invoke(a.hitbox,"Update");Check(GameAudio.calls.Count==1,"Consumed shot cannot also expire");
        a=New();var enemy=new Target();col=Collider(enemy);Invoke(a.hitbox,"OnTriggerEnter2D",col);Invoke(a.hitbox,"OnTriggerEnter2D",col);
        Check(enemy.hits==1&&GameAudio.calls.Count==0,"Enemy hit does not add terrain/fizzle sound");
        Check(a.visual.impacts==1&&a.hitbox.gameObject.destroyed,"Enemy destruction visual once");
        a=New();col=Collider(null,6);Invoke(a.hitbox,"OnTriggerEnter2D",col);
        Check(!a.hitbox.gameObject.destroyed&&GameAudio.calls.Count==0,"Default ordinary terrain pass-through preserved");
        a=New(64);Invoke(a.hitbox,"OnTriggerEnter2D",col);
        Check(a.hitbox.gameObject.destroyed&&GameAudio.calls.Count==1,"Explicit Ground mask enables ordinary terrain impact");
        a=New(64);col=Collider(null,6,true);Invoke(a.hitbox,"OnTriggerEnter2D",col);
        Check(!a.hitbox.gameObject.destroyed&&GameAudio.calls.Count==0,"Terrain triggers do not consume shot");
        a=New();a.hitbox.transform.position=new Vector3(2,0,0);Invoke(a.hitbox,"Update");Invoke(a.hitbox,"Update");
        Check(GameAudio.calls.Count==1&&GameAudio.calls[0].cue==expire,"Range expiry plays fizzle once");
        a=New();Invoke(a.hitbox,"OnDisable");Check(GameAudio.calls.Count==0,"Cleanup/disable does not synthesize expiry sound");
        Console.WriteLine("FIREBALL_AUDIO_VALIDATION checks="+checks+" failures=0");
    }
}
