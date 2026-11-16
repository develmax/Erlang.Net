param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'tuple_entry_tail_probe.erl'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
$outputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
[IO.File]::WriteAllBytes($outputPath, $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = tuple_entry_tail_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    {module,M} = code:load_binary(M,Source,Beam),
    {{wrapped,a},b} = M:route({tag,a},b),
    no = M:route({other,a},b),
    no = M:route(tag,b),
    {{wrapped,a,b},a} = M:preserve({tag,a},b),
    {{wrapped,a},b} = M:guarded({tag,a},b),
    no = M:guarded({tag,1},b),
    true = code:delete(M),
    code:purge(M),
    [Instructions] = [Is || {function,route,2,_,Is} <- Fs],
    Instructions
end,
Instructions = Check(Reference),
Instructions = Check(Generated),
io:format("~p~n",[Instructions]),
halt().
"@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $outputPath
exit $LASTEXITCODE

