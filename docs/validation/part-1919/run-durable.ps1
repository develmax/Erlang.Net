$v='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/field-selectors-and-matched-producers-1919'
& pwsh -NoProfile -File "$v/complete-durable.ps1" > "$v/durable.stdout.log" 2> "$v/durable.stderr.log"
exit $LASTEXITCODE

