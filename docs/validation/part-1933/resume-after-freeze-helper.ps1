$ErrorActionPreference='Stop'
$s='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/matched-contexts-and-call-liveness-1933'
$p='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/continuations-and-protected-dispatch-1932'
$focused=Get-Content "$s/focused-identity.json" -Raw|ConvertFrom-Json
if((Get-FileHash "$s/frozen-payload/Erlang.Tests.dll").Hash -ne $focused.Tests){throw 'Frozen test payload changed'}
if((Get-FileHash "$s/frozen-payload/Erlang.Compiler.dll").Hash -ne $focused.Compiler){throw 'Frozen compiler payload changed'}
& 'C:/Users/Maksim/AppData/Local/Programs/Python/Python312/python.exe' "$s/freeze-source.py" $s $p $s 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/precall-shared-frame-and-typed-lists-1861/source' > "$s/freeze.log" 2> "$s/freeze.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
$receipt=Get-Content "$s/source-freeze.json" -Raw|ConvertFrom-Json
$early=Get-Content "$s/early-release-control-verified/receipt.json" -Raw|ConvertFrom-Json
if($receipt.ParentSource -ne (Get-Content "$p/source-freeze.json" -Raw|ConvertFrom-Json).Source -or
   $receipt.Compiler -ne $early.Compiler -or $receipt.Tests -ne $focused.Tests){throw 'Retaken source and tests do not bind'}
Copy-Item -LiteralPath "$p/assembly-source-verification.json" -Destination $s
& pwsh -NoProfile -File "$s/launch-after-split-focus.ps1" > "$s/full-orchestration.stdout.log" 2> "$s/full-orchestration.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
& pwsh -NoProfile -File "$s/wait-then-collect-full-evidence.ps1" > "$s/full-evidence-driver.stdout.log" 2> "$s/full-evidence-driver.stderr.log"
$LASTEXITCODE|Set-Content "$s/durable.exit"
exit $LASTEXITCODE