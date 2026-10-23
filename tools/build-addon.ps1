# Compiles the BlockingZones client files (bz.xml + bz.css) into YOUR existing client addon -
# the one that already carries the MenuManager / PMM panorama. Nothing else in the addon is touched.
#
#   build-addon.cmd                                   (asks for the addon name)
#   powershell -File build-addon.ps1 -Addon myaddon
#   powershell -File build-addon.ps1 -Addon myaddon -Cs2 "D:\SteamLibrary\steamapps\common\Counter-Strike Global Offensive"
#
# Needs the Counter-Strike 2 Workshop Tools (resourcecompiler.exe). Log: build-addon.log
param(
    [string]$Addon = "",
    [string]$Cs2 = ""
)
$ErrorActionPreference = "Stop"
$Root = $PSScriptRoot
Start-Transcript -Path (Join-Path $Root "build-addon.log") -Force | Out-Null
try {
    if (-not $Cs2) {
        $roots = @("C:\Program Files (x86)\Steam")
        $vdf = "C:\Program Files (x86)\Steam\steamapps\libraryfolders.vdf"
        if (Test-Path $vdf) {
            foreach ($line in (Select-String -Path $vdf -Pattern '"path"\s+"(.+?)"' -AllMatches)) {
                foreach ($m in $line.Matches) { $roots += $m.Groups[1].Value.Replace('\\', '\') }
            }
        }
        foreach ($r in $roots) {
            $c = Join-Path $r "steamapps\common\Counter-Strike Global Offensive"
            if (Test-Path $c) { $Cs2 = $c; break }
        }
    }
    if (-not $Cs2 -or -not (Test-Path $Cs2)) { throw "CS2 not found. Pass -Cs2 <path>" }
    $compiler = Join-Path $Cs2 "game\bin\win64\resourcecompiler.exe"
    if (-not (Test-Path $compiler)) { throw "Workshop Tools are not installed (no $compiler)" }

    $addonsDir = Join-Path $Cs2 "content\csgo_addons"
    if (-not $Addon) {
        Write-Host "Addons in $addonsDir :"
        Get-ChildItem $addonsDir -Directory -ErrorAction SilentlyContinue | ForEach-Object { Write-Host "  $($_.Name)" }
        $Addon = Read-Host "Addon name"
    }
    if (-not $Addon) { throw "no addon name" }
    $content = Join-Path $addonsDir $Addon
    if (-not (Test-Path $content)) { throw "no addon folder $content" }
    Write-Host "CS2:   $Cs2"
    Write-Host "Addon: $content"

    $srcPanorama = (Resolve-Path (Join-Path $Root "..\BlockingZones\panorama")).Path
    $dstPanorama = Join-Path $content "panorama"
    $files = Get-ChildItem $srcPanorama -Recurse -File
    foreach ($f in $files) {
        $rel = $f.FullName.Substring($srcPanorama.Length).TrimStart('\')
        $dst = Join-Path $dstPanorama $rel
        New-Item -ItemType Directory -Force -Path (Split-Path $dst -Parent) | Out-Null
        Copy-Item $f.FullName $dst -Force
        (Get-Item $dst).LastWriteTime = Get-Date   # the compiler skips files it thinks are up to date
    }
    Write-Host "copied $($files.Count) file(s)"

    & $compiler -i (Join-Path $dstPanorama "layout\custom_game\bz.xml") -f -r
    & $compiler -i (Join-Path $dstPanorama "styles\custom_game\bz.css") -f -r

    $game = Join-Path $Cs2 "game\csgo_addons\$Addon\panorama"
    $need = @("layout\custom_game\bz.vxml_c", "styles\custom_game\bz.vcss_c")
    $missing = $need | Where-Object { -not (Test-Path (Join-Path $game $_)) }
    if ($missing) { throw "not compiled: $($missing -join ', ')" }
    Write-Host "OK: layout + stylesheet in $game"
    Write-Host "Now publish/update the addon on the Workshop; players rejoin to get it."
}
finally {
    Stop-Transcript | Out-Null
}
