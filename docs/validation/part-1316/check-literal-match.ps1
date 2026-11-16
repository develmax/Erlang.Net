param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$source = Join-Path $PSScriptRoot 'literal_match_probe.erl'
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
[IO.File]::WriteAllBytes($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output), $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = literal_match_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    [Is] = [I || {function,run,0,_,I} <- Fs],
    [Ds] = [I || {function,discarded,0,_,I} <- Fs],
    [] = [I || I <- Is ++ Ds, is_tuple(I), element(1,I)=:=badmatch],
    {module,M} = code:load_binary(M,Source,Beam),
    {sentinel,7} = M:run(),
    kept = M:discarded(),
    preserved = try M:mismatch() of _ -> unexpected catch error:{badmatch,8} -> preserved end,
    true = code:delete(M),
    code:purge(M),
    io:format("~p~n~p~n", [Is,Ds]), {Is,Ds}
end,
ReferenceInstructions = Check(Reference),
ReferenceInstructions = Check(Generated),
halt().
"@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $Output
exit $LASTEXITCODE





