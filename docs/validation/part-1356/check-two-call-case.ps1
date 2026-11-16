param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'two_call_case_probe.erl'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
$outputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
[IO.File]::WriteAllBytes($outputPath, $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = two_call_case_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    {module,M} = code:load_binary(M,Source,Beam),
    {number,3} = M:rounded(2.5),
    2.5 = erlang:erase(value),
    {number,-2} = M:rounded(-2.5),
    -2.5 = erlang:erase(value),
    {number,-2} = M:rounded_list(key,[-2.5]),
    -2.5 = erlang:erase(key),
    try M:rounded_list(key,[1,2]) of _ -> error(expected_function_clause)
    catch error:function_clause -> ok end,
    undefined = erlang:erase(key),
    try M:rounded(no_number) of _ -> error(expected_badarg)
    catch error:badarg -> ok end,
    no_number = erlang:erase(value),
    accepted = M:bound(ready),
    ready = erlang:erase(bound),
    fallback = M:bound(no),
    no = erlang:erase(bound),
    accepted = M:nested(a),
    a = erlang:erase(nested),
    fallback = M:nested(c),
    c = erlang:erase(nested),
    try M:rebound(other) of _ -> error(expected_badmatch)
    catch error:{badmatch,ready} -> ok end,
    ready = erlang:erase(rebound),
    {number,1} = M:shadowed(4.5),
    4.5 = erlang:erase(shadowed),
    {accepted,a} = M:preserved(a),
    a = erlang:erase(preserved),
    {fallback,c} = M:preserved(c),
    c = erlang:erase(preserved),
    {accepted,b} = M:disjoint(b),
    b = erlang:erase(disjoint),
    {fallback,c} = M:disjoint(c),
    c = erlang:erase(disjoint),
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
    end,[{rounded,1},{rounded_list,2},{bound,1},{nested,1},{preserved,1},{disjoint,1}])
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








