param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$source = Join-Path $PSScriptRoot 'unicode_pair_probe.erl'
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
[IO.File]::WriteAllBytes($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output), $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = unicode_pair_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
{beam_file,M,_,_,_,Fs} = beam_disasm:file(Generated),
[Is] = [I || {function,run,2,_,I} <- Fs],
true = lists:member({allocate,2,2}, Is),
true = lists:member({trim,2,0}, Is),
true = lists:member({call_ext_last,1,{extfunc,M,done,1},0}, Is),
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    marker = M:run([233], "b"),
    receive {<<195,169,0>>,<<98,0>>,marker} -> ok after 0 -> error(wrong_arguments) end,
    function_clause = try M:run(17, []) catch error:Reason -> Reason end,
    true = code:delete(M),
    code:purge(M)
end,
Check(Reference),
Check(Generated),
io:format("Two Unicode conversions reuse the input slots and preserve dynamic call arguments.~n"),
halt().
"@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $Output
exit $LASTEXITCODE






















