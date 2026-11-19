Add-Type -AssemblyName System.Drawing

$root = Join-Path (Get-Location).Path 'Assets\Phu\Sprite asset'
$configs = @(
  @{ Name='Velvet'; Folder='Velvet Chibi Animations'; Dance='velvet_chibi_dance_keys_16.png'; IdleMiss='velvet_chibi_idle_miss.png' },
  @{ Name='Brax'; Folder='Brax Chibi Animations'; Dance='brax_chibi_dance_keys_16.png'; IdleMiss='brax_chibi_idle_miss.png' },
  @{ Name='Maestro'; Folder='Maestro Chibi Animations'; Dance='maestro_chibi_dance_keys_16.png'; IdleMiss='maestro_chibi_idle_miss.png' }
)

function Save-Crop($bmp, $rect, $path) {
  $cell = New-Object System.Drawing.Bitmap $rect.Width, $rect.Height
  $g = [System.Drawing.Graphics]::FromImage($cell)
  $g.DrawImage($bmp, (New-Object System.Drawing.Rectangle 0,0,$rect.Width,$rect.Height), $rect, [System.Drawing.GraphicsUnit]::Pixel)
  $g.Dispose()
  $minX=$cell.Width; $minY=$cell.Height; $maxX=-1; $maxY=-1
  for ($yy=0; $yy -lt $cell.Height; $yy+=2) { for ($xx=0; $xx -lt $cell.Width; $xx+=2) { if ($cell.GetPixel($xx,$yy).A -gt 8) { $minX=[math]::Min($minX,$xx); $minY=[math]::Min($minY,$yy); $maxX=[math]::Max($maxX,$xx); $maxY=[math]::Max($maxY,$yy) } } }
  if ($maxX -lt 0) { $cell.Dispose(); return }
  $pad=4; $minX=[math]::Max(0,$minX-$pad); $minY=[math]::Max(0,$minY-$pad); $maxX=[math]::Min($cell.Width-1,$maxX+$pad); $maxY=[math]::Min($cell.Height-1,$maxY+$pad)
  $crop = New-Object System.Drawing.Bitmap ($maxX-$minX+1),($maxY-$minY+1)
  $gc=[System.Drawing.Graphics]::FromImage($crop); $gc.DrawImage($cell,(New-Object System.Drawing.Rectangle 0,0,$crop.Width,$crop.Height),(New-Object System.Drawing.Rectangle $minX,$minY,$crop.Width,$crop.Height),[System.Drawing.GraphicsUnit]::Pixel); $gc.Dispose(); $cell.Dispose()
  $targetH=600; $scale=$targetH/$crop.Height; $nw=[int]($crop.Width*$scale); $nh=[int]($crop.Height*$scale); if($nw -gt 504){$scale=504/$crop.Width;$nw=504;$nh=[int]($crop.Height*$scale)}
  $out=New-Object System.Drawing.Bitmap 512,640; $go=[System.Drawing.Graphics]::FromImage($out); $go.Clear([System.Drawing.Color]::Transparent); $go.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic; $ox=[int]((512-$nw)/2); $oy=[int](636-$nh); $dest=New-Object System.Drawing.Rectangle $ox,$oy,$nw,$nh; $go.DrawImage($crop,$dest); $go.Dispose(); $crop.Dispose(); $out.Save($path,[System.Drawing.Imaging.ImageFormat]::Png); $out.Dispose()
}

foreach ($cfg in $configs) {
  $base = Join-Path $root $cfg.Folder
  $srcDir = Join-Path $base 'Source Sheets'
  $danceDir = Join-Path $base 'Dance\Frames'
  $idleDir = Join-Path $base 'Idle\Frames'
  $missDir = Join-Path $base 'Miss\Frames'
  $idleKeysDir = Join-Path $base 'Idle\Keys'
  $missKeysDir = Join-Path $base 'Miss\Keys'
  foreach ($d in @($danceDir,$idleDir,$missDir,$idleKeysDir,$missKeysDir)) { if (-not (Test-Path -LiteralPath $d)) { New-Item -ItemType Directory -Path $d | Out-Null } }

  $danceBmp = [System.Drawing.Bitmap]::FromFile((Join-Path $srcDir $cfg.Dance))
  $cw = [int]($danceBmp.Width / 4); $ch = [int]($danceBmp.Height / 4)
  $keys = @()
  for ($i=0; $i -lt 16; $i++) {
    $x = ($i % 4) * $cw; $y = [int]([math]::Floor($i / 4)) * $ch
    $key = Join-Path $danceDir ("key_{0:D2}.png" -f ($i+1)); Save-Crop $danceBmp (New-Object System.Drawing.Rectangle $x,$y,$cw,$ch) $key
    $keys += [System.Drawing.Bitmap]::FromFile($key)
  }
  $danceBmp.Dispose()
  $frame = 1
  foreach ($key in $keys) { for ($hold=0; $hold -lt 7; $hold++) { if ($frame -le 120) { $key.Save((Join-Path $danceDir ("dance_{0:D3}.png" -f $frame)), [System.Drawing.Imaging.ImageFormat]::Png); $frame++ } } }
  for ($j=0; $j -lt 8; $j++) { $keys[$j].Save((Join-Path $danceDir ("dance_{0:D3}.png" -f $frame)), [System.Drawing.Imaging.ImageFormat]::Png); $frame++ }
  foreach ($key in $keys) { $key.Dispose() }

  $imBmp = [System.Drawing.Bitmap]::FromFile((Join-Path $srcDir $cfg.IdleMiss))
  $iw = [int]($imBmp.Width / 3); $ih = [int]($imBmp.Height / 2)
  for ($i=0; $i -lt 6; $i++) {
    $x = ($i % 3) * $iw; $y = [int]([math]::Floor($i / 3)) * $ih
    $target = if ($i -lt 3) { $idleDir } else { $missDir }
    $name = if ($i -lt 3) { 'idle' } else { 'miss' }
    $keyTarget = if ($i -lt 3) { $idleKeysDir } else { $missKeysDir }
    $path = Join-Path $keyTarget ("{0}_{1:D3}.png" -f $name,(($i % 3)+1)); Save-Crop $imBmp (New-Object System.Drawing.Rectangle $x,$y,$iw,$ih) $path
  }
  $imBmp.Dispose()
  for ($i=1; $i -le 3; $i++) { 1..8 | ForEach-Object { Copy-Item (Join-Path $idleKeysDir ("idle_{0:D3}.png" -f $i)) (Join-Path $idleDir ("idle_{0:D3}.png" -f (($i-1)*8 + $_))) -Force } }
  for ($i=1; $i -le 3; $i++) { 1..8 | ForEach-Object { Copy-Item (Join-Path $missKeysDir ("miss_{0:D3}.png" -f $i)) (Join-Path $missDir ("miss_{0:D3}.png" -f (($i-1)*8 + $_))) -Force } }
  Write-Output "$($cfg.Name): 120 dance, 24 idle, 24 miss frames"
}
