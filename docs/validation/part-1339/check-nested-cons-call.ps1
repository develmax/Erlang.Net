param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'nested_cons_call_probe.erl'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
$outputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
[IO.File]::WriteAllBytes($outputPath, $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = nested_cons_call_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    {module,M} = code:load_binary(M,Source,Beam),
    [{exact,4},{exact,6},plain] =
        M:rewrite([{limit,4},{exact,4},{limit,6},{minimum,4,2},plain]),
    [{fallback,4},{exact,4.0}] = M:rewrite([{limit,4},{exact,4.0}]),
    [{fallback,5},{minimum,4,2}] = M:rewrite([{limit,5},{minimum,4,2}]),
    [{fallback,4},{minimum,2,0}] = M:rewrite([{limit,4},{minimum,2,0}]),
    [{fallback,4},{minimum,2,wrong}] = M:rewrite([{limit,4},{minimum,2,wrong}]),
    [{fallback,1},{minimum,2,1}] = M:rewrite([{limit,1},{minimum,2,1}]),
    [{fallback,4},plain] = M:rewrite([{limit,4},plain]),
    [{fallback,4}] = M:rewrite([{limit,4}]),
    [{same,1},{pair,1,1.0}|improper] = M:same([{pair,1,1},{pair,1,1.0}|improper]),
    [value,marker,{pair,other,value}|improper] =
        M:literal([marker,{pair,fixed,value},marker,{pair,other,value}|improper]),
    [{ok,a},next] = M:coupled({key,a},[{pair,a},next],[unused]),
    [{pair,b},next] = M:coupled({key,a},[{pair,b},next],[unused]),
    expected = try M:rewrite([a|bad]) of _ -> unexpected
        catch error:function_clause -> expected
    end,
    true = code:delete(M),
    code:purge(M),
    [Instructions] = [Is || {function,rewrite,1,_,Is} <- Fs],
    [1,1,1,1] = [Slots || {allocate,Slots,_} <- Instructions],
    [{gc_bif,'rem',_,_,_,_}] = [I || I={gc_bif,'rem',_,_,_,_} <- Instructions],
    Labels = [L || {label,L} <- Instructions],
    Map = maps:from_list(lists:zip(Labels,lists:seq(1,length(Labels)))),
    Rewrite = fun Fix({label,L}) -> {label,maps:get(L,Map,L)};
        Fix({f,L}) -> {f,maps:get(L,Map,L)};
        Fix({literal,_}=Literal) -> Literal;
        Fix(Tuple) when is_tuple(Tuple) -> list_to_tuple([Fix(X) || X <- tuple_to_list(Tuple)]);
        Fix([H|T]) -> [Fix(H)|Fix(T)];
        Fix(Value) -> Value
    end,
    [Rewrite(I) || I <- Instructions]
end,
Expected = Check(Reference),
Actual = Check(Generated),
io:format("OTP ~p~nC# ~p~n",[Expected,Actual]),
halt().
"@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $outputPath
exit $LASTEXITCODE



