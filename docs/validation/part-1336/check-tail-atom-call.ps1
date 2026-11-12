param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'tail_atom_call_probe.erl'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
$outputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
[IO.File]::WriteAllBytes($outputPath, $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = tail_atom_call_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    {module,M} = code:load_binary(M,Source,Beam),
    {a,b} = M:dispatch(M,a,b),
    {fallback,erlang,a,b} = M:dispatch(erlang,a,b),
    {echo,value} = M:choice(yes,M,value),
    {fallback,M,value,none} = M:choice(no,M,value),
    expected = try M:choice(other,M,value) of _ -> unexpected
        catch error:{case_clause,other} -> expected
    end,
    true = code:delete(M),
    code:purge(M),
    [Instructions] = [Is || {function,dispatch,3,_,Is} <- Fs],
    [{apply_last,2,3}] = [I || I={apply_last,_,_} <- Instructions],
    [{call_last,3,{M,fallback,3},3}] = [I || I={call_last,_,_,_} <- Instructions],
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
Expected = Check(Generated),
io:format("~p~n",[Expected]),
halt().
"@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $outputPath
exit $LASTEXITCODE
