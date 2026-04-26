param(
    [string]$GameModDirectory = "C:\Dev\sts2mod\GameInstall\mods\ItemLegacy"
)

$ErrorActionPreference = "Stop"

$projectDirectory = Split-Path -Parent $PSCommandPath
$dllPath = Join-Path $projectDirectory ".godot\mono\temp\bin\Debug\ItemLegacy.dll"
$cfgPath = Join-Path $projectDirectory "ItemLegacy.cfg"
$changelogPath = Join-Path $projectDirectory "更新日志.md"

if (-not (Test-Path -LiteralPath $dllPath)) {
    throw "未找到构建产物：$dllPath。请先运行 dotnet build。"
}

New-Item -ItemType Directory -Path $GameModDirectory -Force | Out-Null
Copy-Item -LiteralPath $dllPath -Destination (Join-Path $GameModDirectory "ItemLegacy.dll") -Force
Copy-Item -LiteralPath $cfgPath -Destination (Join-Path $GameModDirectory "ItemLegacy.cfg") -Force
Copy-Item -LiteralPath $changelogPath -Destination (Join-Path $GameModDirectory "更新日志.md") -Force

Write-Host "已部署 ItemLegacy.dll、ItemLegacy.cfg、更新日志.md 到 $GameModDirectory"
