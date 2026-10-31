$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;using System.Drawing;using System.Drawing.Imaging;using System.Drawing.Drawing2D;
public static class ConeDeathPng {
 public static Rectangle Bounds(Bitmap b){int l=b.Width,t=b.Height,r=-1,d=-1;for(int y=0;y<b.Height;y++)for(int x=0;x<b.Width;x++)if(b.GetPixel(x,y).A>=16){l=Math.Min(l,x);t=Math.Min(t,y);r=Math.Max(r,x);d=Math.Max(d,y);}if(r<0)throw new Exception("Empty sprite");return Rectangle.FromLTRB(l,t,r+1,d+1);}
 public static void Run(string sheet,string reference,string dest,string preview){
  using(var src=new Bitmap(sheet))using(var idle=new Bitmap(reference)){
   if(src.Width%3!=0)throw new Exception("Sheet width is not divisible by 3");
   int cw=src.Width/3;var cells=new Bitmap[3];var bounds=new Rectangle[3];var rb=Bounds(idle);
   try{
    for(int i=0;i<3;i++){cells[i]=src.Clone(new Rectangle(i*cw,0,cw,src.Height),PixelFormat.Format32bppArgb);bounds[i]=Bounds(cells[i]);}
    double scale=(rb.Height*640.0/idle.Height)*0.9/bounds[0].Height;
    double baseline=rb.Bottom*640.0/idle.Height;
    using(var contact=new Bitmap(960,320,PixelFormat.Format32bppArgb))using(var pg=Graphics.FromImage(contact)){
     pg.Clear(Color.FromArgb(222,232,231));
     for(int i=0;i<3;i++)using(var dst=new Bitmap(640,640,PixelFormat.Format32bppArgb)){
      double ax=bounds[i].Left+bounds[i].Width*0.5;
      double ay=bounds[i].Bottom;
      using(var g=Graphics.FromImage(dst)){
       g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
       g.DrawImage(cells[i],new RectangleF((float)(320-ax*scale),(float)(baseline-ay*scale),(float)(cw*scale),(float)(src.Height*scale)),new RectangleF(0,0,cw,src.Height),GraphicsUnit.Pixel);
      }
      var b=Bounds(dst);if(b.Left<2||b.Top<2||b.Right>638||b.Bottom>638)throw new Exception("Clipped frame: "+i+" "+b);
      int edge=0;for(int x=0;x<640;x++)if(dst.GetPixel(x,0).A!=0||dst.GetPixel(x,639).A!=0)edge++;for(int y=0;y<640;y++)if(dst.GetPixel(0,y).A!=0||dst.GetPixel(639,y).A!=0)edge++;
      if(edge!=0)throw new Exception("Nontransparent border: "+i);
      string name="Pinecone_Death_0"+(i+1)+".png";dst.Save(System.IO.Path.Combine(dest,name),ImageFormat.Png);
      pg.DrawImage(dst,new Rectangle(i*320,0,320,320));
      Console.WriteLine(name+" 640x640 RGBA; "+b+"; transparent border verified");
     }
     contact.Save(preview,ImageFormat.Png);
    }
   }finally{foreach(var b in cells)if(b!=null)b.Dispose();}
  }
 }
}
'@
$dest=Join-Path (Split-Path $PSScriptRoot -Parent) 'PineconeDeath-PNG'
[ConeDeathPng]::Run((Join-Path $PSScriptRoot 'Death_Sheet.png'),(Join-Path $PSScriptRoot 'Approved_Idle.png'),$dest,(Join-Path $PSScriptRoot 'Preview.png'))
