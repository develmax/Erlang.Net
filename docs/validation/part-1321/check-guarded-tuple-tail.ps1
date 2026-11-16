param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'guarded_tuple_tail_probe.erl'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
$outputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
[IO.File]::WriteAllBytes($outputPath, $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = guarded_tuple_tail_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    {module,M} = code:load_binary(M,Source,Beam),
    {wrapped,a} = M:route(a,{tag,3},2),
    no = M:route(a,{tag,1},2),
    no = M:route(a,other,2),
    {wrapped,a,3} = M:preserve(a,{tag,3},2),
    {tag,1} = M:preserve(a,{tag,1},2),
    true = code:delete(M),
    code:purge(M),
    [Instructions] = [Is || {function,route,3,_,Is} <- Fs],
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


