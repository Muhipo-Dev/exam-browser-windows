# Scripts/generate_assets.ps1
# Script to generate brand assets for Exambro-Muhipo from official SMA Muhipo logo (Assets/images.jpg)
Add-Type -AssemblyName System.Drawing

$ErrorActionPreference = "Stop"

$jpgSource = "c:\exambro-windows\Assets\images.jpg"
if (-not (Test-Path $jpgSource)) {
    throw "Source logo not found at $jpgSource"
}

Write-Host "Processing source logo from $jpgSource..." -ForegroundColor Cyan

$src = [System.Drawing.Bitmap]::FromFile($jpgSource)
$w = $src.Width
$h = $src.Height

# Create 32-bit ARGB working bitmap
$bmp = New-Object System.Drawing.Bitmap($w, $h, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.DrawImage($src, 0, 0, $w, $h)
$g.Dispose()
$src.Dispose()

# 1. Flood fill to find all outer background pixels (white canvas)
$isBg = New-Object 'bool[,]' $w, $h
$queue = New-Object 'System.Collections.Generic.Queue[System.Drawing.Point]'

for ($x = 0; $x -lt $w; $x++) {
    $queue.Enqueue([System.Drawing.Point]::new($x, 0))
    $queue.Enqueue([System.Drawing.Point]::new($x, $h - 1))
}
for ($y = 0; $y -lt $h; $y++) {
    $queue.Enqueue([System.Drawing.Point]::new(0, $y))
    $queue.Enqueue([System.Drawing.Point]::new($w - 1, $y))
}

while ($queue.Count -gt 0) {
    $p = $queue.Dequeue()
    $x = $p.X
    $y = $p.Y
    if ($x -lt 0 -or $x -ge $w -or $y -lt 0 -or $y -ge $h) { continue }
    if ($isBg[$x, $y]) { continue }
    
    $c = $bmp.GetPixel($x, $y)
    # The black border has R,G,B < 100. The white background is > 210.
    if ($c.R -gt 210 -and $c.G -gt 210 -and $c.B -gt 210) {
        $isBg[$x, $y] = $true
        $queue.Enqueue([System.Drawing.Point]::new($x + 1, $y))
        $queue.Enqueue([System.Drawing.Point]::new($x - 1, $y))
        $queue.Enqueue([System.Drawing.Point]::new($x, $y + 1))
        $queue.Enqueue([System.Drawing.Point]::new($x, $y - 1))
    }
}

# 2. Defringe and apply anti-aliased alpha boundary
for ($y = 0; $y -lt $h; $y++) {
    for ($x = 0; $x -lt $w; $x++) {
        if ($isBg[$x, $y]) {
            $bmp.SetPixel($x, $y, [System.Drawing.Color]::Transparent)
        } else {
            # Check if this non-bg pixel touches background (edge pixel)
            $touchesBg = $false
            for ($dy = -1; $dy -le 1; $dy++) {
                for ($dx = -1; $dx -le 1; $dx++) {
                    $nx = $x + $dx
                    $ny = $y + $dy
                    if ($nx -ge 0 -and $nx -lt $w -and $ny -ge 0 -and $ny -lt $h) {
                        if ($isBg[$nx, $ny]) {
                            $touchesBg = $true
                            break
                        }
                    }
                }
                if ($touchesBg) { break }
            }
            
            if ($touchesBg) {
                $c = $bmp.GetPixel($x, $y)
                $avg = ($c.R + $c.G + $c.B) / 3.0
                if ($avg -gt 200) {
                    # Outer compression artifact on edge -> make transparent
                    $bmp.SetPixel($x, $y, [System.Drawing.Color]::Transparent)
                } elseif ($avg -gt 100) {
                    # Semi-edge: smooth blend to black border
                    $alpha = [int](255.0 * (1.0 - ($avg - 100.0) / 100.0))
                    $alpha = [System.Math]::Max(0, [System.Math]::Min(255, $alpha))
                    $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb($alpha, 15, 15, 15))
                }
            }
        }
    }
}

