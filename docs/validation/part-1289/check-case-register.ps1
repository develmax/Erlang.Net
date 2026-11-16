param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$source = Join-Path $PSScriptRoot 'case_register_probe.erl'
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
[IO.File]::WriteAllBytes($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output), $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = case_register_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
{beam_file,M,_,_,_,Fs} = beam_disasm:file(Generated),
[Is] = [I || {function,run,0,_,I} <- Fs],
true = lists:member({move,{atom,left},{x,1}}, Is),
true = lists:member({move,{atom,right},{x,1}}, Is),
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    put(choice,{tag,1}),
    {{saved,left},marker} = M:run(),
    put(choice,other),
    {{saved,right},marker} = M:run(),
    erase(choice),
    true = code:delete(M),
    code:purge(M)
end,
Check(Reference),
Check(Generated),
io:format("Both constant case results are emitted into the second call argument.~n"),
halt().
"@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $Output
exit $LASTEXITCODE
















