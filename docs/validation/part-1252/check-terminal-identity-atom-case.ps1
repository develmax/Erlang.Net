param(
    [Parameter(Mandatory)] [string] $OtpRoot,
    [Parameter(Mandatory)] [string] $CorpusToolDirectory,
    [Parameter(Mandatory)] [string] $GeneratedBeam
)

$ErrorActionPreference = 'Stop'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $CorpusToolDirectory).Path 'Erlang.Compiler.dll'))
$source = Join-Path $PSScriptRoot 'terminal_identity_atom_case_probe.erl'
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
[IO.File]::WriteAllBytes($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($GeneratedBeam), $bytes)

$check = @'
[Source, Generated] = init:get_plain_arguments(),
{ok,terminal_identity_atom_case_probe,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Compiled} = file:read_file(Generated),
M = terminal_identity_atom_case_probe,
{beam_file,M,_,_,_,Functions} = beam_disasm:file(Compiled),
[Run] = [Instructions || {function,run,1,_,Instructions} <- Functions],
true = lists:any(fun({call_last,2,{M0,pick,2},1}) when M0 =:= M -> true; (_) -> false end, Run),
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    local = M:run(#{route => local}),
    {error,reason} = M:run(#{route => remote,value => {error,reason}}),
    remote_result = M:run(#{route => remote,value => remote_result}),
    bound = M:run_bound(#{route => remote,value => bound},bound),
    {case_clause,other} = try M:run_bound(#{route => remote,value => other},bound)
                          catch error:Reason -> Reason end,
    true = code:delete(M),
    code:purge(M)
end,
Check(Reference),
Check(Compiled),
io:format("Identity case uses a local tail call. The already bound fallback still rejects a different value.~n"),
halt().
'@

$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $GeneratedBeam
exit $LASTEXITCODE
