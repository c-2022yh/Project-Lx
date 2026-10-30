param([string]$Manifest)
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System; using System.Drawing; using System.Drawing.Imaging; using System.Drawing.Drawing2D;
public static class LeafFrameTools {
 public static Rectangle Bounds(Bitmap b) {
  int l=b.Width,t=b.Height,r=-1,d=-1;
  for(int y=0;y<b.Height;y++)for(int x=0;x<b.Width;x++)if(b.GetPixel(x,y).A>=16){l=Math.Min(l,x);r=Math.Max(r,x);t=Math.Min(t,y);d=Math.Max(d,y);}
  if(r<0)throw new Exception("Empty sprite"); return Rectangle.FromLTRB(l,t,r+1,d+1);
 }
 public static void Fit(string generated,string original,string neutral,string output) {
  using(var src=new Bitmap(generated))using(var old=new Bitmap(original))using(var idle=new Bitmap(neutral))using(var dst=new Bitmap(640,640,PixelFormat.Format32bppArgb)) {
   var a=Bounds(src);var b=Bounds(old);var n=Bounds(idle);
   int top=(int)Math.Round((b.Top+n.Top)*0.5);
   var target=Rectangle.FromLTRB(b.Left,top,b.Right,b.Bottom);
   using(var g=Graphics.FromImage(dst)) {g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;g.DrawImage(src,target,a,GraphicsUnit.Pixel);}
   dst.Save(output,ImageFormat.Png);
   Console.WriteLine(System.IO.Path.GetFileName(output)+": top "+b.Top+" -> "+Bounds(dst).Top+", baseline "+b.Bottom+" -> "+Bounds(dst).Bottom);
  }
 }
 public static void Preview(string root,string backup,string output) {
  using(var dst=new Bitmap(1920,960,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(dst)) {
   g.Clear(Color.FromArgb(37,45,52));
   for(int t=1;t<=3;t++)for(int f=1;f<=3;f++)for(int state=0;state<2;state++) {
    string file="Leaf"+t.ToString("00")+"_Move_"+f.ToString("00")+".png";
    string path=state==0?System.IO.Path.Combine(backup,file):System.IO.Path.Combine(root,"Assets/Sprites/Enemies/Leaf/Leaf"+t.ToString("00"),file);
    using(var b=new Bitmap(path))g.DrawImage(b,new Rectangle((state*3+f-1)*320,(t-1)*320,320,320));
   }
   dst.Save(output,ImageFormat.Png);
  }
 }
}
'@
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$backup = Join-Path $PSScriptRoot 'LeafWalk-originals'
$generated = Join-Path $PSScriptRoot 'LeafWalk-generated'
New-Item -ItemType Directory -Force -Path $backup,$generated | Out-Null
foreach($t in '01','02','03') {
    Get-ChildItem (Join-Path $root "Assets/Sprites/Enemies/Leaf/Leaf$t") -Filter '*Move*.png' | ForEach-Object {
        $copy = Join-Path $backup $_.Name
        if (!(Test-Path $copy)) { Copy-Item -LiteralPath $_.FullName -Destination $copy }
    }
}
$items = Get-Content $Manifest -Raw | ConvertFrom-Json
foreach($item in $items) {
    $name = "Leaf$($item.type)_Move_$($item.frame).png"
    $raw = Join-Path $generated $name
    Copy-Item -LiteralPath $item.path -Destination $raw
    $ready = Join-Path $generated ("ready-"+$name)
    [LeafFrameTools]::Fit($raw,(Join-Path $backup $name),(Join-Path $backup "Leaf$($item.type)_Move_01.png"),$ready)
    Copy-Item -LiteralPath $ready -Destination (Join-Path $root "Assets/Sprites/Enemies/Leaf/Leaf$($item.type)/$name")
}
[LeafFrameTools]::Preview($root,$backup,(Join-Path $PSScriptRoot 'LeafWalk-before-after.png'))
