param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$source = Join-Path $PSScriptRoot 'dynamic_module_probe.erl'
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
[IO.File]::WriteAllBytes($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output), $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = dynamic_module_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
{beam_file,M,_,_,_,Fs} = beam_disasm:file(Generated),
[Is] = [I || {function,run,1,_,I} <- Fs],
true = lists:member({allocate,1,1}, Is),
true = lists:member({move,{x,0},{x,1}}, Is),
true = lists:member({apply,1}, Is),
true = lists:member({trim,1,0}, Is),
true = lists:member({deallocate,0}, Is),
[{label,Info}|_] = Is,
false = lists:member({jump,{f,Info}}, Is),
true = lists:any(fun({select_val,{x,0},{f,Label},{list,[{integer,0},{f,Good},{integer,1},{f,Good}]}}) ->
    Label =:= Info; (_) -> false end, Is),
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    ok = M:run(0),
    ok = M:run(1),
    function_clause = try M:run(0.0) catch error:Reason -> Reason end,
    M = M:retained(second),
    [{ran,0},{ran,1},{ran,second}] = [receive Message -> Message after 0 -> missing end || _ <- [1,2,3]],
    true = code:delete(M),
    code:purge(M)
end,
Check(Reference),
Check(Generated),
io:format("The integer alternatives dispatch before allocation and reject numerically equal floats.~n"),
halt().
"@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $Output
exit $LASTEXITCODE




















