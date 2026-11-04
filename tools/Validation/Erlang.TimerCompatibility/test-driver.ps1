param([string]$Driver = (Join-Path $PSScriptRoot '../../validate-timer-original.ps1'))
$ErrorActionPreference = 'Stop'
$tokens = $null
$errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Resolve-Path $Driver), [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Driver parse failed' }
$dispatch = $ast.Find({ param($node) $node -is [Management.Automation.Language.IfStatementAst] -and $node.Extent.Text.StartsWith('if (-not $CaptureOnly)') }, $true)
if (-not $dispatch) { throw 'Comparison dispatch missing' }
$block = [scriptblock]::Create($dispatch.Extent.Text)
function dotnet {
    $script:Calls.Add(@($args))
    $global:LASTEXITCODE = if ($script:FailAt -eq $script:Calls.Count) { 1 } else { 0 }
}
$RunnerDll = 'runner.dll'; $repository = 'repo'; $directory = 'oracle'
$timerReference = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'reference-contract.psd1')
$checks = 0
foreach ($BacktraceDepth in @(0, 1, 8, 32)) {
    foreach ($IncludeUpstreamSuite in @($false, $true)) {
        $CaptureOnly = $false; $script:FailAt = 0
        $script:Calls = [Collections.Generic.List[object]]::new()
        & $block
        $expected = @()
        foreach ($backend in @('Managed', 'Beam')) {
            $expected += ,@($RunnerDll, $repository, $directory, $backend, "depth:$BacktraceDepth")
            if ($IncludeUpstreamSuite) { $expected += ,@($RunnerDll, $repository, $directory, $backend, 'suite', "depth:$BacktraceDepth") }
        }
        if ($Calls.Count -ne $expected.Count) { throw 'Requested comparisons missing' }
        for ($i = 0; $i -lt $Calls.Count; $i++) {
            if (($Calls[$i] -join '|') -cne ($expected[$i] -join '|')) { throw 'Mode/depth/backend dispatch differs' }
        }
        $checks++
    }
}
$CaptureOnly = $true; $script:Calls.Clear(); & $block
if ($Calls.Count) { throw 'Capture-only invoked comparison' }; $checks++
$CaptureOnly = $false; $IncludeUpstreamSuite = $true
foreach ($script:FailAt in @(1, 2)) {
    $script:Calls.Clear(); $failed = $false
    try { & $block } catch { $failed = $true }
    if (-not $failed -or $Calls.Count -ne $script:FailAt) { throw 'Comparison failure was not propagated immediately' }
    $checks++
}
$source = $ast.Extent.Text
if ([regex]::Matches($source, 'erlang:system_flag\(backtrace_depth,').Count -ne 2) { throw 'Both oracle captures must set requested depth' }; $checks++
"Timer driver: $checks focused dispatch/capture/failure checks passed"
