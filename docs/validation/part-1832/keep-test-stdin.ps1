param(
    [Parameter(Mandatory)][string]$Dotnet,
    [Parameter(Mandatory)][string]$Assembly,
    [Parameter(Mandatory)][string]$Report,
    [string]$Case
)

$ErrorActionPreference = 'Stop'
$configuration = [Diagnostics.ProcessStartInfo]::new($Dotnet)
$configuration.UseShellExecute = $false
$configuration.CreateNoWindow = $true
$configuration.WorkingDirectory = (Get-Location).Path
$configuration.RedirectStandardInput = $true
$configuration.RedirectStandardOutput = $true
$configuration.RedirectStandardError = $true
$configuration.ArgumentList.Add($Assembly)
$configuration.ArgumentList.Add('--report')
$configuration.ArgumentList.Add($Report)
if ($Case) {
    $configuration.ArgumentList.Add('--case')
    $configuration.ArgumentList.Add($Case)
}

$stdout = [IO.File]::Open("$Report.stdout.log", [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::Read)
$stderr = [IO.File]::Open("$Report.stderr.log", [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::Read)
try {
    $process = [Diagnostics.Process]::Start($configuration)
    $outCopy = $process.StandardOutput.BaseStream.CopyToAsync($stdout)
    $errCopy = $process.StandardError.BaseStream.CopyToAsync($stderr)
    $start = $process.StartTime
    $process.WaitForExit()
    $outCopy.GetAwaiter().GetResult()
    $errCopy.GetAwaiter().GetResult()
    $code = $process.ExitCode
    @{
        ProcessId = $process.Id
        ExitCode = $code
        Seconds = ($process.ExitTime - $start).TotalSeconds
        Report = [IO.File]::Exists($Report)
    } | ConvertTo-Json | Set-Content "$Report.receipt.json"
    exit $code
}
finally {
    $stdout.Dispose()
    $stderr.Dispose()
}
