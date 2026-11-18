param(
    [Parameter(Mandatory)][string]$RunDirectory,
    [Parameter(Mandatory)][string]$CorpusBinary,
    [Parameter(Mandatory)][string]$Dotnet,
    [Parameter(Mandatory)][string]$Release,
    [Parameter(Mandatory)][string]$PublicKeyHeaders,
    [Parameter(Mandatory)][string]$SourceHeaders,
    [Parameter(Mandatory)][string]$ModulesDirectory,
    [ValidateSet(4,5)][int]$Workers = 5
)

$ErrorActionPreference = 'Stop'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
New-Item -ItemType Directory $RunDirectory | Out-Null
$clock = [Diagnostics.Stopwatch]::StartNew()
$children = foreach ($index in 0..($Workers - 1)) {
    $shard = Join-Path $RunDirectory "shard-$index"
    New-Item -ItemType Directory $shard | Out-Null
    $modules = Join-Path $shard 'modules.txt'
    Copy-Item -LiteralPath (Join-Path $ModulesDirectory "shard-$index/modules.txt") -Destination $modules
    $report = Join-Path $shard 'otp-corpus.json'
    $childArguments = @(
        ('"{0}"' -f $CorpusBinary)
        ('"{0}"' -f $Release)
        ('"{0}"' -f $report)
        ('"{0}"' -f $PublicKeyHeaders)
        ('"{0}"' -f $SourceHeaders)
        '--modules-file'
        ('"{0}"' -f $modules)
    )
    $process = Start-Process -FilePath $Dotnet -ArgumentList $childArguments -PassThru -WindowStyle Hidden `
        -RedirectStandardOutput "$shard/stdout.log" -RedirectStandardError "$shard/stderr.log"
    [pscustomobject]@{ Index = $index; Process = $process; Report = $report }
}

$children.Process | Wait-Process
$clock.Stop()
$shards = foreach ($child in $children) {
    $process = $child.Process
    if ($process.ExitCode -notin 0, 1 -or -not (Test-Path -LiteralPath $child.Report)) {
        throw "Shard $($child.Index) did not produce a valid report. Exit code $($process.ExitCode)."
    }
    $raw = Get-Content -LiteralPath $child.Report -Raw | ConvertFrom-Json
    [pscustomobject]@{
        Index = $child.Index
        Seconds = ($process.ExitTime - $process.StartTime).TotalSeconds
        Counts = $raw.Counts
        Modules = $raw.Modules
    }
}

$allModules = @($shards.Modules | Sort-Object Beam)
if ($allModules.Count -ne 1366 -or @($allModules.Beam | Sort-Object -Unique).Count -ne 1366) {
    throw 'The release shard reports do not cover exactly 1,366 unique module paths.'
}

$counts = [ordered]@{}
foreach ($name in @('Equal', 'Different', 'Unsupported', 'SourceRejected')) {
    $counts[$name] = @($allModules | Where-Object Status -eq $name).Count
}
if (($counts.Values | Measure-Object -Sum).Sum -ne 1366) {
    throw 'The release reports contain an unknown status.'
}

$timing = [ordered]@{
    ElapsedSeconds = $clock.Elapsed.TotalSeconds
    Shards = @($shards | Select-Object Index,Seconds,Counts)
}
$timing | ConvertTo-Json -Depth 6 | Set-Content "$RunDirectory/timing.json"
$reportObject = [ordered]@{ OtpVersion = '29.1.1'; Total = $allModules.Count; Counts = $counts; Modules = $allModules }
$reportObject | ConvertTo-Json -Depth 5 | Set-Content "$RunDirectory/otp-corpus.json"
$counts | ConvertTo-Json -Compress

