param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$source = Join-Path $PSScriptRoot 'tuple_guard_probe.erl'
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
[IO.File]::WriteAllBytes($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output), $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = tuple_guard_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
{beam_file,M,_,_,_,Fs} = beam_disasm:file(Generated),
Entry = fun(Name) ->
    [Is] = [I || {function,N,1,_,I} <- Fs, N =:= Name],
    lists:takewhile(fun({allocate,_,_}) -> false; (_) -> true end, Is)
end,
true = lists:any(fun({test,test_arity,_,[{x,0},2]}) -> true; (_) -> false end, Entry(sized)),
true = lists:any(fun({test,is_binary,_,[{x,0}]}) -> true; (_) -> false end, Entry(typed)),
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    ok = M:sized({a,b}),
    ok = M:typed({a}),
    ok = M:typed(<<1>>),
    function_clause = try M:sized({a}) catch error:Reason -> Reason end,
    function_clause = try M:typed(17) catch error:Other -> Other end,
    [{ran,{a,b}},{ran,{a}},{ran,<<1>>}] =
        [receive Message -> Message after 0 -> missing end || _ <- [1,2,3]],
    true = code:delete(M),
    code:purge(M)
end,
Check(Reference),
Check(Generated),
io:format("Tuple arity and tuple-or-binary entry guards agree with OTP.~n"),
halt().
"@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $Output
exit $LASTEXITCODE





















