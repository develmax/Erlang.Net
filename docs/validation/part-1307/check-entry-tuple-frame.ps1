param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$source = Join-Path $PSScriptRoot 'entry_tuple_frame_probe.erl'
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
[IO.File]::WriteAllBytes($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output), $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = entry_tuple_frame_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    [Is] = [I || {function,run,2,_,I} <- Fs],
    Ops = [case I of T when is_tuple(T) -> element(1,T); A -> A end || I <- Is],
    Before = lists:takewhile(fun(Op) -> Op =/= allocate end, Ops),
    true = length([test || test <- Before]) >= 2,
    true = lists:member(get_tuple_element,Before),
    {module,M} = code:load_binary(M,Source,Beam),
    {a,[2,1]} = M:run({a,b},[1,2]),
    {17,tag} = M:run({17,tag},[1,2]),
    {tag,a,b} = M:run({tag,a,b},[1,2]),
    {} = M:run({},[1,2]),
    atom = M:run(atom,[1,2]),
    true = code:delete(M),
    code:purge(M),
    io:format("~p~n", [Is]), Is
end,
ReferenceInstructions = Check(Reference),
ReferenceInstructions = Check(Generated),
halt().
"@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $Output
exit $LASTEXITCODE
