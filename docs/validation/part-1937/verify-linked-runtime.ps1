$d='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/cons-inputs-and-measured-choice-1937'
& 'C:/Users/Maksim/AppData/Local/Programs/Python/Python312/python.exe' "$d/verify-linked-runtime.py" > "$d/linked-runtime.stdout.log" 2> "$d/linked-runtime.stderr.log"
$rc=$LASTEXITCODE
$rc|Set-Content "$d/linked-runtime.exit"
exit $rc



