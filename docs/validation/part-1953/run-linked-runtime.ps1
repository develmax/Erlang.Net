$exe='C:/Users/Maksim/AppData/Local/Programs/Python/Python312/python.exe'
$s='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/checked-entry-and-general-event-plans-1953-exact-retake'
& $exe "$s/verify-linked-runtime.py" > "$s/linked-runtime.stdout.log" 2> "$s/linked-runtime.stderr.log"
$rc=$LASTEXITCODE
$rc | Set-Content "$s/linked-runtime.exit"
exit $rc

