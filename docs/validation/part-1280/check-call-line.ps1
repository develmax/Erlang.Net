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
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    4 = try M:run() catch error:[{value,ok}]:Stack ->
        [Caller] = [Location || {Module,run,0,Location} <- Stack, Module =:= M],
        proplists:get_value(line,Caller)
    end,
    true = code:delete(M),
    code:purge(M)
end,
Check(Reference),
Check(Generated),
io:format("The nested and outer calls share one line instruction and preserve the stacktrace line.~n"),
halt().
"@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $Output
exit $LASTEXITCODE







