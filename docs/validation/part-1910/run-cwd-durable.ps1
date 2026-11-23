$u='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/wx-record-liveness-and-ber-entry-1910'
Set-Location -LiteralPath 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/precall-shared-frame-and-typed-lists-1861/source'
& pwsh -NoProfile -File "$u/run-split-cwd-retake.ps1" > "$u/split-orchestration.stdout.log" 2> "$u/split-orchestration.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
& pwsh -NoProfile -File "$u/wait-then-collect-full-evidence.ps1" > "$u/full-evidence-driver.stdout.log" 2> "$u/full-evidence-driver.stderr.log"
exit $LASTEXITCODE