# 3. Generate High-Resolution SchoolLogo.png (512x512)
Write-Host "Generating SchoolLogo.png (512x512)..." -ForegroundColor Yellow
$logo512 = New-Object System.Drawing.Bitmap(512, 512, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$gLogo = [System.Drawing.Graphics]::FromImage($logo512)
$gLogo.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$gLogo.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$gLogo.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$gLogo.Clear([System.Drawing.Color]::Transparent)

# Fit into 496x496 centered
$scale = [System.Math]::Min(496.0 / $w, 496.0 / $h)
$nw = [int]($w * $scale)
$nh = [int]($h * $scale)
$nx = [int]((512 - $nw) / 2)
$ny = [int]((512 - $nh) / 2)

$gLogo.DrawImage($bmp, $nx, $ny, $nw, $nh)
$gLogo.Dispose()

$schoolLogoPath1 = "c:\exambro-windows\Assets\SchoolLogo.png"
$schoolLogoPath2 = "c:\exambro-windows\src\Exambro-Muhipo\Assets\SchoolLogo.png"

$logo512.Save($schoolLogoPath1, [System.Drawing.Imaging.ImageFormat]::Png)
Copy-Item $schoolLogoPath1 $schoolLogoPath2 -Force
$logo512.Dispose()
Write-Host "Saved SchoolLogo.png to Assets and src/Exambro-Muhipo/Assets." -ForegroundColor Green

# 4. Generate Multi-Resolution AppIcon.ico (256, 128, 64, 48, 32, 16)
Write-Host "Generating Multi-Resolution AppIcon.ico..." -ForegroundColor Yellow
$sizes = @(256, 128, 64, 48, 32, 16)
$iconBitmaps = @()

foreach ($sz in $sizes) {
    $iconBmp = New-Object System.Drawing.Bitmap($sz, $sz, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $gIcon = [System.Drawing.Graphics]::FromImage($iconBmp)
    $gIcon.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $gIcon.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $gIcon.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $gIcon.Clear([System.Drawing.Color]::Transparent)
    
    # 2px margin on larger sizes, 1px on small sizes
    $margin = if ($sz -gt 32) { 2 } elseif ($sz -gt 16) { 1 } else { 0 }
    $usable = $sz - ($margin * 2)
    $s = [System.Math]::Min([double]$usable / $w, [double]$usable / $h)
    $destW = [int]($w * $s)
    $destH = [int]($h * $s)
    $destX = [int](($sz - $destW) / 2)
    $destY = [int](($sz - $destH) / 2)
    
    $gIcon.DrawImage($bmp, $destX, $destY, $destW, $destH)
    $gIcon.Dispose()
    $iconBitmaps += $iconBmp
}
$bmp.Dispose()

function Convert-BitmapToDibBytes([System.Drawing.Bitmap]$b) {
    $bw_w = $b.Width
    $bw_h = $b.Height
    $ms = New-Object System.IO.MemoryStream
    $writer = New-Object System.IO.BinaryWriter($ms)
    
    # BITMAPINFOHEADER (40 bytes)
    $writer.Write([uint32]40)                 # biSize
    $writer.Write([int32]$bw_w)               # biWidth
    $writer.Write([int32]($bw_h * 2))         # biHeight (XOR + AND mask height)
    $writer.Write([uint16]1)                  # biPlanes
    $writer.Write([uint16]32)                 # biBitCount
    $writer.Write([uint32]0)                  # biCompression (BI_RGB)
    $writer.Write([uint32]($bw_w * $bw_h * 4))# biSizeImage
    $writer.Write([int32]0)                   # biXPelsPerMeter
    $writer.Write([int32]0)                   # biYPelsPerMeter
    $writer.Write([uint32]0)                  # biClrUsed
    $writer.Write([uint32]0)                  # biClrImportant
    
    # XOR mask (bottom-up BGRA)
    for ($y = $bw_h - 1; $y -ge 0; $y--) {
        for ($x = 0; $x -lt $bw_w; $x++) {
            $pixel = $b.GetPixel($x, $y)
            $writer.Write([byte]$pixel.B)
            $writer.Write([byte]$pixel.G)
            $writer.Write([byte]$pixel.R)
            $writer.Write([byte]$pixel.A)
        }
    }
    
    # AND mask (1bpp, row aligned to 4 bytes)
    $andRowBytes = [int]([System.Math]::Ceiling($bw_w / 32.0) * 4)
    for ($y = $bw_h - 1; $y -ge 0; $y--) {
        $row = New-Object byte[] $andRowBytes
        for ($x = 0; $x -lt $bw_w; $x++) {
            $pixel = $b.GetPixel($x, $y)
            if ($pixel.A -eq 0) {
                $byteIndex = [int][System.Math]::Floor($x / 8.0)
                $bitIndex = 7 - ($x % 8)
                $row[$byteIndex] = [byte]($row[$byteIndex] -bor (1 -shl $bitIndex))
            }
        }
        $writer.Write($row)
    }
    
    $writer.Flush()
    $bytes = $ms.ToArray()
    $writer.Close()
    $ms.Close()
    return $bytes
}

function Save-CompleteIcoFile($bitmapsList, $outputPath) {
    $fs = [System.IO.File]::Open($outputPath, [System.IO.FileMode]::Create)
    $bw = New-Object System.IO.BinaryWriter($fs)
    
    # ICONDIR
    $bw.Write([uint16]0) # Reserved
    $bw.Write([uint16]1) # Type: 1 = Icon
    $bw.Write([uint16]$bitmapsList.Count)
    
    $offset = 6 + (16 * $bitmapsList.Count)
    $dataStreams = New-Object 'System.Collections.Generic.List[byte[]]'
    
    foreach ($b in $bitmapsList) {
        if ($b.Width -ge 128) {
            # PNG format for >= 128
            $ms = New-Object System.IO.MemoryStream
            $b.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
            $dataStreams.Add($ms.ToArray())
            $ms.Dispose()
        } else {
            # 32bpp DIB format for <= 64 (standard Windows Explorer & Desktop compatible)
            $dibBytes = Convert-BitmapToDibBytes $b
            $dataStreams.Add($dibBytes)
        }
    }
    
    for ($i = 0; $i -lt $bitmapsList.Count; $i++) {
        $sz = $bitmapsList[$i].Width
        $bytes = $dataStreams[$i]
        
        $bw.Write([byte]($sz -band 0xFF)) # Width (0 = 256)
        $bw.Write([byte]($sz -band 0xFF)) # Height
        $bw.Write([byte]0) # Color count
        $bw.Write([byte]0) # Reserved
        $bw.Write([uint16]1) # Color planes
        $bw.Write([uint16]32) # Bits per pixel
        $bw.Write([uint32]$bytes.Length) # Image size
        $bw.Write([uint32]$offset) # Offset
        
        $offset += $bytes.Length
    }
    
    for ($i = 0; $i -lt $bitmapsList.Count; $i++) {
        $bytes = $dataStreams[$i]
        $bw.Write($bytes)
    }
    
    $bw.Flush()
    $bw.Close()
    $fs.Close()
}

$appIconPath1 = "c:\exambro-windows\Assets\AppIcon.ico"
$appIconPath2 = "c:\exambro-windows\src\Exambro-Muhipo\Assets\AppIcon.ico"

Save-CompleteIcoFile $iconBitmaps $appIconPath1
Copy-Item $appIconPath1 $appIconPath2 -Force

foreach ($b in $iconBitmaps) { $b.Dispose() }

Write-Host "Saved AppIcon.ico with multi-resolution DIB/PNG frames to Assets and src/Exambro-Muhipo/Assets!" -ForegroundColor Green
Write-Host "All assets generated successfully!" -ForegroundColor Green
