$ErrorActionPreference='Stop'
$stage='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/response-roots-and-sequential-comprehensions-1944'
& pwsh -NoProfile -File "$stage/launch-after-split-focus.ps1" > "$stage/full-orchestration.stdout.log" 2> "$stage/full-orchestration.stderr.log"
$rc=$LASTEXITCODE
if(!$rc){
 & pwsh -NoProfile -File "$stage/wait-then-collect-full-evidence.ps1" > "$stage/full-evidence-driver.stdout.log" 2> "$stage/full-evidence-driver.stderr.log"
 $rc=$LASTEXITCODE
}
$rc | Set-Content -LiteralPath "$stage/durable.exit"
exit $rc
