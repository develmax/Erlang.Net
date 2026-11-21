$ErrorActionPreference='Stop'
$v='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/forwarded-projections-and-choice-helpers-1923'
Set-Location -LiteralPath 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/precall-shared-frame-and-typed-lists-1861/source'
& pwsh -NoProfile -File "$v/run-linked-scope.ps1" > "$v/linked-scope.stdout.log" 2> "$v/linked-scope.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
& pwsh -NoProfile -File "$v/run-split-focus-and-full.ps1" > "$v/split-orchestration.stdout.log" 2> "$v/split-orchestration.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
& pwsh -NoProfile -File "$v/wait-then-collect-full-evidence.ps1" > "$v/full-evidence-driver.stdout.log" 2> "$v/full-evidence-driver.stderr.log"
exit $LASTEXITCODE


