$s='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/frame-and-generator-planning-1929-retake'
& pwsh -NoProfile -File "$s/complete-durable.ps1" > "$s/durable.stdout.log" 2> "$s/durable.stderr.log"
$rc=$LASTEXITCODE
$rc|Set-Content "$s/durable.exit"
exit $rc
