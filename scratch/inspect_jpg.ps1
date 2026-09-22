Add-Type -AssemblyName System.Drawing
$img = [System.Drawing.Bitmap]::FromFile('c:\exambro-windows\Assets\images.jpg')
Write-Host "Dimensions: $($img.Width) x $($img.Height)"
Write-Host "Pixel format: $($img.PixelFormat)"
$corners = @(
    $img.GetPixel(0, 0),
    $img.GetPixel($img.Width - 1, 0),
    $img.GetPixel(0, $img.Height - 1),
    $img.GetPixel($img.Width - 1, $img.Height - 1),
    $img.GetPixel([int]($img.Width / 2), 2)
)
foreach ($c in $corners) {
    Write-Host "Pixel: R=$($c.R), G=$($c.G), B=$($c.B), A=$($c.A)"
}
$img.Dispose()
