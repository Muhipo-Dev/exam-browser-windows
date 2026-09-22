Add-Type -AssemblyName System.Drawing

function Convert-BitmapToDibBytes([System.Drawing.Bitmap]$bmp) {
    $w = $bmp.Width
    $h = $bmp.Height
    
    $ms = New-Object System.IO.MemoryStream
    $bw = New-Object System.IO.BinaryWriter($ms)
    
    # BITMAPINFOHEADER (40 bytes)
    $bw.Write([uint32]40)              # biSize
    $bw.Write([int32]$w)               # biWidth
    $bw.Write([int32]($h * 2))         # biHeight (XOR + AND mask)
    $bw.Write([uint16]1)               # biPlanes
    $bw.Write([uint16]32)              # biBitCount
    $bw.Write([uint32]0)               # biCompression (BI_RGB)
    $bw.Write([uint32]($w * $h * 4))   # biSizeImage
    $bw.Write([int32]0)                # biXPelsPerMeter
    $bw.Write([int32]0)                # biYPelsPerMeter
    $bw.Write([uint32]0)               # biClrUsed
    $bw.Write([uint32]0)               # biClrImportant
    
    # XOR mask (bottom-up BGRA)
    for ($y = $h - 1; $y -ge 0; $y--) {
        for ($x = 0; $x -lt $w; $x++) {
            $pixel = $bmp.GetPixel($x, $y)
            $bw.Write([byte]$pixel.B)
            $bw.Write([byte]$pixel.G)
            $bw.Write([byte]$pixel.R)
            $bw.Write([byte]$pixel.A)
        }
    }
    
    # AND mask (1bpp, row aligned to 4 bytes)
    $andRowBytes = [int]([System.Math]::Ceiling($w / 32.0) * 4)
    for ($y = $h - 1; $y -ge 0; $y--) {
        $row = New-Object byte[] $andRowBytes
        for ($x = 0; $x -lt $w; $x++) {
            $pixel = $bmp.GetPixel($x, $y)
            if ($pixel.A -eq 0) {
                $byteIndex = [int]($x / 8)
                $bitIndex = 7 - ($x % 8)
                $row[$byteIndex] = $row[$byteIndex] -bor (1 -shl $bitIndex)
            }
        }
        $bw.Write($row)
    }
    
    $bw.Flush()
    $bytes = $ms.ToArray()
    $bw.Close()
    $ms.Close()
    return $bytes
}

Write-Host "Helper function defined."
