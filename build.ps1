$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$wpf = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF"
$framework = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319"

if (-not (Test-Path $csc)) { throw "未找到 .NET Framework C# 编译器：$csc" }
if (-not (Test-Path (Join-Path $wpf "PresentationFramework.dll"))) { throw "未找到 WPF 运行库。" }

$build = Join-Path $root "build"
if (-not (Test-Path $build)) { New-Item -ItemType Directory -Path $build | Out-Null }

$sources = Get-ChildItem (Join-Path $root "src") -Filter "*.cs" | ForEach-Object { $_.FullName }
$ui = Join-Path $root "src\ui.xaml"
$noise = Join-Path $root "assets\noise.png"
$icon = Join-Path $root "assets\app.ico"
$alertIcon = Join-Path $root "assets\app.alert.ico"
$manifest = Join-Path $root "src\app.manifest"
$out = Join-Path $build "FloatingTodo.exe"

$refs = @(
    "/r:$wpf\PresentationFramework.dll",
    "/r:$wpf\PresentationCore.dll",
    "/r:$wpf\WindowsBase.dll",
    "/r:$framework\System.Xaml.dll",
    "/r:System.dll",
    "/r:System.Core.dll",
    "/r:System.Drawing.dll",
    "/r:System.Windows.Forms.dll",
    "/r:System.Runtime.Serialization.dll",
    "/r:System.Xml.dll"
)

$arguments = @(
    "/nologo",
    "/target:winexe",
    "/platform:anycpu",
    "/codepage:65001",
    "/optimize+",
    "/win32icon:$icon",
    "/win32manifest:$manifest",
    "/resource:$ui,FloatingTodo.UI.xaml",
    "/resource:$noise,FloatingTodo.noise.png",
    "/resource:$icon,FloatingTodo.app.ico",
    "/resource:$alertIcon,FloatingTodo.app.alert.ico",
    "/out:$out"
) + $refs + $sources

& $csc $arguments
if ($LASTEXITCODE -ne 0) { throw "编译失败，退出码 $LASTEXITCODE" }

$info = Get-Item $out
Write-Host ("已生成：" + $info.FullName)
Write-Host ("大小：" + [math]::Round($info.Length / 1KB, 1) + " KB")
