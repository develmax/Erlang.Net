$ErrorActionPreference='Stop'
(Get-Process -Id $PID).PriorityClass='BelowNormal'
$stage='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/field-selectors-and-matched-producers-1919'
$env:DOTNET_PROCESSOR_COUNT='2'
$env:TEMP="$stage/temp"
$env:TMP=$env:TEMP
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES='1'
$env:ERLANG_BEAM_CORPUS_TIMINGS='1'
$identity=Get-Content "$stage/focused-identity.json" -Raw | ConvertFrom-Json
if((Get-FileHash "$stage/corpus-bin/Erlang.Compiler.dll").Hash -cne $identity.Compiler){throw 'Different compiler after path correction'}
$scope=Get-Content "$stage/scope.json" -Raw | ConvertFrom-Json
$timer=[Diagnostics.Stopwatch]::StartNew()
& 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/sdk/dotnet.exe' "$stage/corpus-bin/Erlang.BeamCorpus.dll" 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed' "$stage/pilot.json" 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/public-key-generated' 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/pinned-inputs-full' --modules-file "$stage/pilot-modules.txt" --jobs 2 *> "$stage/pilot.log"
$result=$LASTEXITCODE
$timer.Stop()
if($result -gt 1 -or !(Test-Path "$stage/pilot.json")){exit $result}
$actual=Get-Content "$stage/pilot.json" -Raw | ConvertFrom-Json
if($actual.Total -ne $scope.Modules){throw 'Unexpected corpus coverage'}
@{ExitCode=$result;Seconds=$timer.Elapsed.TotalSeconds;Count=$actual.Total} | ConvertTo-Json | Set-Content "$stage/pilot-receipt.json"
$deadline=(Get-Date).AddMinutes(40)
while(!(Test-Path 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/owned-case-layout-and-shared-receivers-1918/full-uncached/otp-corpus.json')){
    if((Get-Date) -ge $deadline){throw 'Prior corpus has not completed'}
    Start-Sleep -Seconds 5
}
& 'C:/Users/Maksim/AppData/Local/Programs/Python/Python312/python.exe' 'C:/Users/Maksim/.codex/worktrees/beam-release-parity/Erlang.Net/artifacts/parity-next-local/compare-cohort.py' 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/owned-case-layout-and-shared-receivers-1918/full-uncached' "$stage/pilot.json" 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed' "$stage/pilot-delta" > "$stage/pilot-delta.stdout.log" 2> "$stage/pilot-delta.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
& pwsh -NoProfile -File "$stage/run-split-focus-and-full.ps1" > "$stage/split-orchestration.stdout.log" 2> "$stage/split-orchestration.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
& pwsh -NoProfile -File "$stage/wait-then-collect-full-evidence.ps1" > "$stage/full-evidence-driver.stdout.log" 2> "$stage/full-evidence-driver.stderr.log"
exit $LASTEXITCODE
