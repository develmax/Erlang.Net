$ErrorActionPreference='Stop'
(Get-Process -Id $PID).PriorityClass='BelowNormal'
$s='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/list-values-and-register-projections-1924'
$full="$s/full-uncached"
$previous='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/forwarded-projections-and-choice-helpers-1923'
$required=@("$full/compare-summary.json","$full/normalized-summary.terms","$full/loader.terms","$s/input-verification.json","$s/windows-build-profiles.json","$previous/windows-build-profiles.json")
foreach($file in $required){if(!(Test-Path $file) -or (Get-Item $file).Length -eq 0){throw "Missing prior completed operation $file"}}
$clock=[Diagnostics.Stopwatch]::StartNew()
& 'C:/Users/Maksim/AppData/Local/Programs/Python/Python312/python.exe' 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/call-and-frame-lifetime-1855-assertions/verify-effective-build-profiles.py' "$previous/windows-build-profiles.json" "$s/windows-build-profiles.json" 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed' > "$s/profile-equivalence.json" 2> "$s/profile-equivalence.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
@{Seconds=$clock.Elapsed.TotalSeconds;Resumed='Profile equivalence after previous profile receipt';PriorNormalization=(Get-FileHash "$full/normalization-timing.json").Hash} | ConvertTo-Json | Set-Content "$full/diagnostics-receipt.json"
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



