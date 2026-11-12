param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'deferred_case_field_probe.erl'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
$outputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
[IO.File]::WriteAllBytes($outputPath, $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = deferred_case_field_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    {module,M} = code:load_binary(M,Source,Beam),
    {payload,{boxed,value}} = M:deferred({ready,payload},value,context),
    value = erlang:erase(last_value),
    {ready,payload} = erlang:erase(last_selector),
    {fallback,context} = M:deferred({wrong,payload},value,context),
    undefined = erlang:erase(last_value),
    {payload,{boxed,value}} = M:mixed({ready,payload},value),
    value = erlang:erase(last_value),
    {stopped,value} = M:mixed(stopped,value),
    undefined = erlang:erase(last_value),
    try M:mixed({ready,payload,extra},value) of _ -> error(expected_case_clause)
    catch error:{case_clause,{ready,payload,extra}} -> ok end,
    {ready,payload,extra} = erlang:erase(last_selector),
    undefined = erlang:erase(last_value),
    {ready,{boxed,payload}} = M:projected({ready,payload}),
    payload = erlang:erase(last_value),
    try M:projected({other,payload}) of _ -> error(expected_case_clause)
    catch error:{case_clause,{other,payload}} -> ok end,
    undefined = erlang:erase(last_value),
    {payload,{boxed,value}} = M:nested({ready,{ignored,payload}},value),
    value = erlang:erase(last_value),
    {stopped,value} = M:nested(stopped,value),
    undefined = erlang:erase(last_value),
    try M:nested({ready,not_a_tuple},value) of _ -> error(expected_case_clause)
    catch error:{case_clause,{ready,not_a_tuple}} -> ok end,
    {ready,not_a_tuple} = erlang:erase(last_selector),
    undefined = erlang:erase(last_value),
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
    end,[{deferred,3},{mixed,2},{projected,1},{nested,2}])
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













