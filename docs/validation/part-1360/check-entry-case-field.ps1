param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'entry_case_field_probe.erl'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
$outputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
[IO.File]::WriteAllBytes($outputPath, $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = entry_case_field_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    {module,M} = code:load_binary(M,Source,Beam),
    {uri,local} = M:record_lookup(key,"local",{ctx,scope,[{key,uri}]}),
    [] = M:record_lookup(missing,"local",{ctx,scope,[{key,uri}]}),
    try M:record_lookup(key,"local",{wrong,scope,[{key,uri}]}) of _ -> error(expected_function_clause)
    catch error:function_clause -> ok end,
    {found,value} = M:tuple_lookup(key,{pair,#{key => value}}),
    missing = M:tuple_lookup(key,{pair,#{}}),
    try M:tuple_lookup(key,{pair,#{},extra}) of _ -> error(expected_function_clause)
    catch error:function_clause -> ok end,
    {found,value} = M:private_lookup(key,#{key => value}),
    missing = M:private_lookup(key,#{}),
    yes = M:guarded_lookup(good,{pair,value},fallback),
    value = erlang:erase(last_value),
    fallback = M:guarded_lookup(other,{pair,value},fallback),
    value = erlang:erase(last_value),
    try M:guarded_lookup(123,{pair,value},fallback) of _ -> error(expected_function_clause)
    catch error:function_clause -> ok end,
    undefined = erlang:erase(last_value),
    {outer,{found,value}} = M:shadow_lookup({pair,outer},{pair,#{key => value}}),
    try M:shadow_lookup({pair,outer},wrong) of _ -> error(expected_function_clause)
    catch error:function_clause -> ok end,
    true = code:delete(M),
    code:purge(M),
    lists:map(fun({Name,Arity}) ->
        [Instructions] = [Is || {function,F,A,_,Is} <- Fs, F =:= Name, A =:= Arity],
        Labels = [L || {label,L} <- Instructions],
        Map = maps:from_list(lists:zip(Labels,lists:seq(1,length(Labels)))),
        Rewrite = fun Fix({label,L}) -> {label,maps:get(L,Map,L)};
            Fix({f,L}) -> {f,maps:get(L,Map,L)};
            Fix({literal,_}=Literal) -> Literal;
            Fix({make_fun3,Target,Index,_,Dst,Args}) -> {make_fun3,Target,Index,uniq,Fix(Dst),Fix(Args)};
            Fix(Tuple) when is_tuple(Tuple) -> list_to_tuple([Fix(X) || X <- tuple_to_list(Tuple)]);
            Fix([H|T]) -> [Fix(H)|Fix(T)];
            Fix(Value) -> Value
        end,
        {{Name,Arity},[Rewrite(I) || I <- Instructions]}
    end,[{record_lookup,3},{tuple_lookup,2},{internal,2}])
end,
Expected = Check(Reference),
Actual = Check(Generated),
case Actual of
    Expected -> io:format("~p~n",[Actual]), halt();
    _ -> io:format("OTP ~p~nC# ~p~n",[Expected,Actual]), halt(1)
end.
"@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $outputPath
exit $LASTEXITCODE















