param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$source = Join-Path $PSScriptRoot 'repeated_argument_probe.erl'
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
[IO.File]::WriteAllBytes($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output), $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = repeated_argument_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
{beam_file,M,_,_,_,RefFs} = beam_disasm:file(Reference),
{beam_file,M,_,_,_,GenFs} = beam_disasm:file(Generated),
[Is] = [I || {function,classify,2,_,I} <- RefFs],
[Is] = [I || {function,classify,2,_,I} <- GenFs],
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    equal = M:classify({one,2},{one,2}),
    different = M:classify(1,1.0),
    different = M:classify(left,right),
    true = code:delete(M),
    code:purge(M)
end,
Check(Reference),
Check(Generated),
io:format("Instructions match OTP; repeated argument uses exact equality.~n"),
halt().
"@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $Output
exit $LASTEXITCODE
