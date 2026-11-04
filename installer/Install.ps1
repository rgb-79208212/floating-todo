param(
    [switch]$NoLaunch,
    [switch]$Silent
)

$ErrorActionPreference = "Stop"

function Write-Step([string]$Text) {
    Write-Host ("  " + $Text)
}

try {
    $sourceDir = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($sourceDir)) { $sourceDir = Split-Path -Parent $MyInvocation.MyCommand.Path }
    $sourceExe = Join-Path $sourceDir "FloatingTodo.exe"
    if (-not (Test-Path $sourceExe)) {
        throw "没有找到 FloatingTodo.exe。请把安装脚本和程序放在同一个文件夹里。"
    }

    $installDir = Join-Path $env:LOCALAPPDATA "Programs\FloatingTodo"
    $destExe = Join-Path $installDir "FloatingTodo.exe"

    Write-Host ""
    Write-Host "  浮光待办 · 安装" -ForegroundColor Magenta
    Write-Host "  --------------------------------"
    Write-Step ("安装位置：" + $installDir)

    # Close a previous copy gracefully, then force any leftovers.
    if (Test-Path $destExe) {
        try {
            $quitInfo = New-Object System.Diagnostics.ProcessStartInfo
            $quitInfo.FileName = $destExe
            $quitInfo.Arguments = "--quit"
            $quitInfo.UseShellExecute = $false
            $quitInfo.WorkingDirectory = Split-Path -Parent $destExe
            [void][System.Diagnostics.Process]::Start($quitInfo)
            Start-Sleep -Milliseconds 900
        } catch { }
    }
    Get-Process -Name "FloatingTodo" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 350

    New-Item -ItemType Directory -Path $installDir -Force | Out-Null
    Copy-Item -LiteralPath $sourceExe -Destination $destExe -Force

    $uninstallerSource = Join-Path $sourceDir "Uninstall.ps1"
    if (-not (Test-Path $uninstallerSource)) {
        throw "安装包缺少 Uninstall.ps1。"
    }
    Copy-Item -LiteralPath $uninstallerSource -Destination (Join-Path $installDir "Uninstall.ps1") -Force

    $readmeSource = Join-Path $sourceDir "README.txt"
    if (Test-Path $readmeSource) {
        Copy-Item -LiteralPath $readmeSource -Destination (Join-Path $installDir "README.txt") -Force
    }

    # Keep the user's existing autostart choice. The app synchronizes the Run
    # entry from its own setting on launch, so an upgrade never flips it on.
    Write-Step "开机自动显示沿用原有设置。"

    # Start menu shortcuts.
    $programs = [Environment]::GetFolderPath("Programs")
    if (-not [string]::IsNullOrWhiteSpace($programs)) {
        $wsh = New-Object -ComObject WScript.Shell
        try {
            $appShortcut = $wsh.CreateShortcut((Join-Path $programs "浮光待办.lnk"))
            $appShortcut.TargetPath = $destExe
            $appShortcut.WorkingDirectory = $installDir
            $appShortcut.IconLocation = ($destExe + ",0")
            $appShortcut.Description = "桌面毛玻璃待办小组件"
            $appShortcut.Save()

            $psExe = Join-Path $env:SystemRoot "System32\WindowsPowerShell\v1.0\powershell.exe"
            $uninstallShortcut = $wsh.CreateShortcut((Join-Path $programs "卸载浮光待办.lnk"))
            $uninstallShortcut.TargetPath = $psExe
            $uninstallShortcut.Arguments = '-NoProfile -ExecutionPolicy Bypass -File "' + (Join-Path $installDir "Uninstall.ps1") + '"'
            $uninstallShortcut.WorkingDirectory = $env:TEMP
            $uninstallShortcut.IconLocation = ($destExe + ",0")
            $uninstallShortcut.Description = "卸载浮光待办（保留待办数据）"
            $uninstallShortcut.Save()
        }
        finally {
            if ($wsh -ne $null) {
                [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($wsh)
            }
        }
        Write-Step "已创建开始菜单快捷方式。"
    }

    if (-not $NoLaunch) {
        try {
            $launchInfo = New-Object System.Diagnostics.ProcessStartInfo
            $launchInfo.FileName = $destExe
            $launchInfo.WorkingDirectory = $installDir
            $launchInfo.UseShellExecute = $false
            [void][System.Diagnostics.Process]::Start($launchInfo)
            Write-Step "浮光待办已启动。"
        }
        catch {
            Write-Host ("  自动启动失败，请从开始菜单打开： " + $destExe) -ForegroundColor Yellow
        }
    }

    Write-Host ""
    Write-Host "  安装完成。" -ForegroundColor Green
    Write-Host "  · 开机自动显示由右上角「⋯」里的开关控制。"
    Write-Host "  · 最小化：右上角「−」直接收进系统托盘。"
    Write-Host "  · 退出：右上角「×」直接关闭并保存。"
    Write-Host "  · 卸载：开始菜单 →「卸载浮光待办」。"
    Write-Host ""
    if (-not $Silent) {
        Read-Host "  按 Enter 关闭安装窗口"
    }
    exit 0
}
catch {
    Write-Host ""
    Write-Host ("  安装失败：" + $_.Exception.Message) -ForegroundColor Red
    Write-Host ""
    if (-not $Silent) {
        Read-Host "  按 Enter 关闭"
    }
    exit 1
}
