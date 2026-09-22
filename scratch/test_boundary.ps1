Add-Type -AssemblyName System.Drawing

$img = [System.Drawing.Bitmap]::FromFile('c:\exambro-windows\Assets\images.jpg')
$w = $img.Width
$h = $img.Height

# Let's inspect horizontal line through center
$y = [int]($h / 2)
Write-Host "Scanning row $y from left (0 to 100):"
for ($x = 0; $x -lt 80; $x++) {
    $c = $img.GetPixel($x, $y)
    if ($c.R -lt 240) {
        Write-Host ("At x={0}: R={1}, G={2}, B={3}" -f $x, $c.R, $c.G, $c.B)
    }
}
$img.Dispose()
