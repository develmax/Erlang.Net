$ErrorActionPreference='Stop'
$t='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/terminal-records-and-private-ber-shapes-1909'
& pwsh -NoProfile -File "$t/launch-durable-full-corpus.ps1" > "$t/durable-full.stdout.log" 2> "$t/durable-full.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
& pwsh -NoProfile -File "$t/wait-then-collect-full-evidence.ps1" > "$t/durable-evidence.stdout.log" 2> "$t/durable-evidence.stderr.log"
exit $LASTEXITCODE
