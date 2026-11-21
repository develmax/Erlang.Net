$ErrorActionPreference='Stop'
(Get-Process -Id $PID).PriorityClass='BelowNormal'
$g='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/guarded-results-and-case-comprehensions-1945'
$p='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/response-roots-and-sequential-comprehensions-1944'
$r='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed'
$py='C:/Users/Maksim/AppData/Local/Programs/Python/Python312/python.exe'
$end=(Get-Date).AddMinutes(45)
while(!(Test-Path "$g/full-uncached/timing.json")){
 if((Get-Date) -ge $end){throw 'Full corpus required for physical label census'}
 Start-Sleep -Seconds 6
}
& $py "$g/prepare-final-label-census.py" $g $r > "$g/prepare-label.stdout.log" 2> "$g/prepare-label.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
while(!(Test-Path "$p/final-label-census.exit")){
 if((Get-Date) -ge $end){throw 'Parent physical label proof is unavailable'}
 Start-Sleep -Seconds 5
}
if((Get-Content "$p/final-label-census.exit" -Raw).Trim() -ne '0'){throw 'Parent physical label proof failed'}
& $py "$g/label-census-retake.py" $g $p "$r/bin/erl.exe" > "$g/final-label-census.stdout.log" 2> "$g/final-label-census.stderr.log"
$LASTEXITCODE | Set-Content "$g/final-label-census.exit"
exit $LASTEXITCODE




