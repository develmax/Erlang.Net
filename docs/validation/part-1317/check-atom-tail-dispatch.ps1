param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$source = Join-Path $PSScriptRoot 'atom_tail_dispatch_probe.erl'
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
[IO.File]::WriteAllBytes($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output), $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = atom_tail_dispatch_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    [Is] = [I || {function,run,3,_,I} <- Fs],
    [] = [I || I <- Is, is_tuple(I), element(1,I)=:=allocate],
    {module,M} = code:load_binary(M,Source,Beam),
    {first,one,two} = M:run(alpha,one,two),
    {second,two,one} = M:run(zulu,one,two),
    {first,one,two} = M:run_second(one,alpha,two),
    {second,two,one} = M:run_second(one,zulu,two),
    missing = try M:run_second(one,unknown,two) of _ -> unexpected catch error:function_clause -> missing end,
    [Second] = [I || {function,run_second,3,_,I} <- Fs],
    missing = try M:run(unknown,one,two) of _ -> unexpected catch error:function_clause -> missing end,
    missing = try M:run({alpha},one,two) of _ -> unexpected catch error:function_clause -> missing end,
    true = code:delete(M),
    code:purge(M),
    io:format("~p~n~p~n", [Is,Second]), {Is,Second}
end,
ReferenceInstructions = Check(Reference),
ReferenceInstructions = Check(Generated),
halt().
"@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $Output
exit $LASTEXITCODE





