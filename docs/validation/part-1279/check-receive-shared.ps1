param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$source = Join-Path $PSScriptRoot 'receive_shared_probe.erl'
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
[IO.File]::WriteAllBytes($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output), $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = receive_shared_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
{beam_file,M,_,_,_,Fs} = beam_disasm:file(Generated),
[Is] = [I || {function,run,1,_,I} <- Fs],
1 = length([I || I = {get_tuple_element,{x,0},2,{x,0}} <- Is]),
true = lists:member({test_heap,4,3}, Is),
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    self() ! {ignored,wrong,value},
    self() ! {item,expected,one},
    {matched,expected,one} = try M:run(expected) catch error:Reason -> Reason end,
    self() ! {item,other,two},
    {wrapped,{marker,[two,other]}} = M:run(expected),
    self() ! {done,three},
    three = M:run(expected),
    timeout = M:run(expected),
    receive {ignored,wrong,value} -> ok after 0 -> error(lost_message) end,
    true = code:delete(M),
    code:purge(M)
end,
Check(Reference),
Check(Generated),
io:format("Shared receive fields preserve equality fallthrough and leave unrelated messages queued.~n"),
halt().
"@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $Output
exit $LASTEXITCODE






