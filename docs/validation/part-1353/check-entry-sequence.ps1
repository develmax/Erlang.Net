param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'entry_sequence_probe.erl'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
$outputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
[IO.File]::WriteAllBytes($outputPath, $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = entry_sequence_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    {module,M} = code:load_binary(M,Source,Beam),
    {done,tail,{{boxed,{value,item}},acc}} = M:walk([{value,item}|tail],acc),
    {done,other,acc} = M:walk(other,acc),
    {done,tail,{wrapped,[{boxed,{value,item}}]}} = M:pack([{value,item}|tail]),
    {rest,other} = M:pack(other),
    {tail,{boxed,left},{boxed,right}} = M:branch([{item,left,right}|tail]),
    {rest,[{wrong,left,right}]} = M:branch([{wrong,left,right}]),
    true = code:delete(M),
    code:purge(M),
    [Branch] = [Is || {function,branch,1,_,Is} <- Fs],
    Prefix = lists:takewhile(fun(I) -> I =/= {call,1,{M,box,1}} end,Branch),
    [Input] = [Reg || {get_tuple_element,_,1,Reg} <- Prefix],
    false = lists:any(fun({move,Source,{y,_}}) -> Source =:= Input; (_) -> false end,Prefix),
    lists:map(fun({Name,Arity}) ->
        [Instructions] = [Is || {function,F,A,_,Is} <- Fs, F =:= Name, A =:= Arity],
        Labels = [L || {label,L} <- Instructions],
        Map = maps:from_list(lists:zip(Labels,lists:seq(1,length(Labels)))),
        Rewrite = fun Fix({label,L}) -> {label,maps:get(L,Map,L)};
            Fix({f,L}) -> {f,maps:get(L,Map,L)};
            Fix({literal,_}=Literal) -> Literal;
            Fix(Tuple) when is_tuple(Tuple) -> list_to_tuple([Fix(X) || X <- tuple_to_list(Tuple)]);
            Fix([H|T]) -> [Fix(H)|Fix(T)];
            Fix(Value) -> Value
        end,
        {{Name,Arity},[Rewrite(I) || I <- Instructions]}
    end,[{walk,2},{pack,1}])
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
