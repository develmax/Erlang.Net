param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$source = Join-Path $PSScriptRoot 'compound_guard_probe.erl'
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
[IO.File]::WriteAllBytes($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output), $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = compound_guard_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
{beam_file,M,_,_,_,Fs} = beam_disasm:file(Generated),
[Is] = [I || {function,run,4,_,I} <- Fs],
Entry = lists:takewhile(fun({allocate,_,_}) -> false; (_) -> true end, Is),
true = lists:any(fun({bif,'or',_,_,_}) -> true; (_) -> false end, Entry),
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    ok = M:run(1.0,{a,b},bad,bad),
    ok = M:run(not_float,{a,b,c},{a,b},bad),
    ok = M:run(not_float,bad,{a,b,c},{a,b}),
    function_clause = try M:run(not_float,bad,bad,bad) catch error:Reason -> Reason end,
    [{1.0,{a,b},bad,bad},{not_float,{a,b,c},{a,b},bad},{not_float,bad,{a,b,c},{a,b}}] =
        [receive Message -> Message after 0 -> missing end || _ <- [1,2,3]],
    true = code:delete(M),
    code:purge(M)
end,
Check(Reference),
Check(Generated),
io:format("Each guard alternative can succeed independently and tuple_size failures fall through.~n"),
halt().
"@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $Output
exit $LASTEXITCODE






















