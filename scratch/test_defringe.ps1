Add-Type -AssemblyName System.Drawing

$src = [System.Drawing.Bitmap]::FromFile('c:\exambro-windows\Assets\images.jpg')
$w = $src.Width
$h = $src.Height

$bmp = New-Object System.Drawing.Bitmap($w, $h, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.DrawImage($src, 0, 0, $w, $h)
$g.Dispose()
$src.Dispose()

# 1. Flood fill to find all outer background pixels
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

# 2. Apply transparency & defringe boundary pixels
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
                    # Mostly white compression artifact on edge -> make transparent
                    $bmp.SetPixel($x, $y, [System.Drawing.Color]::Transparent)
                } elseif ($avg -gt 100) {
                    # Semi-edge: smooth blend to black border
                    $alpha = [int](255 * (1.0 - ($avg - 100) / 100.0))
                    $alpha = [System.Math]::Clamp($alpha, 0, 255)
                    $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb($alpha, 15, 15, 15))
                }
            }
        }
    }
}

# 3. Test on dark navy background
$testPreview = New-Object System.Drawing.Bitmap(512, 512, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$gTest = [System.Drawing.Graphics]::FromImage($testPreview)
$gTest.Clear([System.Drawing.Color]::FromArgb(255, 11, 19, 43)) # App background #0B132B
$gTest.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$gTest.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$gTest.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

$scale = [System.Math]::Min(480.0 / $w, 480.0 / $h)
$destW = [int]($w * $scale)
$destH = [int]($h * $scale)
$destX = [int]((512 - $destW) / 2)
$destY = [int]((512 - $destH) / 2)

$gTest.DrawImage($bmp, $destX, $destY, $destW, $destH)
$gTest.Dispose()

$testPreview.Save('c:\exambro-windows\Assets\defringed_on_dark.png', [System.Drawing.Imaging.ImageFormat]::Png)
$testPreview.Dispose()

$bmp.Save('c:\exambro-windows\Assets\defringed_transparent.png', [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
Write-Host "Defringed test images generated!"
