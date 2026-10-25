param(
    [Parameter(Mandatory=$true)][string] $Executable,
    [Parameter(Mandatory=$true)][string[]] $Arguments,
    [Parameter(Mandatory=$true)][string] $Prefix,
    [int] $TimeoutMilliseconds = 30000,
    [int] $KillWaitMilliseconds = 5000
)
$taskProcess = $null
$taskComplete = $false
$taskTimedOut = $false
$taskStillAlive = $false
$taskExit = $null
$taskLaunchError = $null
$taskKillError = $null
$taskWaitError = $null
try {
    $taskProcess = Start-Process -FilePath $Executable -ArgumentList $Arguments -WindowStyle Hidden -PassThru -RedirectStandardOutput ($Prefix + '.stdout') -RedirectStandardError ($Prefix + '.stderr') -ErrorAction Stop
    $taskComplete = $taskProcess.WaitForExit($TimeoutMilliseconds)
    if (-not $taskComplete) {
        $taskTimedOut = $true
        try {
            if (-not $taskProcess.HasExited) { $taskProcess.Kill($true) }
        }
        catch { $taskKillError = $_.Exception.ToString() }
        try { $taskComplete = $taskProcess.WaitForExit($KillWaitMilliseconds) }
        catch { $taskWaitError = $_.Exception.ToString() }
    }
}
catch { $taskLaunchError = $_.Exception.ToString() }
finally {
    if ($null -ne $taskProcess) {
        try {
            $taskStillAlive = -not $taskProcess.HasExited
            if (-not $taskStillAlive) { $taskExit = $taskProcess.ExitCode }
        }
        catch { $taskWaitError = $_.Exception.ToString() }
    }
    $taskRow = [pscustomobject]@{
        Complete = $taskComplete
        TimedOut = $taskTimedOut
        StillAlive = $taskStillAlive
        ExitCode = $taskExit
        LaunchError = $taskLaunchError
        KillError = $taskKillError
        WaitError = $taskWaitError
        ProcessId = if ($null -ne $taskProcess) { $taskProcess.Id } else { $null }
        TimeoutMilliseconds = $TimeoutMilliseconds
        KillWaitMilliseconds = $KillWaitMilliseconds
    }
    $taskRow | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath ($Prefix + '.metadata.json')
    if ($null -ne $taskProcess) { $taskProcess.Dispose() }
}
$taskRow
