$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;using System.IO;using System.Text;using System.Globalization;using System.Drawing;using System.Drawing.Imaging;using System.Drawing.Drawing2D;using System.Collections.Generic;
public static class GolemPartsPack {
 public class Part { public string Name;public Bitmap Image;public Rectangle Art;public PointF Pivot;public PointF Anchor;public int Order; }
 static Rectangle Bounds(Bitmap b){int l=b.Width,t=b.Height,r=-1,d=-1;for(int y=0;y<b.Height;y++)for(int x=0;x<b.Width;x++)if(b.GetPixel(x,y).A>=16){l=Math.Min(l,x);t=Math.Min(t,y);r=Math.Max(r,x);d=Math.Max(d,y);}if(r<0)throw new Exception("Empty part");return Rectangle.FromLTRB(l,t,r+1,d+1);}
 static Part Pack(string root,string name,int width,int height,double maxW,double maxH){
  using(var src=new Bitmap(Path.Combine(root,"Source",name+".png"))){var b=Bounds(src);double s=Math.Min(maxW/b.Width,maxH/b.Height);int w=(int)Math.Round(b.Width*s),h=(int)Math.Round(b.Height*s);int x=(width-w)/2,y=(height-h)/2;var dst=new Bitmap(width,height,PixelFormat.Format32bppArgb);
   using(var g=Graphics.FromImage(dst)){g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;g.DrawImage(src,new Rectangle(x,y,w,h),b,GraphicsUnit.Pixel);}
   var a=Bounds(dst);if(a.Left<4||a.Top<4||a.Right>width-4||a.Bottom>height-4)throw new Exception("Part clipped: "+name);
   for(int xx=0;xx<width;xx++)if(dst.GetPixel(xx,0).A!=0||dst.GetPixel(xx,height-1).A!=0)throw new Exception("Nontransparent edge: "+name);
   for(int yy=0;yy<height;yy++)if(dst.GetPixel(0,yy).A!=0||dst.GetPixel(width-1,yy).A!=0)throw new Exception("Nontransparent edge: "+name);
   Console.WriteLine(name+": "+width+"x"+height+" art="+a);return new Part{Name=name,Image=dst,Art=a};
  }
 }
 static string N(double n){return n.ToString("0.######",CultureInfo.InvariantCulture);}
 static void Draw(Graphics g,Part p,float angle){var state=g.Save();g.TranslateTransform(p.Anchor.X+256,p.Anchor.Y+256);g.RotateTransform(angle);g.DrawImage(p.Image,-p.Pivot.X,-p.Pivot.Y,p.Image.Width,p.Image.Height);g.Restore(state);}
 static void Preview(string root,List<Part> parts,bool raised){using(var canvas=new Bitmap(2048,2560,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(canvas)){
  g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
  foreach(var p in parts)Draw(g,p,raised&&p.Name=="Arm_Left"?160:0);
  string name=raised?"Assembled_ArmRaised":"Assembled_Neutral";
  canvas.Save(Path.Combine(root,"Preview",name+".png"),ImageFormat.Png);
  using(var matte=new Bitmap(2048,2560,PixelFormat.Format32bppArgb))using(var mg=Graphics.FromImage(matte)){mg.Clear(Color.FromArgb(218,229,227));mg.DrawImageUnscaled(canvas,0,0);matte.Save(Path.Combine(root,"Preview",name+"_Matte.png"),ImageFormat.Png);}
 }}
 public static void Run(string root){
  const float center=768;
  var body=Pack(root,"Body",768,1536,640,1376);float ground=660+body.Art.Height;body.Pivot=new PointF(384,body.Art.Bottom);body.Anchor=new PointF(center,ground);body.Order=0;
  float bodyTop=ground-body.Art.Height;float shoulderY=bodyTop+body.Art.Height*0.3f;
  float shoulderL=center-body.Art.Width*0.44f,shoulderR=center+body.Art.Width*0.44f;
  var left=Pack(root,"Arm_Left",576,1344,448,(ground-shoulderY)/0.85);left.Pivot=new PointF(left.Art.Left+left.Art.Width*0.85f,left.Art.Top+left.Art.Height*0.15f);left.Anchor=new PointF(shoulderL,shoulderY);left.Order=1;
  var right=Pack(root,"Arm_Right",576,1344,448,(ground-shoulderY)/0.84);right.Pivot=new PointF(right.Art.Left+right.Art.Width*0.12f,right.Art.Top+right.Art.Height*0.16f);right.Anchor=new PointF(shoulderR,shoulderY);right.Order=1;
  var arch=Pack(root,"Arch",1152,1152,1040,920);arch.Pivot=new PointF(576,576);arch.Anchor=new PointF(center,550);arch.Order=-2;
  var runeL=Pack(root,"Rune_Left",256,512,160,320);runeL.Pivot=new PointF(128,256);runeL.Anchor=new PointF(220,470);runeL.Order=2;
  var runeR=Pack(root,"Rune_Right",256,512,160,320);runeR.Pivot=new PointF(128,256);runeR.Anchor=new PointF(1316,470);runeR.Order=2;
  var parts=new List<Part>{arch,body,left,right,runeL,runeR};
  try{
   var sb=new StringBuilder("{\n  \"pixelsPerUnit\": 256,\n  \"root\": \"Body base center\",\n  \"previewCanvas\": [2048,2560],\n  \"parts\": [\n");
   for(int i=0;i<parts.Count;i++){var p=parts[i];p.Image.Save(Path.Combine(root,"Parts","Golem_"+p.Name+".png"),ImageFormat.Png);
    sb.Append("    {\"name\":\"").Append(p.Name).Append("\",\"file\":\"Parts/Golem_").Append(p.Name).Append(".png\",\"size\":[").Append(p.Image.Width).Append(',').Append(p.Image.Height).Append("],\"pivotBottomLeft\":[").Append(N(p.Pivot.X/p.Image.Width)).Append(',').Append(N(1-p.Pivot.Y/p.Image.Height)).Append("],\"localPositionUnits\":[").Append(N((p.Anchor.X-center)/256)).Append(',').Append(N((ground-p.Anchor.Y)/256)).Append("],\"sortingOrder\":").Append(p.Order).Append('}');if(i<parts.Count-1)sb.Append(',');sb.Append('\n');
   }sb.Append("  ]\n}\n");File.WriteAllText(Path.Combine(root,"Assembly.json"),sb.ToString());
   Preview(root,parts,false);Preview(root,parts,true);
   using(var plate=new Bitmap(1280,800,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(plate)){
    g.Clear(Color.FromArgb(218,229,227));using(var a=new Bitmap(Path.Combine(root,"Preview","Assembled_Neutral.png")))g.DrawImage(a,new Rectangle(0,0,640,800));using(var a=new Bitmap(Path.Combine(root,"Preview","Assembled_ArmRaised.png")))g.DrawImage(a,new Rectangle(640,0,640,800));plate.Save(Path.Combine(root,"Preview","Assembly_Comparison.png"),ImageFormat.Png);
   }
  }finally{foreach(var p in parts)p.Image.Dispose();}
 }
}
'@
[GolemPartsPack]::Run($PSScriptRoot)
