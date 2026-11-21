$ErrorActionPreference='Stop'
$s='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/ber-continuations-and-owned-frame-frontiers-1958'
$p='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/boolean-branches-and-value-lifetimes-1957-retake'
if((Get-Content "$p/finalize-receipts.exit" -Raw).Trim() -ne '0'){throw 'Parent release evidence is not verified'}
if(Test-Path "$s/full-launch.json"){throw 'An uncached run already started'}
if((Get-Content "$s/durable-recovery.exit" -Raw).Trim() -ne '1'){throw 'Earlier parent failure record missing'}
& pwsh -NoProfile -File "$s/launch-after-split-focus.ps1" > "$s/full-after-parent-recovery.stdout.log" 2> "$s/full-after-parent-recovery.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
& pwsh -NoProfile -File "$s/wait-then-collect-full-evidence.ps1" > "$s/full-after-parent-evidence.stdout.log" 2> "$s/full-after-parent-evidence.stderr.log"
exit $LASTEXITCODE