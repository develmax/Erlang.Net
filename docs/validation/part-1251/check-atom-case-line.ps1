param(
    [Parameter(Mandatory)] [string] $OtpRoot,
    [Parameter(Mandatory)] [string] $CorpusToolDirectory,
    [Parameter(Mandatory)] [string] $GeneratedBeam
)

$ErrorActionPreference = 'Stop'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $CorpusToolDirectory).Path 'Erlang.Compiler.dll'))
$source = Join-Path $PSScriptRoot 'atom_case_line_probe.erl'
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
[IO.File]::WriteAllBytes($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($GeneratedBeam), $bytes)

$check = @'
[Source, Generated] = init:get_plain_arguments(),
{ok, atom_case_line_probe, ReferenceBeam} = compile:file(Source, [binary, deterministic]),
{ok, GeneratedBeam} = file:read_file(Generated),
Check = fun(Beam) ->
    {beam_file, atom_case_line_probe, _, _, _, Functions} = beam_disasm:file(Beam),
    [Run] = [Is || {function,run,2,_,Is} <- Functions],
    [5] = [N || {{line,[{location,_,N}]},{case_end,{x,0}}}
                  <- lists:zip(lists:droplast(Run),tl(Run))],
    {module, atom_case_line_probe} = code:load_binary(atom_case_line_probe, Source, Beam),
    {ok,[1,2]} = atom_case_line_probe:run(#{status => ready}, [1,2]),
    {waiting,[2,1]} = atom_case_line_probe:run(#{status => waiting}, [1,2]),
    try atom_case_line_probe:run(#{status => missing}, []) of
        Result -> error({expected_case_clause,Result})
    catch
        error:{case_clause,missing}:Stack ->
            [{atom_case_line_probe,run,2,Location}|_] = Stack,
            5 = proplists:get_value(line,Location)
    end,
    true = code:delete(atom_case_line_probe),
    code:purge(atom_case_line_probe)
end,
Check(ReferenceBeam),
Check(GeneratedBeam),
io:format("Reference and generated case failures point to selector line 5 after a branch call on line 7.~n"),
halt().
'@

$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $GeneratedBeam
exit $LASTEXITCODE
