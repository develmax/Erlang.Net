param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$source = Join-Path $PSScriptRoot 'call_line_probe.erl'
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
[IO.File]::WriteAllBytes($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output), $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = call_line_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
{beam_file,M,_,_,_,Fs} = beam_disasm:file(Generated),
[Is] = [I || {function,run,0,_,I} <- Fs],
1 = length([ok || {line,[{location,_,4}]} <- Is]),
[Separate] = [I || {function,separate,0,_,I} <- Fs],
[8,7] = [L || {line,[{location,_,L}]} <- Separate, L >= 7],
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    4 = try M:run() catch error:ok:Stack ->
        [Caller] = [Location || {Module,run,0,Location} <- Stack, Module =:= M],
        proplists:get_value(line,Caller)
    end,
    7 = try M:separate() catch error:ok:SeparateStack ->
        [SeparateCaller] = [Location || {Module,separate,0,Location} <- SeparateStack, Module =:= M],
        proplists:get_value(line,SeparateCaller)
    end,
    true = code:delete(M),
    code:purge(M)
end,
Check(Reference),
Check(Generated),
io:format("Direct nested calls share a line only when their coordinates match.~n"),
halt().
"@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $Output
exit $LASTEXITCODE








