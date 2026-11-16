param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$source = Join-Path $PSScriptRoot 'list_argument_dispatch_probe.erl'
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
[IO.File]::WriteAllBytes($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output), $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = list_argument_dispatch_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    [Is] = [I || {function,run,2,_,I} <- Fs],
    [] = [I || I <- Is, is_tuple(I), element(1,I)=:=allocate],
    {module,M} = code:load_binary(M,Source,Beam),
    {items,[a,tag]} = M:run([a,b],tag),
    {items,[a,tag]} = M:run([a|tail],tag),
    {empty,tag} = M:run([],tag),
    atom = M:run(atom,tag),
    {items,[a,b]} = M:run_tuple({tag,a,b}),
    {other,a,b} = M:run_tuple({other,a,b}),
    {tag,a} = M:run_tuple({tag,a}),
    [TupleInstructions] = [I || {function,run_tuple,1,_,I} <- Fs],

    true = code:delete(M),
    code:purge(M),
    io:format("~p~n~p~n", [Is,TupleInstructions]), {Is,TupleInstructions}
end,
ReferenceInstructions = Check(Reference),
ReferenceInstructions = Check(Generated),
halt().
"@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $Output
exit $LASTEXITCODE



