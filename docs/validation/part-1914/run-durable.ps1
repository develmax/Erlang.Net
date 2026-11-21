$v='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/dispatch-and-projected-supervisor-1914'
& pwsh -NoProfile -File "$v/complete-durable.ps1" > "$v/durable.stdout.log" 2> "$v/durable.stderr.log"
exit $LASTEXITCODE

