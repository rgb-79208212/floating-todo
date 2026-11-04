$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) { throw "未找到 .NET Framework 编译器" }
$out = Join-Path $PSScriptRoot "CoreTests.exe"
$args = @(
    "/nologo", "/target:exe", "/codepage:65001", "/out:$out",
    "/r:System.dll", "/r:System.Core.dll", "/r:System.Runtime.Serialization.dll", "/r:System.Xml.dll",
    "src\Models.cs", "src\TaskService.cs", "src\Storage.cs", "tests\CoreTests.cs"
)
& $csc $args
if ($LASTEXITCODE -ne 0) { throw "核心测试编译失败" }
& $out
exit $LASTEXITCODE
