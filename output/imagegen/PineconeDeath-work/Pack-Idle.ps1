param([string]$Inhale,[string]$Exhale)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;using System.Drawing;using System.Drawing.Imaging;using System.Drawing.Drawing2D;
public static class PineIdlePack{
 public static Rectangle Bounds(Bitmap b){int l=b.Width,t=b.Height,r=-1,d=-1;for(int y=0;y<b.Height;y++)for(int x=0;x<b.Width;x++)if(b.GetPixel(x,y).A>=16){l=Math.Min(l,x);t=Math.Min(t,y);r=Math.Max(r,x);d=Math.Max(d,y);}if(r<0)throw new Exception("Empty frame");return Rectangle.FromLTRB(l,t,r+1,d+1);}
 public static void Fit(string input,string reference,string output,double factor){
  using(var src=new Bitmap(input))using(var idle=new Bitmap(reference))using(var dst=new Bitmap(640,640,PixelFormat.Format32bppArgb)){
   var a=Bounds(src);var b=Bounds(idle);int height=(int)Math.Round(b.Height*factor);var target=new Rectangle(b.Left,b.Bottom-height,b.Width,height);
   using(var g=Graphics.FromImage(dst)){g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;g.DrawImage(src,target,a,GraphicsUnit.Pixel);}
   var bounds=Bounds(dst);if(bounds.Left<2||bounds.Top<2||bounds.Right>638||bounds.Bottom>638)throw new Exception("Clipped frame");
   for(int p=0;p<640;p++)if(dst.GetPixel(p,0).A!=0||dst.GetPixel(p,639).A!=0||dst.GetPixel(0,p).A!=0||dst.GetPixel(639,p).A!=0)throw new Exception("Nontransparent border");
   dst.Save(output,ImageFormat.Png);Console.WriteLine(System.IO.Path.GetFileName(output)+" "+bounds+" transparent border OK");
  }
 }
 public static void Preview(string folder,string path){using(var canvas=new Bitmap(960,320,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(canvas)){g.Clear(Color.FromArgb(222,232,231));for(int i=1;i<=3;i++)using(var b=new Bitmap(System.IO.Path.Combine(folder,"Pinecone_Idle_0"+i+".png")))g.DrawImage(b,new Rectangle((i-1)*320,0,320,320));canvas.Save(path,ImageFormat.Png);}}
}
'@
$root=Split-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) -Parent
$folder=Join-Path $root 'output/imagegen/Pinecone-SpritePNG'
$reference=Join-Path $folder 'Pinecone_Idle.png'
Copy-Item -LiteralPath $reference -Destination (Join-Path $folder 'Pinecone_Idle_01.png')
[PineIdlePack]::Fit($Inhale,$reference,(Join-Path $folder 'Pinecone_Idle_02.png'),1.02)
[PineIdlePack]::Fit($Exhale,$reference,(Join-Path $folder 'Pinecone_Idle_03.png'),0.98)
[PineIdlePack]::Preview($folder,(Join-Path $PSScriptRoot 'IdleFrames-Preview.png'))
