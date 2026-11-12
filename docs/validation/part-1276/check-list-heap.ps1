param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$source = Join-Path $PSScriptRoot 'receive_forward_probe.erl'
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
[IO.File]::WriteAllBytes($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output), $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = receive_forward_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
{beam_file,M,_,_,_,Fs} = beam_disasm:file(Generated),
[Is] = [I || {function,run,0,_,I} <- Fs],
true = lists:member({put_list,{x,0},{literal,[suffix]},{x,0}}, Is),
true = lists:member({test_heap,7,1}, Is),
false = lists:member({test_heap,3,1}, Is),
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    self() ! {run},
    done = M:run(),
    [first,second,[prefix,{message,{wrapped,value}},suffix]] =
        [receive Message -> Message after 0 -> missing end || _ <- [1,2,3]],
    self() ! {retained},
    value = M:retained(),
    [first,second] = [receive Event -> Event after 0 -> missing end || _ <- [1,2]],
    true = code:delete(M),
    code:purge(M)
end,
Check(Reference),
Check(Generated),
io:format("The tagged tuple and two cons cells share one heap check, with call order preserved.~n"),
halt().
"@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $Output
exit $LASTEXITCODE








