# Root executes later. No build or RED was run by author.
$ErrorActionPreference='Stop'
$root=(Get-Location).Path
$proof=$PSScriptRoot
$python='C:/Users/Maksim/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$env:DOTNET_PROCESSOR_COUNT='2'
$env:DOTNET_CLI_USE_MSBUILD_SERVER='0'
$env:MSBUILDDISABLENODEREUSE='1'
foreach ($name in @('target-build.log','exit-two-actual-red.json','exit-three-actual-red.json')) { if (Test-Path -LiteralPath (Join-Path $proof $name)) { throw 'Preserve first execution; fresh outputs required' } }
& $python -B "$proof/verify-source.py"
if ($LASTEXITCODE -ne 0) { throw 'Source refusal' }
& dotnet build "$proof/candidate/tests/Erlang.Tests/Erlang.Tests.csproj" --configuration Debug --maxcpucount:1 -p:UseSharedCompilation=false -p:RestoreDisableParallel=true *> "$proof/target-build.log"
$code=$LASTEXITCODE
@{ExitCode=$code;ActualBuild=$true} | ConvertTo-Json | Set-Content "$proof/target-build-exit.json" -Encoding utf8NoBOM
if ($code -ne 0) { throw 'Compile failure is not semantic RED' }
$runner=Join-Path $root 'artifacts/source-purge-stage315-focus/bin/Debug/net10.0/Focus.dll'
$dll=Join-Path $proof 'candidate/tests/Erlang.Tests/bin/Debug/net10.0/Erlang.Tests.dll'
foreach ($entry in @(@('exit-two','ExitTwoPublishes'),@('exit-three','ExitThreePublishes'))) {
    & dotnet $runner $dll "$proof/$($entry[0])-actual-red.json" "DistributionGuestExitPublicRouteHostChecks:$($entry[1])" *> "$proof/$($entry[0])-actual-red.log"
    $result=$LASTEXITCODE
    @{ExitCode=$result;ExpectedSemanticRedExitCode=1} | ConvertTo-Json | Set-Content "$proof/$($entry[0])-exit.json" -Encoding utf8NoBOM
    if ($result -ne 1) { throw 'Expected actual missing-packet failure' }
}
& $python -B "$proof/review-red.py"
if ($LASTEXITCODE -ne 0) { throw 'Wrong RED classification' }
& $python -B "$proof/verify-source.py"
if ($LASTEXITCODE -ne 0) { throw 'Post-run source changed' }
