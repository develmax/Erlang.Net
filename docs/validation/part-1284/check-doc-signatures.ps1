param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$source = Join-Path $PSScriptRoot 'quoted_doc_probe.erl'
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
[IO.File]::WriteAllBytes($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output), $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = quoted_doc_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
Signatures = fun(Beam) ->
    {ok,M,Chunks} = beam_lib:all_chunks(Beam),
    {docs_v1,_,_,_,_,_,Entries} = binary_to_term(proplists:get_value("Docs",Chunks)),
    maps:from_list([{Name,Signature} || {{function,Name,_},_,[Signature],_,_} <- Entries])
end,
Expected = Signatures(Reference),
Expected = Signatures(Generated),
<<"'begin'(Mode)">> = maps:get('begin',Expected),
<<"'end'()">> = maps:get('end',Expected),
io:format("Spec-derived Docs signatures agree with OTP for reserved, quoted and plain names.~n"),
halt().
"@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $Output
exit $LASTEXITCODE












