param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$source = Join-Path $PSScriptRoot 'receive_projection_probe.erl'
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
[IO.File]::WriteAllBytes($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output), $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = receive_projection_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
{beam_file,M,_,_,_,Fs} = beam_disasm:file(Generated),
[Is] = [I || {function,project,0,_,I} <- Fs],
true = lists:any(fun({select_tuple_arity,_,_,_}) -> true; (_) -> false end, Is),
true = lists:member({allocate,0,0}, Is),
false = lists:member({move,{x,0},{x,1}}, Is),
true = lists:any(fun({get_tuple_element,{x,0},_,_}) -> true; (_) -> false end, Is),
false = lists:any(fun({move,{x,_},{y,_}}) -> true; (_) -> false end, Is),
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    self() ! {take,{projected,42}},
    self() ! {ignore,unused,also_unused},
    {projected,42} = M:project(),
    ignored = M:project(),
    timeout = M:project(),
    true = code:delete(M),
    code:purge(M)
end,
Check(Reference),
Check(Generated),
io:format("Terminal receive returns projected values with the message in X0 and a zero-slot frame.~n"),
halt().
"@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $Output
exit $LASTEXITCODE



