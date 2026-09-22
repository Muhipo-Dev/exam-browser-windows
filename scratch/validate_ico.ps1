Add-Type -AssemblyName System.Drawing
$fs = [System.IO.File]::OpenRead('Assets\AppIcon.ico')
$br = New-Object System.IO.BinaryReader($fs)

$reserved = $br.ReadUInt16()
$type = $br.ReadUInt16()
$count = $br.ReadUInt16()

Write-Host "Reserved=$reserved, Type=$type, Count=$count"

for ($i = 0; $i -lt $count; $i++) {
    $fs.Seek(6 + $i * 16, [System.IO.SeekOrigin]::Begin)
    $w = $br.ReadByte()
    $h = $br.ReadByte()
    $colors = $br.ReadByte()
    $res = $br.ReadByte()
    $planes = $br.ReadUInt16()
    $bpp = $br.ReadUInt16()
    $size = $br.ReadUInt32()
    $offset = $br.ReadUInt32()
    
    $fs.Seek($offset, [System.IO.SeekOrigin]::Begin)
    $magic = $br.ReadBytes(8)
    $isPng = ($magic[0] -eq 0x89 -and $magic[1] -eq 0x50 -and $magic[2] -eq 0x4E -and $magic[3] -eq 0x47)
    
    Write-Host ("Entry {0}: {1}x{2}, planes={3}, bpp={4}, size={5}, offset={6}, isPng={7}" -f $i, $w, $h, $planes, $bpp, $size, $offset, $isPng)
    if (-not $isPng) {
        $fs.Seek($offset, [System.IO.SeekOrigin]::Begin)
        $biSize = $br.ReadUInt32()
        $biW = $br.ReadInt32()
        $biH = $br.ReadInt32()
        $biPlanes = $br.ReadUInt16()
        $biBitCount = $br.ReadUInt16()
        Write-Host ("   DIB header: biSize={0}, biW={1}, biH={2}, biPlanes={3}, biBitCount={4}" -f $biSize, $biW, $biH, $biPlanes, $biBitCount)
    }
}

$fs.Close()

# Try loading with System.Drawing.Icon
try {
    $ico = New-Object System.Drawing.Icon('Assets\AppIcon.ico')
    Write-Host ("System.Drawing.Icon loaded successfully! Size: {0}x{1}" -f $ico.Width, $ico.Height)
    $ico.Dispose()
} catch {
    Write-Host "System.Drawing.Icon failed: $_"
}
