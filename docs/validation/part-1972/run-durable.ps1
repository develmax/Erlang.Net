$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = '0'
$d='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/deferred-construction-and-result-ownership-1972-linked-owned-frame'
Set-Location -LiteralPath 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/precall-shared-frame-and-typed-lists-1861/source'
& pwsh -NoProfile -File "$d/complete-durable.ps1" > "$d/durable.stdout.log" 2> "$d/durable.stderr.log"
$rc=$LASTEXITCODE
$rc|Set-Content "$d/durable.exit"
exit $rc
