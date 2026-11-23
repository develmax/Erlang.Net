$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = '0'
$ErrorActionPreference='Stop'
$s='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-tuples-and-projected-entry-liveness-1961-linked-retake'
$env:ERL_CRASH_DUMP="$s/scoped-disasm-crash.dump"
$paths="$s/pilot-delta.tsv"
$errFile="$s/scoped-disasm-failures.terms"
& 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed/bin/erl.exe' +S '1:1' +A 1 -noshell -eval (Get-Content "$s/check-disassembler-role.eval" -Raw) -extra $paths $errFile > "$s/scoped-disasm.stdout.log" 2> "$s/scoped-disasm.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
if((Get-Content $errFile -Raw).Trim() -cne '[].'){throw 'The pinned disassembler rejected a scoped image'}
$rows=@(Get-Content $paths | Where-Object {$_})
@{Images=$rows.Count;Roles=3;InputSHA=(Get-FileHash $paths).Hash;Compiler=(Get-FileHash "$s/corpus-bin/Erlang.Compiler.dll").Hash} |
    ConvertTo-Json | Set-Content "$s/scoped-disasm-receipt.json"
exit 0



