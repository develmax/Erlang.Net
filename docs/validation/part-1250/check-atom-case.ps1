param(
    [Parameter(Mandatory)] [string] $OtpRoot,
    [Parameter(Mandatory)] [string] $CorpusToolDirectory,
    [Parameter(Mandatory)] [string] $GeneratedBeam
)

$ErrorActionPreference = 'Stop'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $CorpusToolDirectory).Path 'Erlang.Compiler.dll'))
$source = Join-Path $PSScriptRoot 'atom_case_probe.erl'
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
[IO.File]::WriteAllBytes($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($GeneratedBeam), $bytes)

$check = @'
[Source, Generated] = init:get_plain_arguments(),
{ok, atom_case_probe, ReferenceBeam} = compile:file(Source, [binary, deterministic]),
{ok, GeneratedBeam} = file:read_file(Generated),
{beam_file, atom_case_probe, _, _, _, Fs} = beam_disasm:file(GeneratedBeam),
[Run] = [Is || {function,run,2,_,Is} <- Fs],
true = lists:member({allocate,1,2}, Run),
true = lists:any(fun({select_val,{x,0},_,_}) -> true; (_) -> false end, Run),
true = lists:any(fun({case_end,{x,0}}) -> true; (_) -> false end, Run),
Check = fun(Beam) ->
    {module, atom_case_probe} = code:load_binary(atom_case_probe, Source, Beam),
    {ok,{kept,42}} = atom_case_probe:run(#{status => ready}, {kept,42}),
    pending = atom_case_probe:run(#{status => waiting}, ignored),
    Payload = {other,42},
    {case_clause,Payload} = try atom_case_probe:run(#{status => Payload}, ignored)
                            catch error:Reason -> Reason end,
    true = code:delete(atom_case_probe),
    code:purge(atom_case_probe)
end,
Check(ReferenceBeam),
Check(GeneratedBeam),
io:format("One Y slot preserves the live argument. Both branches and the unmatched edge agree with OTP.~n"),
halt().
'@

$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $GeneratedBeam
exit $LASTEXITCODE
