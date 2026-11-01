# Renders every banner and thumbnail variant into assets/images:
#
#   <banner|thumbnail>-<right|left>.webp                 still, as the SVG shows it
#   <banner|thumbnail>-<right|left>-animated.webp        logo draws, title fades in at the start
#   <banner|thumbnail>-<right|left>-animated-late.webp   logo draws, title fades in at the end
#
# right and left are the SVGs' "Layout: Strip right" and "Layout: Strip left" layers: which side
# the Deadman strip is on, with the title on the other.
#
#   ./scripts/banner/build.ps1
#   ./scripts/banner/build.ps1 -Video clip.mp4 -Start 22.3 -BannerFocus 75%
#
# -Video plays a clip behind the title of the animated ones, in place of the still background. It
# plays 3.2 seconds from -Start and slows to a stop, so pick a stretch that ends on a good frame.
# -BannerFocus and -ThumbnailFocus pick which band of the clip shows, as a CSS object-position y.
#
# Needs Node with Playwright and Python with Pillow: `npm i --no-save playwright` in scripts/banner,
# `pip install pillow`. -Python names the Python to use.
param(
    [string]$Video,
    [double]$Start = 0,
    [string]$BannerFocus = '50%',
    [string]$ThumbnailFocus = '50%',
    [string]$Python = 'python'
)

$ErrorActionPreference = 'Stop'

$Root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$Assets = Join-Path $Root 'assets'
$Out = Join-Path $Assets 'images'
$Logo = Join-Path $Assets 'logo-animated.svg'
$Frames = Join-Path ([System.IO.Path]::GetTempPath()) 'adaptive-arsenal-banner-frames'

foreach ($image in 'banner', 'thumbnail') {
    $focus = if ($image -eq 'banner') { $BannerFocus } else { $ThumbnailFocus }
    foreach ($layout in 'right', 'left') {
        $variants = [ordered]@{
            ''               = @('--static')
            '-animated'      = @('--title-at', '0.35')
            '-animated-late' = @()
        }
        foreach ($suffix in $variants.Keys) {
            $options = @('--layout', $layout) + $variants[$suffix]
            if ($Video -and $suffix) { $options += @('--video', $Video, '--start', $Start, '--focus', $focus) }

            if (Test-Path $Frames) { Remove-Item -Recurse -Force $Frames }
            node (Join-Path $PSScriptRoot 'render.mjs') (Join-Path $Assets "$image.svg") $Logo $Frames @options
            if ($LASTEXITCODE) { throw "render.mjs failed for $image-$layout$suffix" }
            & $Python (Join-Path $PSScriptRoot 'encode.py') $Frames (Join-Path $Out "$image-$layout$suffix.webp")
            if ($LASTEXITCODE) { throw "encode.py failed for $image-$layout$suffix" }
        }
    }
}
Remove-Item -Recurse -Force $Frames
