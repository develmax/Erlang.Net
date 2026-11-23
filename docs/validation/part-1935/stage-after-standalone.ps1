$ErrorActionPreference='Stop'
$stage='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/proof-elision-and-destinations-1935'
$parent='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/closed-inets-and-counted-positions-1934'
if(!(Test-Path "$parent/full-launch.json")){throw 'Frozen parent evidence is incomplete'}
& 'C:/Users/Maksim/AppData/Local/Programs/Python/Python312/python.exe' "$stage/declare-intake.py" $stage 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/asn-named-bits-replay-1931' 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/binary-match-audit-1931' 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/mod-head-field-heap-1935' > "$stage/declare-intake.stdout.log" 2> "$stage/declare-intake.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
$tests=Get-Content "$stage/test-patches.txt"
& 'C:/Users/Maksim/AppData/Local/Programs/Python/Python312/python.exe' "$stage/prepare-agent-intake.py" $stage $parent 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/matched-contexts-and-call-liveness-1933' 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/precall-shared-frame-and-typed-lists-1861/source' @tests > "$stage/prepare-agent-intake.log" 2> "$stage/prepare-agent-intake.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
& 'C:/Users/Maksim/AppData/Local/Programs/Python/Python312/python.exe' "$stage/merge-harness-hunks.py" $stage 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/precall-shared-frame-and-typed-lists-1861/source' > "$stage/merged-harness.stdout.log" 2> "$stage/merged-harness.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
$ordered=Get-Content "$stage/intake-replay-order.txt"
& 'C:/Users/Maksim/AppData/Local/Programs/Python/Python312/python.exe' "$stage/strict-preflight.py" $stage 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/precall-shared-frame-and-typed-lists-1861/source' 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/precall-shared-frame-and-typed-lists-1861/artifact-git' @ordered > "$stage/preflight.stdout.log" 2> "$stage/preflight.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
& 'C:/Users/Maksim/AppData/Local/Programs/Python/Python312/python.exe' "$stage/prepare-linked-scope.py" $stage $parent 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/precall-shared-frame-and-typed-lists-1861/source' 'C:/Users/Maksim/.codex/worktrees/beam-release-parity/Erlang.Net' @ordered > "$stage/prepare-scope.stdout.log" 2> "$stage/prepare-scope.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
& pwsh -NoProfile -File "$stage/run-durable.ps1"
exit $LASTEXITCODE