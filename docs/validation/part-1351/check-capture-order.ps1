param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'capture_order_probe.erl'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
$outputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
[IO.File]::WriteAllBytes($outputPath, $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = capture_order_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    {module,M} = code:load_binary(M,Source,Beam),
    First = M:walk(left,[{selected,item}|tail],right),
    {left, arg, tail, right, {boxed,item}} = First(arg),
    Second = M:walk(left,[item,remaining],right),
    {left, arg, [remaining], right, {boxed,item}} = Second(arg),
    empty = M:walk(left,[],right),
    {chosen,tail,42} = M:choose([{value,42}|tail]),
    {unready,waiting} = M:choose([{value,waiting}]),
    {chosen,[],skip} = M:choose([{skip,one,two}]),
    {rest,[{other,42}]} = M:choose([{other,42}]),
    true = code:delete(M),
    code:purge(M),
    [Walk] = [Is || {function,walk,3,_,Is} <- Fs],
    [{test,is_nonempty_list,_,[{x,1}]},{get_list,{x,1},_,{x,1}}|_] = lists:nthtail(4,Walk),
    Captured = [{Name,Arity,Is} || {function,Name,Arity,_,Is} <- Fs,
        string:prefix(atom_to_list(Name),"-walk/3-fun-") =/= nomatch],
    [_,_] = Captured,
    [Choose] = [Is || {function,choose,1,_,Is} <- Fs],
    [{allocate,1,_}] = [I || I = {allocate,_,_} <- Choose],
    lists:map(fun({Name,Arity,Instructions}) ->
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
    end,lists:sort([{choose,1,Choose}|Captured]))
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
