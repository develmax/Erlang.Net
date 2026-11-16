param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'tuple_branch_body_probe.erl'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
$outputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
[IO.File]::WriteAllBytes($outputPath, $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = tuple_branch_body_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    {module,M} = code:load_binary(M,Source,Beam),
    {finished, tail, {made,{make,item}}} = M:run([{make,item}|tail]),
    {finished, [], skip} = M:run([{skip,a,b}]),
    {rest, [{wrong,item}]} = M:run([{wrong,item}]),
    {finished, tail, 42} = M:frame([{value,42}|tail]),
    {unready, waiting} = M:frame([{value,waiting}]),
    {keep, tail, {made,left}, {made,right}} = M:frame([{combine,left,right,keep}|tail]),
    {rest, [{combine,left,right}]} = M:frame([{combine,left,right}]),
    Small = M:capture([{small,item}|tail]),
    {small, tail, {made,item}} = Small(),
    Large = M:capture([{large,item,keep}|tail]),
    {large, tail, keep, {made,item}} = Large(),
    true = code:delete(M),
    code:purge(M),
    [Frame] = [Is || {function,frame,1,_,Is} <- Fs],
    [{test,is_nonempty_list,_,_},{get_list,_,_,_},{test,is_tuple,_,_},{select_tuple_arity,_,_,_}|_] =
        lists:nthtail(4,Frame),
    Sizes = lists:usort([Size || {allocate,Size,_} <- Frame]),
    [_,_|_] = Sizes,
    [Capture] = [Is || {function,capture,1,_,Is} <- Fs],
    [{test,is_nonempty_list,_,_},{get_list,_,_,_},{test,is_tuple,_,_},{select_tuple_arity,_,_,_}|_] =
        lists:nthtail(4,Capture),
    {Sizes,lists:map(fun({Name,Arity}) ->
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
    end,[{run,1}])}
end,
{OtpFrames,Expected} = Check(Reference),
{GeneratedFrames,Actual} = Check(Generated),
io:format("OTP branch frames ~p, C# branch frames ~p~n",[OtpFrames,GeneratedFrames]),
case Actual of
    Expected -> io:format("~p~n",[Actual]), halt();
    _ -> io:format("OTP ~p~nC# ~p~n",[Expected,Actual]), halt(1)
end.
"@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $outputPath
exit $LASTEXITCODE
