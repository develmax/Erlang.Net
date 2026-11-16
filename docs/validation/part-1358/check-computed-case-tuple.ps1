param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'computed_case_tuple_probe.erl'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
$outputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
[IO.File]::WriteAllBytes($outputPath, $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = computed_case_tuple_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    {module,M} = code:load_binary(M,Source,Beam),
    {wrapped,{boxed,one}} = M:inline(active,one),
    active = erlang:erase(last_selector),
    one = erlang:erase(last_value),
    two = M:inline(stopped,two),
    stopped = erlang:erase(last_selector),
    undefined = erlang:erase(last_value),
    try M:inline(other,two) of _ -> error(expected_case_clause)
    catch error:{case_clause,other} -> ok end,
    other = erlang:erase(last_selector),
    {wrapped,context,{boxed,value},{boxed,value}} = M:bound(active,value,context),
    value = erlang:erase(last_value),
    {stopped,context} = M:bound(stopped,value,context),
    undefined = erlang:erase(last_value),
    {wrapped,{boxed,item},item} = M:expired(active,item),
    item = erlang:erase(last_value),
    {wrapped,{boxed,found}} = M:projected(key,#{key => found}),
    found = erlang:erase(last_value),
    missing = M:projected(key,#{}),
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
    end,[{inline,2},{bound,3},{expired,2},{projected,2}])
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











