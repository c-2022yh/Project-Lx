using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace UnityEngine.SceneManagement { public class Stub { } }
namespace UnityEngine
{
    public class SerializeField : Attribute { }
    public class HeaderAttribute : Attribute { public HeaderAttribute(string v) { } }
    public class TooltipAttribute : Attribute { public TooltipAttribute(string v) { } }
    public class MinAttribute : Attribute { public MinAttribute(float v) { } }
    public struct Vector2 { public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;} public static implicit operator Vector3(Vector2 v)=>new Vector3(v.x,v.y,0); }
    public struct Vector3 { public float x,y,z; public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;} public static Vector3 one=>new Vector3(1,1,1); }
    public struct Vector3Int { public int x,y,z; public Vector3Int(int x,int y,int z){this.x=x;this.y=y;this.z=z;} }
    public struct Quaternion { public static Quaternion identity=>default; }
    public struct Color { public float a; public Color(float r,float g,float b){a=1;} public static Color operator *(Color a,Color b)=>a; }
    public struct Bounds { public Bounds(Vector3 a,Vector3 b) { } }
    public class Transform { public Vector3 position; public Vector3 lossyScale=Vector3.one; }
    public class Scene { public string name="Test"; }
    public class GameObject { public string name="Test"; public Scene scene=new Scene(); public bool destroyed; }
    public class MonoBehaviour
    {
        public GameObject gameObject=new GameObject(); public Transform transform=new Transform();
        public T GetComponent<T>() where T:class=>null;
        public static void Destroy(GameObject g){g.destroyed=true;}
        public static GameObject Instantiate(GameObject g,Vector3 p,Quaternion q)=>new GameObject();
    }
    public class Collider2D { public Player actor; public T GetComponentInParent<T>() where T:class=>actor as T; }
    public static class Debug { public static void Log(object o){} public static void LogWarning(object o,object owner){} public static void LogError(object o){} }
    public class GridLayout { public Vector3 cellSize=Vector3.one; }
}
namespace UnityEngine.Tilemaps
{
    public class Tilemap : MonoBehaviour
    {
        public HashSet<Vector3Int> tiles=new HashSet<Vector3Int>(); public GridLayout layoutGrid=new GridLayout(); public Color color;
        public bool HasTile(Vector3Int p)=>tiles.Contains(p);
        public void SetTile(Vector3Int p,object tile){if(tile==null)tiles.Remove(p);else tiles.Add(p);}
        public void RefreshTile(Vector3Int p){}
        public Vector3Int WorldToCell(Vector3 p)=>new Vector3Int((int)Math.Floor(p.x),(int)Math.Floor(p.y),0);
        public Vector3 GetCellCenterWorld(Vector3Int p)=>new Vector3(p.x+0.5f,p.y+0.5f,0);
        public Color GetColor(Vector3Int p)=>default;
    }
    public class TilemapRenderer { public int sortingLayerID,sortingOrder; }
}
public class SoundCue{}
public class Player:MonoBehaviour { public void SetInteractable(IInteractable i){} public void ClearInteractable(IInteractable i){} }
public static class GameAudio
{
    public static readonly List<(SoundCue cue,Vector3 position,bool stops)> calls=new List<(SoundCue,Vector3,bool)>();
    public static void Play(SoundCue cue,Vector3 position,Transform owner=null,bool stopWithOwner=false){calls.Add((cue,position,stopWithOwner));}
}
public static class SceneFader { public static bool IsTransitioning; public static int loads; public static void LoadScene(string s){loads++;IsTransitioning=true;} }
public static class SceneTransitionData { public static string TargetSpawnId; }
public static class FieldObjectState
{
    public static HashSet<object> done=new HashSet<object>(); public static List<Vector3Int> broken=new List<Vector3Int>();
    public static bool IsDone(object o)=>done.Contains(o); public static void MarkDone(object o){done.Add(o);}
    public static IEnumerable<Vector3Int> BrokenTiles(string scene)=>broken;
    public static void MarkTileBroken(string scene,Vector3Int cell){broken.Add(cell);}
}
public static class DebrisBurst
{
    public struct Options { public float strength;public int sortingLayerId,sortingOrder;public static Options Default(Color c)=>default; }
    public static void SpawnFrom(GameObject g,Color c,float strength=1){} public static void Spawn(Bounds b,Options o){}
}
internal static class Program
{
    static int checks;
    static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;}
    static void Set(object o,string field,object value)=>o.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,value);
    static object Get(object o,string field)=>o.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(o);
    static void Invoke(object o,string method,params object[] args)=>o.GetType().GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,args);
    static void ResetPortal(){SceneFader.loads=0;SceneFader.IsTransitioning=false;GameAudio.calls.Clear();SceneTransitionData.TargetSpawnId=null;}
    static void Main()
    {
        var actor=new Player();ResetPortal();var portal=new ScenePortal();
        portal.Interact(actor);Check(GameAudio.calls.Count==0&&SceneFader.loads==0,"Empty manual portal silent");
        Set(portal,"targetSceneName","Destination");Set(portal,"targetSpawnId","A");portal.Interact(actor);
        Check(GameAudio.calls.Count==1&&SceneFader.loads==1&&SceneTransitionData.TargetSpawnId=="A","Manual travel sound and destination");
        Check(!GameAudio.calls[0].stops,"Portal tail is not bound to scene object lifetime");
        Set(portal,"targetSpawnId","B");portal.Interact(actor);
        Check(GameAudio.calls.Count==1&&SceneTransitionData.TargetSpawnId=="A","Busy manual portal neither repeats nor replaces spawn");
        ResetPortal();var auto=new AutoScenePortal();Set(auto,"targetSceneName","Destination");
        Invoke(auto,"OnTriggerEnter2D",new Collider2D());Check(GameAudio.calls.Count==0,"Non-player automatic trigger silent");
        SceneFader.IsTransitioning=true;Invoke(auto,"OnTriggerEnter2D",new Collider2D{actor=actor});
        Check(!(bool)Get(auto,"isTriggered")&&GameAudio.calls.Count==0,"Busy auto portal not consumed");
        SceneFader.IsTransitioning=false;Invoke(auto,"OnTriggerEnter2D",new Collider2D{actor=actor});Invoke(auto,"OnTriggerEnter2D",new Collider2D{actor=actor});
        Check(GameAudio.calls.Count==1&&SceneFader.loads==1,"Auto multi-collider travel only once");
        GameAudio.calls.Clear();var brick=new BreakableBrick();Set(brick,"spawnDebris",false);Set(brick,"requiredHitsToBreak",2);
        brick.HitBrick();Check(GameAudio.calls.Count==1&&ReferenceEquals(GameAudio.calls[0].cue,Get(brick,"hitSound")),"Surviving brick plays hit");
        brick.HitBrick();brick.HitBrick();Check(GameAudio.calls.Count==2&&ReferenceEquals(GameAudio.calls[1].cue,Get(brick,"breakSound")),"Final brick hit plays break only, duplicate ignored");
        Check(brick.gameObject.destroyed&&FieldObjectState.IsDone(brick)&&!GameAudio.calls[1].stops,"Brick persists state and sound tail");
        var savedBrick=new BreakableBrick();FieldObjectState.MarkDone(savedBrick);int count=GameAudio.calls.Count;Invoke(savedBrick,"Awake");savedBrick.HitBrick();
        Check(GameAudio.calls.Count==count&&savedBrick.gameObject.destroyed,"Restored broken brick silent even before Destroy completes");
        var fire=new FireBreakableWall();Set(fire,"spawnDebris",false);Set(fire,"requiredHitsToBreak",2);GameAudio.calls.Clear();
        fire.HitWall();fire.HitWall();fire.HitWall();Check(GameAudio.calls.Count==2&&ReferenceEquals(GameAudio.calls[0].cue,Get(fire,"hitSound"))&&ReferenceEquals(GameAudio.calls[1].cue,Get(fire,"breakSound")),"Fire wall hit/break separation and repeat guard");
        var single=new BreakableBrick();Set(single,"spawnDebris",false);GameAudio.calls.Clear();single.HitBrick();
        Check(GameAudio.calls.Count==1&&ReferenceEquals(GameAudio.calls[0].cue,Get(single,"breakSound")),"One-hit object has no separate hit sound");
        var tilemap=new Tilemap();var cell=new Vector3Int(4,2,0);var neighbor=new Vector3Int(10,2,0);tilemap.tiles.Add(cell);tilemap.tiles.Add(neighbor);
        var secret=new SecretBreakableWall();Set(secret,"tilemapSecretWall",tilemap);Set(secret,"requiredHitsToBreak",2);Set(secret,"spawnDebris",false);GameAudio.calls.Clear();
        secret.HitWallAtWorldPosition(new Vector2(4.2f,2.2f));Check(GameAudio.calls.Count==1&&tilemap.HasTile(cell),"Secret tile surviving hit");
        secret.HitWallAtWorldPosition(new Vector2(4.2f,2.2f));Check(GameAudio.calls.Count==2&&!tilemap.HasTile(cell)&&tilemap.HasTile(neighbor),"Secret destroys target tile only");
        Check(ReferenceEquals(GameAudio.calls[1].cue,Get(secret,"breakSound"))&&GameAudio.calls[1].position.x==4.5f,"Secret break sound at tile center");
        secret.HitWallAtWorldPosition(new Vector2(4.2f,2.2f));Check(GameAudio.calls.Count==2,"No audio on empty secret region");
        var restoredMap=new Tilemap();restoredMap.tiles.Add(cell);var restoredSecret=new SecretBreakableWall();Set(restoredSecret,"tilemapSecretWall",restoredMap);Invoke(restoredSecret,"Awake");
        Check(!restoredMap.HasTile(cell)&&GameAudio.calls.Count==2,"Secret restoration silent");
        Console.WriteLine("FIELD_AUDIO_VALIDATION checks="+checks+" failures=0");
    }
}
