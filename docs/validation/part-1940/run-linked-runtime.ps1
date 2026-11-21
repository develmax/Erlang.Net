$exe='C:/Users/Maksim/AppData/Local/Programs/Python/Python312/python.exe'
& $exe 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/bounded-entry-and-generator-calls-1940/verify-linked-runtime.py' > 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/bounded-entry-and-generator-calls-1940/linked-runtime.stdout.log' 2> 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/bounded-entry-and-generator-calls-1940/linked-runtime.stderr.log'
$result=$LASTEXITCODE
$result | Set-Content 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/bounded-entry-and-generator-calls-1940/linked-runtime.exit'
exit $result
