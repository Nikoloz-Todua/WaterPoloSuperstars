[CmdletBinding()]
param()
Add-Type -AssemblyName System.Drawing
$target = Join-Path $PSScriptRoot '../../Assets/ProductionAthletes/Textures/CapNumbers.png'
$atlas = [Drawing.Bitmap]::new(512, 512, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics = [Drawing.Graphics]::FromImage($atlas)
$font = [Drawing.Font]::new('Arial', 42, [Drawing.FontStyle]::Bold, [Drawing.GraphicsUnit]::Pixel)
$format = [Drawing.StringFormat]::new()
$format.Alignment = [Drawing.StringAlignment]::Center
$format.LineAlignment = [Drawing.StringAlignment]::Center
try {
    $graphics.Clear([Drawing.Color]::FromArgb(0, 255, 255, 255))
    $graphics.TextRenderingHint = [Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    foreach ($number in 1..63) {
        $column = $number % 8
        $row = [math]::Floor($number / 8)
        $cell = [Drawing.RectangleF]::new($column * 64, $row * 64 - 1, 64, 64)
        $graphics.DrawString([string]$number, $font, [Drawing.Brushes]::White, $cell, $format)
    }
    $atlas.Save($target, [Drawing.Imaging.ImageFormat]::Png)
}
finally {
    $format.Dispose()
    $font.Dispose()
    $graphics.Dispose()
    $atlas.Dispose()
}
