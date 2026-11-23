$stage='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/guarded-results-and-case-comprehensions-1945'
& pwsh -NoProfile -File "$stage/case-helper-topology-retake/resume-after-test-update.ps1" > "$stage/case-helper-topology-retake/rebuild.stdout.log" 2> "$stage/case-helper-topology-retake/rebuild.stderr.log"
$rc=$LASTEXITCODE
$rc|Set-Content -LiteralPath "$stage/durable.exit"
exit $rc
