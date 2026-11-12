param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'integer_guard_range_probe.erl'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
$outputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
[IO.File]::WriteAllBytes($outputPath, $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = integer_guard_range_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    {module,M} = code:load_binary(M,Source,Beam),
    {wrapped,a} = M:low(a,{tag,1}),
    no = M:high(a,{tag,1}),
    {wrapped,a} = M:high(a,{tag,2}),
    {wrapped,a} = M:exported(a,{tag,300},300),
    Callback = M:indirect(),
    {wrapped,a} = Callback(a,{tag,300},300),
    true = code:delete(M),
    code:purge(M),
    maps:from_list([{Name, Is} || {function,Name,3,_,Is} <- Fs])
end,
Ref = Check(Reference),
Actual = Check(Generated),
Instructions = maps:get(route,Ref),
Instructions = maps:get(route,Actual),
[{test,is_ge,_,[_,{tr,{x,2},{t_integer,{1,2}}}]}] =
    [I || I={test,is_ge,_,_} <- Instructions],
lists:foreach(fun(Name) ->
    [{test,is_ge,_,[_,{x,2}]}] = [I || I={test,is_ge,_,_} <- maps:get(Name,Actual)]
end,[exported,indirect_route]),
io:format("~p~n",[Instructions]),
halt().
"@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $outputPath
exit $LASTEXITCODE



