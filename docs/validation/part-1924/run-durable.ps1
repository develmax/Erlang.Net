$v='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/list-values-and-register-projections-1924'
& pwsh -NoProfile -File "$v/complete-durable.ps1" > "$v/durable.stdout.log" 2> "$v/durable.stderr.log"
exit $LASTEXITCODE

