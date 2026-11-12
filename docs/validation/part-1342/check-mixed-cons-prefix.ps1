param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'mixed_cons_prefix_probe.erl'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
$outputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
[IO.File]::WriteAllBytes($outputPath, $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = mixed_cons_prefix_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    {module,M} = code:load_binary(M,Source,Beam),
    [<<"binary">>,["&lt;"],"&gt;","&amp;",42] = M:rewrite([<<"binary">>,[60],62,38,42]),
    [unknown|improper] = M:rewrite([unknown|improper]),
    [42|improper] = M:rewrite([42|improper]),
    [] = M:rewrite([]),
    <<"binary">> = M:rewrite(<<"binary">>),
    [1,mapped|done] = M:nil_marker([1,item]),
    [unknown] = M:nil_marker([unknown]),
    done = M:nil_marker([]),
    [mapped_a,mapped_c,unknown] = M:literal_first([a,c,unknown]),
    [first,mapped] = M:duplicate([a,b]),
    {tag,key} = M:fields(key,[{tag,other},{tag,key}]),
    absent = M:fields(key,[other]),
    [first,value] = M:literal_field([{tag,undefined},{tag,value}]),
    true = code:delete(M),
    code:purge(M),
    [Instructions] = [Is || {function,rewrite,1,_,Is} <- Fs],
    1 = length([I || I={test,is_nonempty_list,_,_} <- Instructions]),
    1 = length([I || I={get_list,_,_,_} <- Instructions]),
    Normalize = fun(Is) ->
        Labels = [L || {label,L} <- Is],
        Map = maps:from_list(lists:zip(Labels,lists:seq(1,length(Labels)))),
        Rewrite = fun Fix({label,L}) -> {label,maps:get(L,Map,L)};
            Fix({f,L}) -> {f,maps:get(L,Map,L)};
            Fix({literal,_}=Literal) -> Literal;
            Fix(Tuple) when is_tuple(Tuple) -> list_to_tuple([Fix(X) || X <- tuple_to_list(Tuple)]);
            Fix([H|T]) -> [Fix(H)|Fix(T)];
            Fix(Value) -> Value
        end,
        [Rewrite(I) || I <- Is]
    end,
    [{N,Normalize(Is)} || {function,N,1,_,Is} <- Fs, lists:member(N,[rewrite,literal_first])]
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




