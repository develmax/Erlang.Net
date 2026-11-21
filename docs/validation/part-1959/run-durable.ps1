$stage='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/checked-projections-and-protected-entry-1959-retake'
& (Get-Process -Id $PID).Path -NoProfile -File "$stage/complete-test-only-retake.ps1" > "$stage/durable.stdout.log" 2> "$stage/durable.stderr.log"
$LASTEXITCODE|Set-Content "$stage/durable.exit"
exit $LASTEXITCODE