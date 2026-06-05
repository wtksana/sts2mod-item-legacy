param(
    [string]$GameInstallDirectory = "C:\Programs\Steam\steamapps\common\Slay the Spire 2",
    [string]$GameModDirectory = ""
)

$ErrorActionPreference = "Stop"

$projectDirectory = Split-Path -Parent $PSCommandPath
if ([string]::IsNullOrWhiteSpace($GameModDirectory)) {
    $GameModDirectory = Join-Path $GameInstallDirectory "mods\ItemLegacy"
}

$dllPath = Join-Path $projectDirectory ".godot\mono\temp\bin\Debug\ItemLegacy.dll"
$manifestPath = Join-Path $projectDirectory "ItemLegacy.json"
$cfgPath = Join-Path $projectDirectory "ItemLegacy.cfg"
$changelogPath = Join-Path $projectDirectory "更新日志.txt"

if (-not (Test-Path -LiteralPath $dllPath)) {
    throw "未找到构建产物：$dllPath。请先运行 dotnet build。"
}

New-Item -ItemType Directory -Path $GameModDirectory -Force | Out-Null
Copy-Item -LiteralPath $dllPath -Destination (Join-Path $GameModDirectory "ItemLegacy.dll") -Force
Copy-Item -LiteralPath $manifestPath -Destination (Join-Path $GameModDirectory "ItemLegacy.json") -Force
$deployedCfgPath = Join-Path $GameModDirectory "ItemLegacy.cfg"
if (-not (Test-Path -LiteralPath $deployedCfgPath)) {
    Copy-Item -LiteralPath $cfgPath -Destination $deployedCfgPath -Force
}
Copy-Item -LiteralPath $changelogPath -Destination (Join-Path $GameModDirectory "更新日志.txt") -Force

Write-Host "已部署 ItemLegacy.dll、ItemLegacy.json、更新日志.txt 到 $GameModDirectory"
if (Test-Path -LiteralPath $deployedCfgPath) {
    Write-Host "已保留现有配置文件：$deployedCfgPath"
}
