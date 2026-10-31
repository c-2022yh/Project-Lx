$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;using System.Drawing;using System.Drawing.Imaging;using System.Drawing.Drawing2D;
public static class ConeBodyFrames {
 public static Rectangle Bounds(Bitmap b){int l=b.Width,t=b.Height,r=-1,d=-1;for(int y=0;y<b.Height;y++)for(int x=0;x<b.Width;x++)if(b.GetPixel(x,y).A>=16){l=Math.Min(l,x);t=Math.Min(t,y);r=Math.Max(r,x);d=Math.Max(d,y);}if(r<0)throw new Exception("Empty sprite");return Rectangle.FromLTRB(l,t,r+1,d+1);}
 public static double BaseX(Bitmap b,Rectangle bounds){int l=b.Width,r=-1;for(int y=Math.Max(bounds.Top,bounds.Bottom-28);y<bounds.Bottom;y++)for(int x=0;x<b.Width;x++)if(b.GetPixel(x,y).A>=16){l=Math.Min(l,x);r=Math.Max(r,x);}return(l+r+1)*0.5;}
 public static void Run(string sheet,string dest,string preview){
  using(var src=new Bitmap(sheet)){
   int cw=src.Width/2,ch=src.Height/2;string[] names={"Idle","Prepare","Fire","Recovery"};
   var cells=new Bitmap[4];var bounds=new Rectangle[4];
   try{
    for(int i=0;i<4;i++){cells[i]=src.Clone(new Rectangle(i%2*cw,i/2*ch,cw,ch),PixelFormat.Format32bppArgb);bounds[i]=Bounds(cells[i]);}
    double scale=640.0/cw;double baseline=bounds[0].Bottom*scale;
    using(var contact=new Bitmap(1280,320,PixelFormat.Format32bppArgb))using(var pg=Graphics.FromImage(contact)){
     pg.Clear(Color.FromArgb(222,232,231));
     for(int i=0;i<4;i++)using(var dst=new Bitmap(640,640,PixelFormat.Format32bppArgb)){
      double x=320-BaseX(cells[i],bounds[i])*scale,y=baseline-bounds[i].Bottom*scale;
      using(var g=Graphics.FromImage(dst)){
       g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
       g.DrawImage(cells[i],new RectangleF((float)x,(float)y,(float)(cw*scale),(float)(ch*scale)),new RectangleF(0,0,cw,ch),GraphicsUnit.Pixel);
      }
      for(int yy=0;yy<640;yy++)for(int xx=0;xx<640;xx++){var p=dst.GetPixel(xx,yy);if(p.A>0&&p.A<16)dst.SetPixel(xx,yy,Color.FromArgb(0,p.R,p.G,p.B));}
      var b=Bounds(dst);if(b.Left<2||b.Top<2||b.Right>638||b.Bottom>638)throw new Exception("Clipped frame: "+i+" "+b);
      int edge=0;for(int a=0;a<640;a++)if(dst.GetPixel(a,0).A!=0||dst.GetPixel(a,639).A!=0||dst.GetPixel(0,a).A!=0||dst.GetPixel(639,a).A!=0)edge++;
      if(edge!=0)throw new Exception("Nontransparent edge");
      string name="Pinecone_"+names[i]+".png";dst.Save(System.IO.Path.Combine(dest,name),ImageFormat.Png);
      pg.DrawImage(dst,new Rectangle(i*320,0,320,320));Console.WriteLine(name+" 640x640 RGBA; "+b);
     }
     contact.Save(preview,ImageFormat.Png);
    }
   }finally{foreach(var b in cells)if(b!=null)b.Dispose();}
  }
 }
}
'@
$root=Split-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) -Parent
$dest=Join-Path $root 'output/imagegen/Pinecone-SpritePNG'
New-Item -ItemType Directory -Force -Path $dest | Out-Null
[ConeBodyFrames]::Run('C:\Users\Ayumi\AppData\Local\Temp\codex-clipboard-e707d677-cc37-4ac2-916d-98189edf3f7b.png',$dest,(Join-Path $PSScriptRoot 'BodyFrames-Preview.png'))
Get-ChildItem (Join-Path $root 'output/imagegen/PineconeDeath-PNG') -Filter '*.png' | ForEach-Object {Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $dest $_.Name)}
