param(
    [Parameter(Mandatory)] [string] $OtpRoot,
    [Parameter(Mandatory)] [string] $CorpusToolDirectory,
    [Parameter(Mandatory)] [string] $GeneratedBeam
)

$ErrorActionPreference = 'Stop'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $CorpusToolDirectory).Path 'Erlang.Compiler.dll'))
$source = Join-Path $PSScriptRoot 'guarded_atom_tail_probe.erl'
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
[IO.File]::WriteAllBytes($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($GeneratedBeam), $bytes)

$check = @'
[Source, Generated] = init:get_plain_arguments(),
M = guarded_atom_tail_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Compiled} = file:read_file(Generated),
{beam_file,M,_,_,_,Functions} = beam_disasm:file(Compiled),
[Run] = [Is || {function,run,2,_,Is} <- Functions],
[done,done] = [Name || {call_last,2,{_,Name,2},_} <- Run],
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    {head,value} = M:run([head],#{mode => first,value => value}),
    {head,fallback} = M:run([head],#{mode => second}),
    skipped = M:run([42],ignored),
    skipped = M:run([],ignored),
    {badkey,value} = try M:run([head],#{mode => first})
                     catch error:BindingError -> BindingError end,
    {case_clause,other} = try M:run([head],#{mode => other})
                         catch error:CaseError -> CaseError end,
    true = code:delete(M),
    code:purge(M)
end,
Check(Reference),
Check(Compiled),
io:format("Guarded clause tail calls preserve branch-local binding, guard fallback, and both error reasons.~n"),
halt().
'@

$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $GeneratedBeam
exit $LASTEXITCODE
