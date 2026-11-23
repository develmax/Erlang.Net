$ErrorActionPreference = 'Stop'
(Get-Process -Id $PID).PriorityClass = 'BelowNormal'
$stage='D:\Distr\Work\Codex\Erlang.Net\Erlang.Net\artifacts\parity-inputs-20261118\bootstrap-and-sibling-generator-1878\compare-1875'
$full=$stage
$beam='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed/bin/erl.exe'
$env:ERL_CRASH_DUMP="$stage/pilot-diagnostic-crash.dump"
$timer=[Diagnostics.Stopwatch]::StartNew()
& 'C:/Users/Maksim/AppData/Local/Programs/Python/Python312/python.exe' 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/map-result-root-1846/section-delta.py' 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/snmp-provenance-1845/beam_chunk_inventory.py' "$full/pilot-delta.tsv" | Set-Content "$full/pilot-section-delta.json"
if($LASTEXITCODE){exit $LASTEXITCODE}
& $beam +S '1:1' +A 1 -noshell -eval (Get-Content 'C:/Users/Maksim/.codex/worktrees/beam-leaf-heap-roots/Erlang.Net/artifacts/parity-part-1833/validation-loader-final.eval' -Raw) -extra "$full/pilot-delta.tsv" > "$full/pilot-loader.terms" 2> "$full/pilot-loader.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
$normalized='C:/Users/Maksim/.codex/worktrees/beam-release-parity/Erlang.Net/docs/validation/part-1832/cached-normalized-delta.eval'
& $beam +S '1:1' +A 1 -noshell -eval (Get-Content $normalized -Raw) -extra "$full/pilot-delta.tsv" 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/singleton-returns-1840/part1839-cumulative-normalization/cache' (Get-FileHash $normalized).Hash > "$full/pilot-normalized.terms" 2> "$full/pilot-normalized.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
& $beam +S '1:1' +A 1 -noshell -eval (Get-Content 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/call-and-frame-lifetime-1855-retake/pilot-summary.eval' -Raw) -extra "$full/pilot-normalized.terms" > "$full/pilot-summary.terms" 2> "$full/pilot-summary.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
$timer.Stop()
@{Seconds=$timer.Elapsed.TotalSeconds;StartedAt=(Get-Date).AddSeconds(-$timer.Elapsed.TotalSeconds).ToString('o')} | ConvertTo-Json | Set-Content "$full/pilot-diagnostics-receipt.json"
& $beam +S '1:1' +A 1 -noshell -eval (Get-Content 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/call-and-frame-lifetime-1855-assertions/loader-summary.eval' -Raw) -extra "$stage/pilot-loader.terms" > "$stage/pilot-loader-summary.terms"









