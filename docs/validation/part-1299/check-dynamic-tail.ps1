param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$source = Join-Path $PSScriptRoot 'dynamic_module_probe.erl'
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
[IO.File]::WriteAllBytes($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output), $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = dynamic_module_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
{beam_file,M,_,_,_,Fs} = beam_disasm:file(Generated),
[Is] = [I || {function,run,1,_,I} <- Fs],
true = lists:member({allocate,1,1}, Is),
true = lists:member({move,{x,0},{x,1}}, Is),
true = lists:member({apply,1}, Is),
true = lists:member({trim,1,0}, Is),
true = lists:member({call_ext_last,1,{extfunc,dynamic_module_probe,done,1},0}, Is),
[{label,Info}|_] = Is,
false = lists:member({jump,{f,Info}}, Is),
true = lists:any(fun({test,is_atom,{f,Label},_}) -> Label =:= Info; (_) -> false end, Is),
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    ok = M:run(first),
    function_clause = try M:run(17) catch error:Reason -> Reason end,
    M = M:retained(second),
    [{ran,first},{ran,second}] = [receive Message -> Message after 0 -> missing end || _ <- [1,2]],
    true = code:delete(M),
    code:purge(M)
end,
Check(Reference),
Check(Generated),
io:format("The discarded apply trims the frame before a constant-argument tail call.~n"),
halt().
"@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $Output
exit $LASTEXITCODE




















