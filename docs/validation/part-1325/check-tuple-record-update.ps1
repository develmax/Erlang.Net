param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'tuple_record_update_probe.erl'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
$outputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
[IO.File]::WriteAllBytes($outputPath, $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = tuple_record_update_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    {module,M} = code:load_binary(M,Source,Beam),
    erase(M),
    {key,{state,new,kept}} = M:run(tag,{ignored,new}),
    put(M,{state,old,retained}),
    {key,{state,new,retained}} = M:run(tag,{ignored,new}),
    {key,{state,new,retained}} = M:run_bound(tag,{ignored,new}),
    {state,old,retained} = get(M),
    no = M:run(tag,invalid),
    erase(M),
    true = code:delete(M),
    code:purge(M),
    [{Name,Is} || {function,Name,2,_,Is} <- Fs]
end,
Normalize = fun(Instructions) ->
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
ReferenceFunctions = Check(Reference),
GeneratedFunctions = Check(Generated),
lists:foreach(fun(Name) ->
    Instructions = Normalize(proplists:get_value(Name,ReferenceFunctions)),
    Instructions = Normalize(proplists:get_value(Name,GeneratedFunctions)),
    io:format("~p~n",[Instructions])
end,[run,run_bound]),
halt().
"@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $outputPath
exit $LASTEXITCODE
