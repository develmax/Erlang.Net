$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = '0'
$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$ref='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed'
$previous='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/checked-call-frames-and-alias-prefixes-1975-linked-record-return'
$env:ERL_CRASH_DUMP="$root/regression-restoration-crash.dump"
& 'C:/Users/Maksim/AppData/Local/Programs/Python/Python312/python.exe' "$root/prepare-regression-restoration-paths.py" $root $previous $ref
if($LASTEXITCODE){exit $LASTEXITCODE}
& "$ref/bin/erl.exe" +S 1:1 +A 1 -noshell -eval (Get-Content "$root/verify-regression-restorations.eval" -Raw) -extra "$root/regression-restoration-paths.tsv"
exit $LASTEXITCODE
