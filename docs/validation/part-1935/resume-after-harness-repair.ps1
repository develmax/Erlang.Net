$d='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/proof-elision-and-destinations-1935'
Set-Location -LiteralPath 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/precall-shared-frame-and-typed-lists-1861/source'
& pwsh -NoProfile -File "$d/resume-complete.ps1" > "$d/durable.stdout.log" 2> "$d/durable.stderr.log"
$rc=$LASTEXITCODE
$rc|Set-Content "$d/durable.exit"
exit $rc
