param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'registered_process_case_probe.erl'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
$outputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
[IO.File]::WriteAllBytes($outputPath, $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = registered_process_case_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    {module,M} = code:load_binary(M,Source,Beam),
    {missing,value} = M:dispatch(value),
    {render,"registered process received [value]"} = M:remote_dispatch(value),
    true = register(M,self()),
    {render,"registered process received [value]"} = M:dispatch(value),
    {found,value} = M:remote_dispatch(value),
    expected = try M:dispatch(42) of _ -> unexpected
        catch error:badarg -> expected
    end,
    true = unregister(M),
    Pid = spawn(fun() -> receive stop -> ok end end),
    true = register(M,Pid),
    {found,value} = M:dispatch(value),
    true = unregister(M),
    Pid ! stop,
    true = code:delete(M),
    code:purge(M),
    [Instructions] = [Is || {function,dispatch,1,_,Is} <- Fs],
    [{trim,2,0}] = [I || I={trim,_,_} <- Instructions],
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
case Actual of
    Expected -> io:format("~p~n",[Actual]), halt();
    _ -> io:format("OTP ~p~nC# ~p~n",[Expected,Actual]), halt(1)
end.
"@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $outputPath
exit $LASTEXITCODE

