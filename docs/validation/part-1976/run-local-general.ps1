$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH='0'
$ErrorActionPreference='Stop'
$stage='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/shared-compiler-corrections-1976-normalized-repair'
$work='C:/Users/Maksim/.codex/worktrees/beam-release-parity/Erlang.Net'
$sdk='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/sdk'
$ref='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed'
$env:PATH="$sdk;$ref/bin;$env:PATH"
$env:DOTNET_PROCESSOR_COUNT='1'
$env:TMP="$stage/local-general/temp"
$env:TEMP=$env:TMP
$env:ERL_CRASH_DUMP="$stage/local-general/erl_crash.dump"
$d="$stage/local-general"
New-Item -ItemType Directory -Path $d,$env:TEMP|Out-Null
Set-Location -LiteralPath $work
$knownCompiler=(Get-Content -LiteralPath "$stage/source-freeze.json" -Raw|ConvertFrom-Json).Compiler
$started=[DateTime]::UtcNow
$clock=[Diagnostics.Stopwatch]::StartNew()
$buildClock=[Diagnostics.Stopwatch]::StartNew()
& "$sdk/dotnet.exe" build -m:1 -p:UseSharedCompilation=false -p:ConcurrentBuild=false --nologo > "$d/build.stdout.log" 2> "$d/build.stderr.log"
$buildExit=$LASTEXITCODE
$buildClock.Stop()
$validatorExit=$null
$report=$null
if($buildExit -eq 0){
    $buildCompiler=(Get-FileHash -LiteralPath "$work/src/Compiler/Erlang.Compiler/bin/Debug/net10.0/Erlang.Compiler.dll").Hash
    & pwsh -NoProfile -File "$work/tools/validate.ps1" -Part All > "$d/validation.stdout.log" 2> "$d/validation.stderr.log"
    $validatorExit=$LASTEXITCODE
    $file=Get-Item -LiteralPath "$work/artifacts/tests.json" -ErrorAction SilentlyContinue
    if($file -and $file.LastWriteTimeUtc -ge $started){
        Copy-Item -LiteralPath $file.FullName -Destination "$d/tests.json"
        $report=(Get-FileHash -LiteralPath "$d/tests.json").Hash
    }
}
$clock.Stop()
@{StartedUtc=$started.ToString('o');Seconds=$clock.Elapsed.TotalSeconds;BuildSeconds=$buildClock.Elapsed.TotalSeconds;
  BuildExitCode=$buildExit;ValidateExitCode=$validatorExit;TestsReport=$report;FrozenCompiler=$knownCompiler;
  ValidationBuildCompiler=$buildCompiler;Script=(Get-FileHash -LiteralPath "$work/tools/validate.ps1").Hash;
  LocalOnly=$true} | ConvertTo-Json|Set-Content -LiteralPath "$d/receipt.json"
if($buildExit){exit $buildExit}
exit $validatorExit
