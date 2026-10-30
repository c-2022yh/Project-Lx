Add-Type -AssemblyName System.Drawing
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$dest = Join-Path $root 'Assets/Sprites/Enemies/MothWisp'
New-Item -ItemType Directory -Path $dest -Force | Out-Null
$source = [System.Drawing.Bitmap]::new((Join-Path $PSScriptRoot 'Moth_Wisp_SpriteSheet_v1.png'))
# Source rectangles are deliberately not uniform: generated wings cross grid boundaries.
# Anchors identify the body center, keeping wing motion separate from pivot motion.
$frames = @(
    @('Idle_01',0,0,500,350,320,240),
    @('Idle_02',500,0,380,350,735,225),
    @('Idle_03',880,0,427,350,1140,245),
    @('Prepare_01',0,350,500,265,320,515),
    @('Prepare_02',500,350,380,265,735,500),
    @('Prepare_03',880,350,427,265,1140,525),
    @('Dash_01',0,615,500,285,390,800),
    @('Dash_02',500,615,380,285,805,820),
    @('Dash_03',880,615,427,285,1220,820),
    @('Death_01',0,900,500,303,320,1070),
    @('Death_02',500,900,380,303,715,1070),
    @('Death_03',880,900,427,303,1100,1070)
)
$template = Get-Content (Join-Path $root 'Assets/Sprites/Enemies/Wisp/Wisp_Idle_01.png.meta') -Raw
$template = $template -replace '(?ms)  internalIDToNameTable:.*?  externalObjects:', "  internalIDToNameTable: []`n  externalObjects:"
$template = $template -replace '(?ms)  spriteSheet:.*?  mipmapLimitGroupName:', "  spriteSheet:`n    serializedVersion: 2`n    sprites: []`n    outline: []`n    physicsShape: []`n    bones: []`n    spriteID: 5e97eb03825dee720800000000000000`n    internalID: 0`n    vertices: []`n    indices: `n    edges: []`n    weights: []`n    secondaryTextures: []`n    nameFileIdTable: {}`n  mipmapLimitGroupName:"
$template = $template -replace 'spritePixelsToUnits: 144','spritePixelsToUnits: 256'
$template = $template -replace 'textureCompression: 1','textureCompression: 0'
try {
    foreach ($f in $frames) {
        $name = 'MothWisp_' + $f[0] + '.png'
        $path = Join-Path $dest $name
        $canvas = [System.Drawing.Bitmap]::new(768,512,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $g = [System.Drawing.Graphics]::FromImage($canvas)
        try {
            $g.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
            $srcRect = [System.Drawing.Rectangle]::new($f[1],$f[2],$f[3],$f[4])
            $dstRect = [System.Drawing.Rectangle]::new((384+$f[1]-$f[5]),(256+$f[2]-$f[6]),$f[3],$f[4])
            $g.DrawImage($source,$dstRect,$srcRect,[System.Drawing.GraphicsUnit]::Pixel)
            $canvas.Save($path,[System.Drawing.Imaging.ImageFormat]::Png)
        } finally { $g.Dispose(); $canvas.Dispose() }
        if (!(Test-Path ($path+'.meta'))) {
            $meta = $template -replace '(?m)^guid: .*$',('guid: '+[guid]::NewGuid().ToString('N'))
            [System.IO.File]::WriteAllText(($path+'.meta'),$meta)
        }
        Write-Output $name
    }
} finally { $source.Dispose() }
