$ErrorActionPreference='Stop'
(Get-Process -Id $PID).PriorityClass='BelowNormal'
$stage='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/arithmetic-constructors-and-xml-wrappers-1950'
$s=$stage
$full="$stage/full-uncached"
$beam='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed/bin/erl.exe'
$env:ERL_CRASH_DUMP="$stage/recovered-diagnostics-crash.dump"
$timer=[Diagnostics.Stopwatch]::StartNew()
$py='C:/Users/Maksim/AppData/Local/Programs/Python/Python312/python.exe'
& $py "$stage/normalize-frozen-partitions.py" $stage 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118' 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed' > "$stage/normalize-recovery.stdout.log" 2> "$stage/normalize-recovery.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
& $beam +S '1:1' +A 1 -noshell -eval (Get-Content 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/call-and-frame-lifetime-1855-retake/pilot-summary.eval' -Raw) -extra "$full/normalized.terms" > "$full/normalized-summary.terms" 2> "$full/normalized-summary.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
& 'C:/Users/Maksim/AppData/Local/Programs/Python/Python312/python.exe' 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/matched-call-projection-1853/verify-inputs.py' 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed' 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118' 'D:/Distr/Work/Codex/Erlang.Net/otp' > "$stage/input-verification.json" 2> "$stage/input-verification.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
& 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/ordered-roots-includes-1851/collect-build-profiles.ps1' -CorpusBinary "$stage/corpus-bin/Erlang.BeamCorpus.dll" -Report "$full/otp-corpus.json" -OtpRoot 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed' -PublicKeyHeaders 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/public-key-generated' -SourceHeaders 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/pinned-inputs-full' -Output "$stage/windows-build-profiles.json" > "$stage/profile-collection.log" 2> "$stage/profile-collection.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
$dependency='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/tag-continuations-and-directory-exits-1949/windows-build-profiles.json'
$cutoff=(Get-Date).AddMinutes(12)
while(!(Test-Path $dependency) -or (Get-Item $dependency).Length -eq 0){
  if((Get-Date) -gt $cutoff){throw 'Parent full profile collection is incomplete'}
  Start-Sleep -Seconds 5
}
& 'C:/Users/Maksim/AppData/Local/Programs/Python/Python312/python.exe' 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/call-and-frame-lifetime-1855-assertions/verify-effective-build-profiles.py' 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/tag-continuations-and-directory-exits-1949/windows-build-profiles.json' "$stage/windows-build-profiles.json" 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed' > "$stage/profile-equivalence.json" 2> "$stage/profile-equivalence.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
$timer.Stop()
@{Seconds=$timer.Elapsed.TotalSeconds;StartedAt=(Get-Date).AddSeconds(-$timer.Elapsed.TotalSeconds).ToString('o')} | ConvertTo-Json | Set-Content "$full/diagnostics-receipt.json"
Get-Content "$full/compare-summary.json","$full/normalized-summary.terms","$stage/profile-equivalence.json"
$eval='C:/Users/Maksim/.codex/worktrees/beam-release-parity/Erlang.Net/tools/Validation/Erlang.BeamCorpus/rank-opcode-differences.eval'
$refs='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed'
$beam="$refs/bin/erl.exe"
& 'C:/Users/Maksim/AppData/Local/Programs/Python/Python312/python.exe' "$s/partition-rank.py" $s $refs $eval 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/opcode-rank-cache' $beam > "$s/partitioned-rank.stdout.log" 2> "$s/partitioned-rank.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
$receipt=Get-Content "$s/partitioned-rank/receipt.json" -Raw | ConvertFrom-Json
$env:ERL_CRASH_DUMP="$s/partitioned-rank/merge-crash.dump"
& $beam +S '1:1' +A 1 -noshell -eval (Get-Content "$s/merge-rank.eval" -Raw) -extra "$s/partitioned-rank" "$s/function-rank.terms" "$($receipt.Images)" > "$s/partitioned-rank/merge.stdout.log" 2> "$s/partitioned-rank/merge.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
& $beam +S '1:1' +A 1 -noshell -eval (Get-Content "$s/summarize-function-rank.eval" -Raw) -extra "$s/function-rank.terms" "$s/compact-function-rank.terms" > "$s/partitioned-rank/summary.stdout.log" 2> "$s/partitioned-rank/summary.stderr.log"
exit $LASTEXITCODE








