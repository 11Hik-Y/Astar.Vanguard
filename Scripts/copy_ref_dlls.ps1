[CmdletBinding()]
param(
    [string]$WorkspaceFolder = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string]$GameRoot = "D:\game\SPT\client\EFT-40087__SPT-4.0.x",
    [string]$SptLayer = "D:\game\SPT\client\_layers\SPT-4.0.13-40087-2891fd4",
    [string]$AstarUiDll = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$refDir = Join-Path $WorkspaceFolder "Ref"
if (-not (Test-Path -LiteralPath $refDir)) {
    New-Item -ItemType Directory -Path $refDir -Force | Out-Null
}

$baseDllRelativePaths = @(
    "EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll"
    "EscapeFromTarkov_Data\Managed\CommonExtensions.dll"
    "EscapeFromTarkov_Data\Managed\UnityEngine.dll"
    "EscapeFromTarkov_Data\Managed\UnityEngine.CoreModule.dll"
    "EscapeFromTarkov_Data\Managed\UnityEngine.IMGUIModule.dll"
    "EscapeFromTarkov_Data\Managed\UnityEngine.PhysicsModule.dll"
    "EscapeFromTarkov_Data\Managed\UnityEngine.TextRenderingModule.dll"
    "EscapeFromTarkov_Data\Managed\UnityEngine.UI.dll"
    "EscapeFromTarkov_Data\Managed\UnityEngine.AIModule.dll"
    "EscapeFromTarkov_Data\Managed\UnityEngine.AssetBundleModule.dll"
    "EscapeFromTarkov_Data\Managed\Unity.TextMeshPro.dll"
    "EscapeFromTarkov_Data\Managed\Comfort.dll"
    "EscapeFromTarkov_Data\Managed\UnityEngine.InputLegacyModule.dll"
    "EscapeFromTarkov_Data\Managed\Sirenix.Serialization.dll"
    "EscapeFromTarkov_Data\Managed\DissonanceVoip.dll"
    "EscapeFromTarkov_Data\Managed\Comfort.Unity.dll"
    "EscapeFromTarkov_Data\Managed\Newtonsoft.Json.dll"
    "EscapeFromTarkov_Data\Managed\ItemComponent.Types.dll"
)

$layerDllRelativePaths = @(
    "BepInEx\plugins\spt\ConfigurationManager\ConfigurationManager.dll"
    "BepInEx\plugins\spt\spt-common.dll"
    "BepInEx\plugins\spt\spt-core.dll"
    "BepInEx\plugins\spt\spt-custom.dll"
    "BepInEx\plugins\spt\spt-debugging.dll"
    "BepInEx\plugins\spt\spt-reflection.dll"
    "BepInEx\plugins\spt\spt-singleplayer.dll"
    "BepInEx\core\0Harmony.dll"
    "BepInEx\core\BepInEx.dll"
    "BepInEx\plugins\Fika\Fika.Core.dll"
    "BepInEx\plugins\DrakiaXYZ-BigBrain.dll"
)

$copiedCount = 0
$missingFiles = [System.Collections.Generic.List[string]]::new()
$entries = @(
    $baseDllRelativePaths | ForEach-Object {
        [pscustomobject]@{ Root = $GameRoot; RelativePath = $_ }
    }
) + @(
    $layerDllRelativePaths | ForEach-Object {
        [pscustomobject]@{ Root = $SptLayer; RelativePath = $_ }
    }
)

foreach ($entry in $entries) {
    $fullPath = Join-Path $entry.Root $entry.RelativePath
    if (Test-Path -LiteralPath $fullPath -PathType Leaf) {
        Copy-Item -LiteralPath $fullPath -Destination $refDir -Force
        Write-Host "Copied: $fullPath"
        $copiedCount++
    }
    else {
        $missingFiles.Add($fullPath)
        Write-Warning "Missing: $fullPath"
    }
}

if ([string]::IsNullOrWhiteSpace($AstarUiDll)) {
    $astarUiRepo = Resolve-Path (Join-Path $WorkspaceFolder "..\Astar.UI") -ErrorAction SilentlyContinue
    if ($null -ne $astarUiRepo) {
        foreach ($configuration in @("Release", "Debug")) {
            $candidate = Join-Path $astarUiRepo "artifacts\bin\$configuration\Astar.UI.dll"
            if (Test-Path -LiteralPath $candidate -PathType Leaf) {
                $AstarUiDll = $candidate
                break
            }
        }
    }
}

if (-not [string]::IsNullOrWhiteSpace($AstarUiDll)) {
    if (-not (Test-Path -LiteralPath $AstarUiDll -PathType Leaf)) {
        throw "Astar.UI.dll not found: $AstarUiDll"
    }

    Copy-Item -LiteralPath $AstarUiDll -Destination (Join-Path $refDir "Astar.UI.dll") -Force
    Write-Host "Copied: $AstarUiDll"
    $copiedCount++
}
else {
    $missingFiles.Add("Astar.UI.dll (pass -AstarUiDll or build ..\Astar.UI first)")
}

$expectedCount = $baseDllRelativePaths.Count + $layerDllRelativePaths.Count + 1
Write-Host "`nCopy complete: $copiedCount/$expectedCount files copied to $refDir"

if ($missingFiles.Count -gt 0) {
    Write-Host "`nMissing files:" -ForegroundColor Yellow
    $missingFiles | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
    exit 1
}

Write-Host "All compile-time references are ready." -ForegroundColor Green
