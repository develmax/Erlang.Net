param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$source = Join-Path $PSScriptRoot 'list_tail_dispatch_probe.erl'
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
[IO.File]::WriteAllBytes($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output), $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = list_tail_dispatch_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    [Is] = [I || {function,run,2,_,I} <- Fs],
    [{test,is_nonempty_list,_,_}] = [I || {test,is_nonempty_list,_,_}=I <- Is],
    [] = [I || I <- Is, is_tuple(I), element(1,I)=:=allocate],
    {module,M} = code:load_binary(M,Source,Beam),
    {tag,a,[b]} = M:run([a,b],tag),
    {tag,a,tail} = M:run([a|tail],tag),
    {empty,tag} = M:run([],tag),
    atom = M:run(atom,tag),
    true = M:proper([a,b]),
    true = M:proper([]),
    false = M:proper([a|tail]),
    false = M:proper(atom),
    [Proper] = [I || {function,proper,1,_,I} <- Fs],
    [{bif,'=:=',{f,0},[{x,0},nil],{x,0}}] = [I || {bif,'=:=',_,_,_}=I <- Proper],

    true = code:delete(M),
    code:purge(M),
    io:format("~p~n~p~n", [Is,Proper]), {Is,Proper}
end,
ReferenceInstructions = Check(Reference),
ReferenceInstructions = Check(Generated),
halt().
"@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $Output
exit $LASTEXITCODE


