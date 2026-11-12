param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$source = Join-Path $PSScriptRoot 'receive_tail_probe.erl'
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
[IO.File]::WriteAllBytes($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output), $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = receive_tail_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
{beam_file,M,_,_,_,Fs} = beam_disasm:file(Generated),
[Is] = [I || {function,read,1,_,I} <- Fs],
true = lists:any(fun({select_tuple_arity,_,_,_}) -> true; (_) -> false end, Is),
true = lists:any(fun({call_last,1,{receive_tail_probe,read,1},_}) -> true; (_) -> false end, Is),
true = lists:any(fun({call_ext_last,1,{extfunc,erlang,error,1},_}) -> true; (_) -> false end, Is),
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    self() ! skipped,
    self() ! {tag,ignored},
    self() ! {retry,1,2,3},
    self() ! {tag,3,4},
    self() ! {tag,2},
    self() ! {tag,1},
    {pair,3,4} = M:read(1),
    {other,2} = M:read(1),
    matched = M:read(1),
    self() ! {fail,6,7,8},
    {failed,6} = try M:read(1) catch error:Reason -> Reason end,
    timeout = M:read(1),
    receive skipped -> ok after 0 -> error(missing_atom) end,
    receive {tag,ignored} -> ok after 0 -> error(missing_tuple) end,
    true = code:delete(M),
    code:purge(M)
end,
Check(Reference),
Check(Generated),
io:format("Tail receive agrees with OTP for direct return, recursion, exception and timeout.~n"),
halt().
"@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $Output
exit $LASTEXITCODE

