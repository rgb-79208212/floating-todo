param(
    [switch]$RemoveData,
    [switch]$Silent
)

$ErrorActionPreference = "Stop"

try {
    $installDir = Join-Path $env:LOCALAPPDATA "Programs\FloatingTodo"
    $destExe = Join-Path $installDir "FloatingTodo.exe"
    $dataDir = Join-Path $env:APPDATA "FloatingTodo"

    Write-Host ""
    Write-Host "  浮光待办 · 卸载" -ForegroundColor Magenta
    Write-Host "  --------------------------------"

    if (Test-Path $destExe) {
        try {
            $quitInfo = New-Object System.Diagnostics.ProcessStartInfo
            $quitInfo.FileName = $destExe
            $quitInfo.Arguments = "--quit"
            $quitInfo.UseShellExecute = $false
            [void][System.Diagnostics.Process]::Start($quitInfo)
            Start-Sleep -Milliseconds 900
        } catch { }
    }
    Get-Process -Name "FloatingTodo" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 300

    $runKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
    Remove-ItemProperty -Path $runKey -Name "FloatingTodo" -ErrorAction SilentlyContinue

    $programs = [Environment]::GetFolderPath("Programs")
    if (-not [string]::IsNullOrWhiteSpace($programs)) {
        Remove-Item -LiteralPath (Join-Path $programs "浮光待办.lnk") -Force -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath (Join-Path $programs "卸载浮光待办.lnk") -Force -ErrorAction SilentlyContinue
    }

    Set-Location $env:TEMP
    if (Test-Path $installDir) {
        Remove-Item -LiteralPath $installDir -Recurse -Force -ErrorAction SilentlyContinue
    }

    if ($RemoveData -and (Test-Path $dataDir)) {
        Remove-Item -LiteralPath $dataDir -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "  已删除待办数据。" -ForegroundColor Yellow
    }
    elseif (Test-Path $dataDir) {
        Write-Host ("  待办数据已保留： " + $dataDir)
    }

    Write-Host "  浮光待办已卸载，开机自动显示也已关闭。" -ForegroundColor Green
    Write-Host ""
}
catch {
    Write-Host ("  卸载时遇到问题：" + $_.Exception.Message) -ForegroundColor Red
    Write-Host ""
}

if (-not $Silent) {
    Read-Host "  按 Enter 关闭"
}
