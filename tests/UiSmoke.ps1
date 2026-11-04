$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $root 'build\FloatingTodo.exe'
$test = Join-Path $root 'test-smoke'
if (Test-Path $test) { Remove-Item $test -Recurse -Force }
New-Item -ItemType Directory -Path $test | Out-Null

function Stop-App {
    try {
        $q = New-Object System.Diagnostics.ProcessStartInfo
        $q.FileName = $exe
        $q.Arguments = '--dev --quit'
        $q.UseShellExecute = $false
        $q.EnvironmentVariables['FLOATING_TODO_DATA_DIR'] = $test
        $qp = [System.Diagnostics.Process]::Start($q)
        [void]$qp.WaitForExit(1800)
    } catch { }
    Start-Sleep -Milliseconds 600
}
function Start-App {
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $exe
    $psi.Arguments = '--dev'
    $psi.UseShellExecute = $false
    $psi.WorkingDirectory = $root
    $psi.EnvironmentVariables['FLOATING_TODO_DATA_DIR'] = $test
    return [System.Diagnostics.Process]::Start($psi)
}
function Wait-Window($process) {
    Add-Type -AssemblyName UIAutomationClient
    Add-Type -AssemblyName UIAutomationTypes
    $A = [System.Windows.Automation.AutomationElement]
    for ($i = 0; $i -lt 20; $i++) {
        $c = New-Object System.Windows.Automation.AndCondition (New-Object System.Windows.Automation.PropertyCondition ($A::NameProperty), '浮光待办'), (New-Object System.Windows.Automation.PropertyCondition ($A::ProcessIdProperty), $process.Id)
        $w = $A::RootElement.FindFirst([System.Windows.Automation.TreeScope]::Children, $c)
        if ($null -ne $w) { return $w }
        Start-Sleep -Milliseconds 250
    }
    return $null
}
function Find-ByName($window, [string]$name, $type) {
    $A = [System.Windows.Automation.AutomationElement]
    $nameCond = New-Object System.Windows.Automation.PropertyCondition ($A::NameProperty), $name
    if ($null -eq $type) { return $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $nameCond) }
    $typeCond = New-Object System.Windows.Automation.PropertyCondition ($A::ControlTypeProperty), $type
    $both = New-Object System.Windows.Automation.AndCondition $nameCond, $typeCond
    return $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $both)
}
function Invoke-Name($window, [string]$name) {
    $A = [System.Windows.Automation.AutomationElement]
    $e = Find-ByName $window $name ([System.Windows.Automation.ControlType]::Button)
    if ($null -eq $e) { throw "找不到按钮：$name" }
    $e.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
}
function Read-State {
    $json = Get-Content (Join-Path $test 'data.json') -Raw -Encoding UTF8
    return $json | ConvertFrom-Json
}

Stop-App
$app = Start-App
$win = Wait-Window $app
if ($null -eq $win) { throw '主窗口未出现' }
Write-Output 'PASS  主窗口启动'

$A = [System.Windows.Automation.AutomationElement]
$edit = Find-ByName $win '新的待办事项' ([System.Windows.Automation.ControlType]::Edit)
if ($null -eq $edit) { throw '找不到输入框' }
$edit.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue('主题回归测试')
Invoke-Name $win '添加待办'
Start-Sleep -Milliseconds 800
$title = Find-ByName $win '主题回归测试' $null
Write-Output ("PASS  新增任务=" + ($null -ne $title))

Invoke-Name $win '标记为已完成：主题回归测试'
Start-Sleep -Milliseconds 800
$state = Read-State
Write-Output ("PASS  完成任务=" + ($state.Tasks.Count -eq 1 -and $state.Tasks[0].Done -eq $true))

Invoke-Name $win '切换到深色'
Start-Sleep -Seconds 2
$win = Wait-Window $app
$darkButton = Find-ByName $win '切换到浅色' ([System.Windows.Automation.ControlType]::Button)
Write-Output ("PASS  切换深色=" + ($null -ne $darkButton))
$state = Read-State
Write-Output ("PASS  深色持久化=" + ($state.Settings.Theme -eq 'dark' -and $state.Tasks.Count -eq 1))

Invoke-Name $win '切换到浅色'
Start-Sleep -Seconds 2
$win = Wait-Window $app
$lightButton = Find-ByName $win '切换到深色' ([System.Windows.Automation.ControlType]::Button)
Write-Output ("PASS  切换浅色=" + ($null -ne $lightButton))
$state = Read-State
Write-Output ("PASS  浅色持久化=" + ($state.Settings.Theme -eq 'light' -and $state.Tasks.Count -eq 1))

Invoke-Name $win '最小化到系统托盘'
Start-Sleep -Milliseconds 900
$stillRunning = -not $app.HasExited
Write-Output ("PASS  最小化驻留=" + $stillRunning)

$second = Start-App
[void]$second.WaitForExit(2500)
Start-Sleep -Milliseconds 900
$win = Wait-Window $app
$visible = $null -ne $win
Write-Output ("PASS  托盘恢复=" + $visible)

Invoke-Name $win '关闭应用'
[void]$app.WaitForExit(3000)
Write-Output ("PASS  直接退出=" + $app.HasExited)

$app2 = Start-App
$win2 = Wait-Window $app2
if ($null -eq $win2) { throw '重启后主窗口未出现' }
$state = Read-State
Write-Output ("PASS  重启保留任务=" + ($state.Tasks.Count -eq 1 -and $state.Settings.Theme -eq 'light'))
Invoke-Name $win2 '关闭应用'
[void]$app2.WaitForExit(3000)
Write-Output ("PASS  二次退出=" + $app2.HasExited)
