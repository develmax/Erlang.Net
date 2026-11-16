param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'cons_literal_prefix_probe.erl'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
$outputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
[IO.File]::WriteAllBytes($outputPath, $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = cons_literal_prefix_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    {module,M} = code:load_binary(M,Source,Beam),
    {accepted,"42"} = M:route([$- | 42]),
    rejected = M:route([$+ | not_a_list]),
    {accepted,"5"} = M:route(5),
    expected = try M:route("x") of _ -> unexpected
        catch error:badarg -> expected
    end,
    {state,tail} = M:scan([skip|tail],state),
    state = M:scan([stop],state),
    {state,1} = M:scan([other],state),
    {first,second,tail} = M:forward([pass|tail]),
    {length,2} = M:forward([one,two]),
    state = M:keep(state,[keep|tail]),
    {state,2} = M:keep(state,[one,two]),
    true = code:delete(M),
    code:purge(M),
    lists:map(fun({Name,Arity}) ->
        [Body] = [Is || {function,F,A,_,Is} <- Fs, F =:= Name, A =:= Arity],
        [{test,is_nonempty_list,{f,Fallback},_} | _] = [I || I = {test,is_nonempty_list,_,_} <- Body],
        FallbackLabel = {label,Fallback},
        Instructions = lists:takewhile(fun(I) -> I =/= FallbackLabel end,Body) ++ [FallbackLabel],
        Labels = [L || {label,L} <- Instructions],
        Map = maps:from_list(lists:zip(Labels,lists:seq(1,length(Labels)))),
        Rewrite = fun Fix({label,L}) -> {label,maps:get(L,Map,L)};
            Fix({f,L}) -> {f,maps:get(L,Map,L)};
            Fix({literal,_}=Literal) -> Literal;
            Fix(Tuple) when is_tuple(Tuple) -> list_to_tuple([Fix(X) || X <- tuple_to_list(Tuple)]);
            Fix([H|T]) -> [Fix(H)|Fix(T)];
            Fix(Value) -> Value
        end,
        {Name,[Rewrite(I) || I <- Instructions]}
    end,[{route,1},{keep,2}])
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
