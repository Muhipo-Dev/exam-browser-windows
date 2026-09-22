Add-Type -AssemblyName System.Drawing
$fs = [System.IO.File]::OpenRead('Assets\AppIcon.ico')
$br = New-Object System.IO.BinaryReader($fs)
$fs.Seek(4, [System.IO.SeekOrigin]::Begin)
$count = $br.ReadUInt16()
for ($i = 0; $i -lt $count; $i++) {
    $fs.Seek(6 + $i * 16, [System.IO.SeekOrigin]::Begin)
    $w = $br.ReadByte()
    $h = $br.ReadByte()
    $fs.Seek(6 + $i * 16 + 8, [System.IO.SeekOrigin]::Begin)
    $len = $br.ReadUInt32()
    $off = $br.ReadUInt32()
    $fs.Seek($off, [System.IO.SeekOrigin]::Begin)
    $bytes = $br.ReadBytes($len)
    $outName = "Assets\frame_{0}_{1}x{2}.png" -f $i, $w, $h
    [System.IO.File]::WriteAllBytes($outName, $bytes)
    Write-Host "Wrote frame $outName"
}
$fs.Close()
