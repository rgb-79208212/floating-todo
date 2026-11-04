$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

$exe = Join-Path $root "build\FloatingTodo.exe"
if (-not (Test-Path $exe)) { throw "请先运行 build.ps1 生成 build\FloatingTodo.exe" }

$releaseRoot = Join-Path $root "release"
$packageDir = Join-Path $releaseRoot "浮光待办"
if (Test-Path $packageDir) { Remove-Item $packageDir -Recurse -Force }
New-Item -ItemType Directory -Path $packageDir -Force | Out-Null

Copy-Item -LiteralPath $exe -Destination (Join-Path $packageDir "FloatingTodo.exe")
Copy-Item -LiteralPath (Join-Path $root "installer\Install.ps1") -Destination $packageDir
Copy-Item -LiteralPath (Join-Path $root "installer\Install.cmd") -Destination $packageDir
Copy-Item -LiteralPath (Join-Path $root "installer\Uninstall.ps1") -Destination $packageDir
Copy-Item -LiteralPath (Join-Path $root "installer\README.txt") -Destination $packageDir

$zipPath = Join-Path $releaseRoot "浮光待办-v1.4.0.zip"
Compress-Archive -Path $packageDir -DestinationPath $zipPath -CompressionLevel Optimal -Force

Get-ChildItem $packageDir | Select-Object Name, Length | Format-Table -AutoSize
Write-Host ("安装包：" + $zipPath)
Write-Host ("解压后双击 Install.cmd 即可安装。")
