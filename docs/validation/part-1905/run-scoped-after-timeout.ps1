$ErrorActionPreference='Stop'
(Get-Process -Id $PID).PriorityClass='BelowNormal'
$stage='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/two-atom-dispatch-and-owned-result-trims-1905'
$work='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/precall-shared-frame-and-typed-lists-1861/source'
$sdk='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/sdk/dotnet.exe'
$env:TEMP="$stage/temp"
$env:TMP=$env:TEMP
$env:DOTNET_PROCESSOR_COUNT='1'
Set-Location -LiteralPath $work
$compilerHash=(Get-Content "$stage/focused-identity.json" -Raw | ConvertFrom-Json).Compiler
$catalog=Get-Content "$stage/scope.json" -Raw | ConvertFrom-Json
$focus=Get-Content "$stage/focused.json" -Raw | ConvertFrom-Json
$failed=@($focus.Tests|Where-Object Status -eq Failed)
if($focus.Passed -ne 109 -or $failed.Count -ne 1 -or $failed[0].Name -ne 'compiler/beam/whole-module-matches-otp' -or
    $failed[0].Error -notmatch '^System.TimeoutException'){throw 'Unexpected first focused result'}
$toolBuild=@('--no-dependencies','-m:1','-p:UseSharedCompilation=false','-p:ConcurrentBuild=false')
& $sdk build tools/Validation/Erlang.BeamCorpus/Erlang.BeamCorpus.csproj @toolBuild *> "$stage/corpus-tool-build.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
Copy-Item -LiteralPath "$work/tools/Validation/Erlang.BeamCorpus/bin/Debug/net10.0" -Destination "$stage/corpus-bin" -Recurse
if((Get-FileHash "$stage/corpus-bin/Erlang.Compiler.dll").Hash -cne $compilerHash){throw 'Compiler copied into corpus differs'}
$env:DOTNET_PROCESSOR_COUNT='2'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES='1'
$env:ERLANG_BEAM_CORPUS_TIMINGS='1'
$timer=[Diagnostics.Stopwatch]::StartNew()
& $sdk "$stage/corpus-bin/Erlang.BeamCorpus.dll" 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed' "$stage/pilot.json" 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/public-key-generated' 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/pinned-inputs-full' --modules-file "$stage/pilot-modules.txt" --jobs 2 *> "$stage/pilot.log"
$result=$LASTEXITCODE
$timer.Stop()
@{ExitCode=$result;Seconds=$timer.Elapsed.TotalSeconds;Count=$catalog.Modules} | ConvertTo-Json | Set-Content "$stage/pilot-receipt.json"
if($result -gt 1 -or !(Test-Path "$stage/pilot.json")){exit $result}
& 'C:/Users/Maksim/AppData/Local/Programs/Python/Python312/python.exe' 'C:/Users/Maksim/.codex/worktrees/beam-release-parity/Erlang.Net/artifacts/parity-next-local/compare-cohort.py' 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/rooted-records-and-local-tail-lines-1902/full-uncached' "$stage/pilot.json" 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed' "$stage/pilot-delta"
exit $LASTEXITCODE








