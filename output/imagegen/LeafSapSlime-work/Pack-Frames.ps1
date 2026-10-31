param([Parameter(Mandatory=$true)][string]$Sheet)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;using System.Drawing;using System.Drawing.Imaging;using System.Drawing.Drawing2D;
public static class LeafSapFrames{
 public static Rectangle Bounds(Bitmap b){int l=b.Width,t=b.Height,r=-1,d=-1;for(int y=0;y<b.Height;y++)for(int x=0;x<b.Width;x++)if(b.GetPixel(x,y).A>=16){l=Math.Min(l,x);t=Math.Min(t,y);r=Math.Max(r,x);d=Math.Max(d,y);}if(r<0)throw new Exception("Empty sprite");return Rectangle.FromLTRB(l,t,r+1,d+1);}
 public static void Save(Bitmap input,Rectangle rect,RectangleF dest,string output){
  using(var dst=new Bitmap(640,640,PixelFormat.Format32bppArgb)){
   using(var g=Graphics.FromImage(dst)){g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;g.DrawImage(input,dest,rect,GraphicsUnit.Pixel);}
   var b=Bounds(dst);if(b.Left<2||b.Top<2||b.Right>638||b.Bottom>638)throw new Exception("Clipped: "+output+" "+b);
   for(int p=0;p<640;p++)if(dst.GetPixel(p,0).A!=0||dst.GetPixel(p,639).A!=0||dst.GetPixel(0,p).A!=0||dst.GetPixel(639,p).A!=0)throw new Exception("Nontransparent border: "+output);
   dst.Save(output,ImageFormat.Png);Console.WriteLine(System.IO.Path.GetFileName(output)+" 640x640 RGBA "+b);
  }
 }
 public static void Run(string sheet,string reference,string folder,string preview){
  using(var src=new Bitmap(sheet))using(var original=new Bitmap(reference)){
   int cw=src.Width/3,cut=(int)Math.Round(src.Height*0.547);var cells=new Bitmap[6];var bounds=new Rectangle[6];var rb=Bounds(original);
   try{
    for(int i=0;i<6;i++){int y=i<3?0:cut;int h=i<3?cut:src.Height-cut;cells[i]=src.Clone(new Rectangle(i%3*cw,y,cw,h),PixelFormat.Format32bppArgb);bounds[i]=Bounds(cells[i]);}
    double originalScale=400.0/rb.Width;
    double sx=400.0/bounds[0].Width,sy=rb.Height*originalScale/bounds[0].Height;
    Save(original,rb,new RectangleF(120,(float)(610-rb.Height*originalScale),400,(float)(rb.Height*originalScale)),System.IO.Path.Combine(folder,"LeafSapSlime_Idle_01.png"));
    for(int i=1;i<6;i++){
     double frameSx=i<3?400.0*(i==1?1.03:0.97)/bounds[i].Width:sx;
     double frameSy=i<3?rb.Height*originalScale*(i==1?0.95:1.05)/bounds[i].Height:sy;
     double ax=i<3?bounds[i].Left+bounds[i].Width*0.5:bounds[3].Left+bounds[3].Width*0.5;
     double ay=i<3?bounds[i].Bottom:bounds[3].Bottom;
     string name="LeafSapSlime_"+(i<3?"Idle_":"Death_")+((i%3)+1).ToString("00")+".png";
     Save(cells[i],new Rectangle(0,0,cw,cells[i].Height),new RectangleF((float)(320-ax*frameSx),(float)(610-ay*frameSy),(float)(cw*frameSx),(float)(cells[i].Height*frameSy)),System.IO.Path.Combine(folder,name));
    }
    using(var contact=new Bitmap(960,640,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(contact)){
     g.Clear(Color.FromArgb(222,232,231));for(int i=0;i<6;i++){
      string name="LeafSapSlime_"+(i<3?"Idle_":"Death_")+((i%3)+1).ToString("00")+".png";
      using(var b=new Bitmap(System.IO.Path.Combine(folder,name)))g.DrawImage(b,new Rectangle(i%3*320,i/3*320,320,320));
     }contact.Save(preview,ImageFormat.Png);
    }
   }finally{foreach(var b in cells)if(b!=null)b.Dispose();}
  }
 }
}
'@
$root=Split-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) -Parent
$folder=Join-Path $root 'output/imagegen/LeafSapSlime-PNG'
[LeafSapFrames]::Run((Resolve-Path $Sheet).Path,'C:\Users\Ayumi\AppData\Local\Temp\codex-clipboard-e3399cc9-9beb-4652-911f-f809f1b3576d.png',$folder,(Join-Path $PSScriptRoot 'Preview.png'))
