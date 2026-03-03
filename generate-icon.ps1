# Gerar ícone .ico usando PowerShell + System.Drawing (sem ImageMagick)
# Converte lfb-logo.png para icon.ico com múltiplos tamanhos (16,32,48,64,128,256)

Add-Type -AssemblyName System.Drawing

$logoPath = "ControleMateriais.Desktop/Assets/lfb-logo.png"
$iconPath = "ControleMateriais.Desktop/Assets/icon.ico"

if (-not (Test-Path $logoPath)) {
    Write-Error "Logo não encontrado: $logoPath"
    exit 1
}

Write-Host "Gerando $iconPath a partir de $logoPath..."

# Carrega imagem original
$original = [System.Drawing.Image]::FromFile((Resolve-Path $logoPath))

# Tamanhos para o .ico
$sizes = @(256, 128, 64, 48, 32, 16)

$bitmaps = @()
foreach ($size in $sizes) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)
    # Centraliza e redimensiona mantendo aspecto
    $scale = [Math]::Min($size / $original.Width, $size / $original.Height)
    $w = [int]($original.Width * $scale)
    $h = [int]($original.Height * $scale)
    $x = ($size - $w) / 2
    $y = ($size - $h) / 2
    $g.DrawImage($original, $x, $y, $w, $h)
    $g.Dispose()
    $bitmaps += $bmp
}

# Cria IconWriter via reflexão (não há API nativa em .NET)
$iconStream = New-Object System.IO.FileStream($iconPath, [System.IO.FileMode]::Create)
$binaryWriter = New-Object System.IO.BinaryWriter($iconStream)

# Header .ico (6 bytes)
$binaryWriter.Write([UInt16]0)   # Reserved
$binaryWriter.Write([UInt16]1)   # Type (1=icon)
$binaryWriter.Write([UInt16]$bitmaps.Count) # ImageCount

# Image directory entries (16 bytes cada)
$imageOffset = 6 + $bitmaps.Count * 16
$offset = $imageOffset
foreach ($bmp in $bitmaps) {
    $binaryWriter.Write([Byte]0)   # Width (0 for 256)
    $binaryWriter.Write([Byte]0)   # Height (0 for 256)
    $binaryWriter.Write([Byte]0)   # ColorCount
    $binaryWriter.Write([Byte]0)   # Reserved
    $binaryWriter.Write([UInt16]1) # ColorPlanes
    $binaryWriter.Write([UInt16]32) # BitsPerPixel
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bytes = $ms.ToArray()
    $binaryWriter.Write([UInt32]$bytes.Length) # SizeInBytes
    $binaryWriter.Write([UInt32]$offset)       # ImageOffset
    $offset += $bytes.Length
    $ms.Dispose()
}

# Escreve cada imagem como PNG
foreach ($bmp in $bitmaps) {
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bytes = $ms.ToArray()
    $binaryWriter.Write($bytes)
    $ms.Dispose()
}

$binaryWriter.Close()
$iconStream.Close()

# Libera bitmaps
foreach ($bmp in $bitmaps) { $bmp.Dispose() }
$original.Dispose()

if (Test-Path $iconPath) {
    Write-Host "Ícone gerado com sucesso: $iconPath"
} else {
    Write-Error "Falha ao gerar ícone"
    exit 1
}
