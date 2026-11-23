$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = '0'
$ErrorActionPreference = 'Stop'
(Get-Process -Id $PID).PriorityClass = 'BelowNormal'
$stage='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/deferred-construction-and-result-ownership-1972-linked-owned-frame'
$full="$stage/full-uncached"
$prior='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/constructor-fallthrough-and-capture-order-1971-linked-qualified-finite-entry-retake/full-uncached'
$beam='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed/bin/erl.exe'
$env:ERL_CRASH_DUMP="$stage/full-diagnostic-crash.dump"
$timer=[Diagnostics.Stopwatch]::StartNew()
& 'C:/Users/Maksim/AppData/Local/Programs/Python/Python312/python.exe' 'C:/Users/Maksim/.codex/worktrees/beam-release-parity/Erlang.Net/artifacts/parity-next-local/compare-frozen-corpora.py' $prior $full 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed' | Set-Content "$full/compare-summary.json"
if($LASTEXITCODE){exit $LASTEXITCODE}
& 'C:/Users/Maksim/AppData/Local/Programs/Python/Python312/python.exe' 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/map-result-root-1846/section-delta.py' 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/snmp-provenance-1845/beam_chunk_inventory.py' "$full/changed-paths.tsv" | Set-Content "$full/section-delta.json"
if($LASTEXITCODE){exit $LASTEXITCODE}
& 'C:/Users/Maksim/AppData/Local/Programs/Python/Python312/python.exe' 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/matched-call-projection-1853/rank-input-and-chunks.py' 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/snmp-provenance-1845/beam_chunk_inventory.py' 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed' $full "$full/residual-sections.json" "$stage/rank-input" > "$full/residual-sections.log" 2> "$full/residual-sections.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
& $beam +S '1:1' +A 1 -noshell -eval (Get-Content 'C:/Users/Maksim/.codex/worktrees/beam-leaf-heap-roots/Erlang.Net/artifacts/parity-part-1833/validation-loader-final.eval' -Raw) -extra "$full/changed-paths.tsv" > "$full/loader.terms" 2> "$full/loader.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
& 'C:/Users/Maksim/AppData/Local/Programs/Python/Python312/python.exe' "$stage/normalize-frozen-partitions.py" $stage 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118' 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed' > "$stage/normalize-bounded.stdout.log" 2> "$stage/normalize-bounded.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
& $beam +S '1:1' +A 1 -noshell -eval (Get-Content 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/call-and-frame-lifetime-1855-retake/pilot-summary.eval' -Raw) -extra "$full/normalized.terms" > "$full/normalized-summary.terms" 2> "$full/normalized-summary.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
& 'C:/Users/Maksim/AppData/Local/Programs/Python/Python312/python.exe' 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/matched-call-projection-1853/verify-inputs.py' 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed' 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118' 'D:/Distr/Work/Codex/Erlang.Net/otp' > "$stage/input-verification.json" 2> "$stage/input-verification.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
& 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/ordered-roots-includes-1851/collect-build-profiles.ps1' -CorpusBinary "$stage/corpus-bin/Erlang.BeamCorpus.dll" -Report "$full/otp-corpus.json" -OtpRoot 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed' -PublicKeyHeaders 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/public-key-generated' -SourceHeaders 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/pinned-inputs-full' -Output "$stage/windows-build-profiles.json" > "$stage/profile-collection.log" 2> "$stage/profile-collection.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
$dependency='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/constructor-fallthrough-and-capture-order-1971-linked-qualified-finite-entry-retake/windows-build-profiles.json'
$cutoff=(Get-Date).AddMinutes(12)
while(!(Test-Path $dependency) -or (Get-Item $dependency).Length -eq 0){
  if((Get-Date) -gt $cutoff){throw 'Parent full profile collection is incomplete'}
  Start-Sleep -Seconds 5
}
& 'C:/Users/Maksim/AppData/Local/Programs/Python/Python312/python.exe' 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/call-and-frame-lifetime-1855-assertions/verify-effective-build-profiles.py' 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/constructor-fallthrough-and-capture-order-1971-linked-qualified-finite-entry-retake/windows-build-profiles.json' "$stage/windows-build-profiles.json" 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed' > "$stage/profile-equivalence.json" 2> "$stage/profile-equivalence.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
$timer.Stop()
@{Seconds=$timer.Elapsed.TotalSeconds;StartedAt=(Get-Date).AddSeconds(-$timer.Elapsed.TotalSeconds).ToString('o')} | ConvertTo-Json | Set-Content "$full/diagnostics-receipt.json"
Get-Content "$full/compare-summary.json","$full/normalized-summary.terms","$stage/profile-equivalence.json"
