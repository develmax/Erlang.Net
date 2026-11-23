$stage='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/proof-elision-and-destinations-1935'
& pwsh -NoProfile -File "$stage/stage-after-standalone.ps1" > "$stage/intake-driver.stdout.log" 2> "$stage/intake-driver.stderr.log"
$LASTEXITCODE|Set-Content "$stage/intake-driver.exit"
exit $LASTEXITCODE