param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$source = Join-Path $PSScriptRoot 'shared_list_head_probe.erl'
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
[IO.File]::WriteAllBytes($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output), $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = shared_list_head_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    [Is] = [I || {function,run,2,_,I} <- Fs],
    [] = [I || I <- Is, is_tuple(I), element(1,I)=:=allocate],
    {module,M} = code:load_binary(M,Source,Beam),
    [{test,is_nonempty_list,_,_}] = [I || {test,is_nonempty_list,_,_}=I <- Is],
    [{get_list,_,_,_}] = [I || {get_list,_,_,_}=I <- Is],
    {tag,[a],marker} = M:run([tag,a],marker),
    {other,x,tail,marker} = M:run([x|tail],marker),
    [] = M:run([],marker),
    atom = M:run(atom,marker),
    {tuple,[a],v,marker} = M:run_tuple([{tag,v},a],marker),
    {other,{wrong,v},tail,marker} = M:run_tuple([{wrong,v}|tail],marker),
    [] = M:run_tuple([],marker),
    [TupleInstructions] = [I || {function,run_tuple,2,_,I} <- Fs],
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





