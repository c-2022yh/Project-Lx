param([Parameter(Mandatory=$true)][string]$Sheet)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System; using System.Drawing; using System.Drawing.Imaging; using System.Drawing.Drawing2D;
public static class SapSlimePack {
 public static Rectangle Bounds(Bitmap b) {
  int l=b.Width,t=b.Height,r=-1,d=-1;
  for(int y=0;y<b.Height;y++)for(int x=0;x<b.Width;x++)if(b.GetPixel(x,y).A>=16){l=Math.Min(l,x);t=Math.Min(t,y);r=Math.Max(r,x);d=Math.Max(d,y);}
  if(r<0)throw new Exception("Empty frame");return Rectangle.FromLTRB(l,t,r+1,d+1);
 }
 public static void Pack(string sheet,string dest) {
  using(var src=new Bitmap(sheet)) {
   int cw=src.Width/3,ch=src.Height/2;var cells=new Bitmap[6];var bounds=new Rectangle[6];
   try {
    for(int i=0;i<6;i++){cells[i]=src.Clone(new Rectangle(i%3*cw,i/3*ch,cw,ch),PixelFormat.Format32bppArgb);bounds[i]=Bounds(cells[i]);}
    double scale=400.0/bounds[0].Width;
    for(int i=0;i<6;i++)using(var dst=new Bitmap(640,640,PixelFormat.Format32bppArgb)) {
     double ax=i<3?bounds[i].Left+bounds[i].Width*0.5:bounds[3].Left+bounds[3].Width*0.5;
     double ay=i<3?bounds[i].Bottom:bounds[3].Bottom;
     using(var g=Graphics.FromImage(dst)) {
      g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
      g.DrawImage(cells[i],new RectangleF((float)(320-ax*scale),(float)(610-ay*scale),(float)(cw*scale),(float)(ch*scale)),new RectangleF(0,0,cw,ch),GraphicsUnit.Pixel);
     }
     string name="SapSlime_"+(i<3?"Idle_":"Death_")+((i%3)+1)+".png";
     var b=Bounds(dst);
     if(b.Left<2||b.Top<2||b.Right>638||b.Bottom>638)throw new Exception("Frame may be clipped: "+name+" "+b);
     if(dst.GetPixel(0,0).A!=0)throw new Exception("Nontransparent corner: "+name);
     dst.Save(System.IO.Path.Combine(dest,name),ImageFormat.Png);
     Console.WriteLine(name+" 640x640 alpha bounds="+b);
    }
   } finally {foreach(var c in cells)if(c!=null)c.Dispose();}
  }
 }
}
'@
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$dest = Join-Path $root 'Assets/Sprites/Enemies/SapSlime'
New-Item -ItemType Directory -Force -Path $dest | Out-Null
[SapSlimePack]::Pack((Resolve-Path $Sheet).Path,$dest)
$template = Get-Content (Join-Path $root 'Assets/Sprites/Enemies/Slime/Slime_Idle_1.png.meta') -Raw
$template = $template -replace '(?ms)  internalIDToNameTable:.*?  externalObjects:', "  internalIDToNameTable: []`n  externalObjects:"
$template = $template -replace '(?ms)  spriteSheet:.*?  mipmapLimitGroupName:', "  spriteSheet:`n    serializedVersion: 2`n    sprites: []`n    outline: []`n    physicsShape: []`n    bones: []`n    spriteID: 5e97eb03825dee720800000000000000`n    internalID: 0`n    vertices: []`n    indices: `n    edges: []`n    weights: []`n    secondaryTextures: []`n    nameFileIdTable: {}`n  mipmapLimitGroupName:"
$template = $template -replace 'textureCompression: 1','textureCompression: 0'
$prefab = Get-Content (Join-Path $root 'Assets/Prefabs/Enemies/Enemy_Slime.prefab') -Raw
foreach($state in 'Idle','Death') { foreach($index in 1..3) {
    $file = Join-Path $dest "SapSlime_${state}_$index.png"
    $metaPath = $file+'.meta'
    if (!(Test-Path $metaPath)) {
        $meta = $template -replace '(?m)^guid: .*$',('guid: '+[guid]::NewGuid().ToString('N'))
        [System.IO.File]::WriteAllText($metaPath,$meta)
    }
    $oldGuid = [regex]::Match((Get-Content (Join-Path $root "Assets/Sprites/Enemies/Slime/Slime_${state}_$index.png.meta") -Raw),'(?m)^guid: (\w+)').Groups[1].Value
    $newGuid = [regex]::Match((Get-Content $metaPath -Raw),'(?m)^guid: (\w+)').Groups[1].Value
    $prefab = $prefab.Replace($oldGuid,$newGuid)
} }
$prefab = $prefab.Replace('m_Name: Enemy_Slime','m_Name: Enemy_SapSlime').Replace('spriteFacesRight: 0','spriteFacesRight: 1')
$prefabPath = Join-Path $root 'Assets/Prefabs/Enemies/Enemy_SapSlime.prefab'
if(Test-Path $prefabPath){throw 'Refusing to overwrite existing SapSlime prefab.'}
[System.IO.File]::WriteAllText($prefabPath,$prefab)
$prefabMeta = (Get-Content (Join-Path $root 'Assets/Prefabs/Enemies/Enemy_Slime.prefab.meta') -Raw) -replace '(?m)^guid: .*$',('guid: '+[guid]::NewGuid().ToString('N'))
[System.IO.File]::WriteAllText(($prefabPath+'.meta'),$prefabMeta)
$folderMeta = "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))`nfolderAsset: yes`nDefaultImporter:`n  externalObjects: {}`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n"
if(!(Test-Path ($dest+'.meta'))){[System.IO.File]::WriteAllText(($dest+'.meta'),$folderMeta)}
