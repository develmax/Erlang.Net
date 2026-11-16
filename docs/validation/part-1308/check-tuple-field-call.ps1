param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$source = Join-Path $PSScriptRoot 'tuple_field_call_probe.erl'
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
[IO.File]::WriteAllBytes($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output), $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = tuple_field_call_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    [Is] = [I || {function,run,1,_,I} <- Fs],
    Ops = [case I of T when is_tuple(T) -> element(1,T); A -> A end || I <- Is],
    [func_info,test,test,allocate,get_tuple_element,get_tuple_element|_] =
        [Op || Op<-Ops, Op=/=label, Op=/=line],
    {module,M} = code:load_binary(M,Source,Beam),
    {2,kept} = M:run({3,kept}),
    {tag,kept} = M:run({tag,kept}),
    {a,b,c} = M:run({a,b,c}),
    atom = M:run(atom),

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

