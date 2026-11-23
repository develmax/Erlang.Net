$ErrorActionPreference='Stop'
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH='0'
$root=$PSScriptRoot
$inputs=Split-Path -Parent $root
$accepted='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/shared-returns-and-initialized-call-roots-1965-linked'
$otp='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed/bin/erl.exe'
$env:ERL_CRASH_DUMP="$root/corrected-pilot-crash.dump"
$pilot=Get-Content "$root/pilot.json" -Raw | ConvertFrom-Json
$monitor=@($pilot.Modules | Where-Object {$_.Beam -like '*megaco_monitor.beam'})
if($monitor.Count -ne 1 -or $monitor[0].Status -ne 'Equal'){throw 'The linked compiler lost the exact monitor image'}
& 'C:/Users/Maksim/AppData/Local/Programs/Python/Python312/python.exe' "$root/prepare-narrow-correction-comparison.py" $accepted "$root/pilot.json" "$root/corrected-pilot-pairs" > "$root/corrected-pilot-prep.log" 2> "$root/corrected-pilot-prep.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
$identity=(Get-FileHash "$inputs/bounded-normalization-workloads-1921/serial-reference.eval").Hash.ToLowerInvariant()
& $otp +S 1:1 +SDcpu 1 +SDio 1 +A 1 -noshell -eval (Get-Content "$root/cached-normalized-delta.eval" -Raw) -extra "$root/corrected-pilot-pairs.tsv" "$inputs/singleton-returns-1840/part1839-cumulative-normalization/cache" $identity 1 "$inputs/normalization-content-cache-1953" > "$root/corrected-pilot-normalized.terms" 2> "$root/corrected-pilot-normalized.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
& $otp +S 1:1 +SDcpu 1 +SDio 1 +A 1 -noshell -eval (Get-Content "$root/verify-narrow-correction.eval" -Raw) -extra "$root/corrected-pilot-normalized.terms" > "$root/corrected-pilot-verdict.terms" 2> "$root/corrected-pilot-verdict.stderr.log"
exit $LASTEXITCODE
