$ErrorActionPreference='Stop'
(Get-Process -Id $PID).PriorityClass='BelowNormal'
$s='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-failures-and-shared-match-lifetime-1956-retake'
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








