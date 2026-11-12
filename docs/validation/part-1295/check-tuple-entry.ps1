param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$source = Join-Path $PSScriptRoot 'tuple_entry_probe.erl'
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
[IO.File]::WriteAllBytes($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output), $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = tuple_entry_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
{beam_file,M,_,_,_,Fs} = beam_disasm:file(Generated),
[Is] = [I || {function,run,1,_,I} <- Fs],
BeforeFrame = lists:takewhile(fun({allocate,_,_}) -> false; (_) -> true end, Is),
true = lists:any(fun({test,is_tuple,_,_}) -> true; (_) -> false end, BeforeFrame),
true = lists:any(fun({test,is_eq_exact,_,_}) -> true; (_) -> false end, BeforeFrame),
true = lists:any(fun({allocate,1,_}) -> true; (_) -> false end, Is),
true = lists:member({get_tuple_element,{x,0},0,{x,0}}, Is),
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    {{wrapped,first},second} = M:run({first,1.5,second}),
    function_clause = try M:run({first,wrong,second}) catch error:Reason -> Reason end,
    function_clause = try M:run(not_tuple) catch error:Other -> Other end,
    true = code:delete(M),
    code:purge(M)
end,
Check(Reference),
Check(Generated),
io:format("The first call reads the tuple field in X0 and the frame retains only one slot.~n"),
halt().
"@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $Output
exit $LASTEXITCODE
















