$s='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-failures-and-shared-match-lifetime-1956-retake'
& (Get-Process -Id $PID).Path -NoProfile -File "$s/rank-after-normalized-block.ps1" > "$s/blocked-rank.stdout.log" 2> "$s/blocked-rank.stderr.log"
$LASTEXITCODE | Set-Content "$s/blocked-rank.exit"
exit $LASTEXITCODE