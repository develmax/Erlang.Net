
$ErrorActionPreference='Stop'
(Get-Process -Id $PID).PriorityClass='BelowNormal'
$stage='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/source-proven-tuple-result-types-1864'
$out="$stage/native-fixtures"
$reference=New-Item "$out/reference" -ItemType Directory
$candidate=New-Item "$out/candidate" -ItemType Directory
$cli="$out/tool"
Copy-Item -LiteralPath 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/frameless-tagged-fallback-1863/native-fixtures/tool' -Destination $cli -Recurse
Copy-Item -LiteralPath "$stage/frozen-payload/Erlang.Compiler.dll" -Destination "$cli/Erlang.Compiler.dll" -Force
$hash=(Get-Content "$stage/source-freeze.json" -Raw | ConvertFrom-Json).Compiler
if((Get-FileHash "$cli/Erlang.Compiler.dll").Hash -ne $hash){throw 'Fixture compiler identity differs'}
$prefix='[P]=init:get_plain_arguments(),{module,M}=code:load_abs(filename:rootname(P)),{beam_file,M,_,_,_,Fs}=beam_disasm:file(P),'
$tail='io:format("~0p.~n",[ok]),halt().'
$atom='beam_adjacent_numeric=M,one=M:value(found,one),absent=M:value(missing,one),absent=M:value(extra,one),Err=try M:value(refused,one) of A->error({unexpected,A}) catch error:R->R end,io:format("~0p.~n",[Err]),'
$key='beam_keyfind_tuple_type=M,T=erl_syntax:set_ann(erl_syntax:atom(probe),[{env,value}]),{T,value}=M:lookup(T),badarg=try M:lookup(erl_syntax:atom(probe)) of Wrong->error({unexpected,Wrong}) catch error:Reason->Reason end,'
$checkType='[Type]=[Input||{function,FN,FA,_,Ops}<-Fs,FN=:=FUNCTION,FA=:=ARITY,{test,is_tuple,_,[Input]}<-Ops],OPERAND=Type,'
$cases=@(
    @{Name='beam_keyfind_tuple_type';Eval=$prefix+$key+$checkType.Replace('FUNCTION','lookup').Replace('ARITY','1').Replace('OPERAND','{x,0}')+$tail}
    @{Name='beam_adjacent_numeric';Eval=$prefix+$atom+$checkType.Replace('FUNCTION','value').Replace('ARITY','2').Replace('OPERAND','{tr,{x,0},{t_union,{t_atom,any},none,none,{t_tuple,_,_,_},none,none}}')+$tail}
)
$env:ERL_FLAGS='+S 1:1 +SDcpu 1 +SDio 1 +A 1'
$receipts=@(
foreach($case in $cases){
    $name=$case.Name
    $src="$out/$name.erl"
    & 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed/bin/erlc.exe' -o $reference.FullName $src *> "$out/$name-reference-build.log"
    if($LASTEXITCODE){exit $LASTEXITCODE}
    & 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/sdk/dotnet.exe' "$cli/Erlang.Tool.dll" compile-beam $src "$candidate/$name.beam" *> "$out/$name-candidate-build.log"
    if($LASTEXITCODE){exit $LASTEXITCODE}
    $results=@(foreach($side in $reference,$candidate){
        $result="$out/$name-$($side.Name).terms"
        $env:ERL_CRASH_DUMP="$out/$name-$($side.Name).dump"
        & 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed/bin/erl.exe' -noshell -eval $case.Eval -extra "$side/$name.beam" > $result 2> "$out/$name-$($side.Name).stderr.log"
        if($LASTEXITCODE){exit $LASTEXITCODE}
        (Get-FileHash $result).Hash
    })
    if($results[0] -cne $results[1]){throw "Native fixture observation differs: $name"}
    @{Fixture=$name;Compiler=$hash;Source=(Get-FileHash $src).Hash;Observation=$results[0]}
}
)
$receipts | ConvertTo-Json | Set-Content "$out/receipt.json"

