$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;using System.Drawing;using System.Drawing.Imaging;using System.Drawing.Drawing2D;
public static class ConeProjectilePng{
 public static void Run(string source,string output){using(var src=new Bitmap(source))using(var dst=new Bitmap(128,128,PixelFormat.Format32bppArgb)){
  int l=src.Width,t=src.Height,r=-1,b=-1;for(int y=0;y<src.Height;y++)for(int x=0;x<src.Width;x++)if(src.GetPixel(x,y).A>=16){l=Math.Min(l,x);t=Math.Min(t,y);r=Math.Max(r,x);b=Math.Max(b,y);}
  if(r<0)throw new Exception("Empty source");var rect=Rectangle.FromLTRB(l,t,r+1,b+1);float width=96,height=96f*rect.Height/rect.Width;
  using(var g=Graphics.FromImage(dst)){g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;g.DrawImage(src,new RectangleF(16,64-height/2,width,height),rect,GraphicsUnit.Pixel);}
  for(int p=0;p<128;p++)if(dst.GetPixel(p,0).A!=0||dst.GetPixel(p,127).A!=0||dst.GetPixel(0,p).A!=0||dst.GetPixel(127,p).A!=0)throw new Exception("Opaque border");
  dst.Save(output,ImageFormat.Png);Console.WriteLine("128x128 RGBA; centered; artwork width 96px; transparent borders verified");
 }}
}
'@
$root=Split-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) -Parent
[ConeProjectilePng]::Run((Join-Path $PSScriptRoot 'Projectile_Source.png'),(Join-Path $root 'output/imagegen/Pinecone-SpritePNG/Pinecone_Projectile.png'))
