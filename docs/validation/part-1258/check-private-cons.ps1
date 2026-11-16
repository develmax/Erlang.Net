param(
    [Parameter(Mandatory)] [string] $OtpRoot,
    [Parameter(Mandatory)] [string] $CorpusToolDirectory,
    [Parameter(Mandatory)] [string] $GeneratedBeam
)

$ErrorActionPreference = 'Stop'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $CorpusToolDirectory).Path 'Erlang.Compiler.dll'))
$source = Join-Path $PSScriptRoot 'private_cons_probe.erl'
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
[IO.File]::WriteAllBytes($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($GeneratedBeam), $bytes)

$check = @'
[Source, Generated] = init:get_plain_arguments(),
M = private_cons_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Compiled} = file:read_file(Generated),
{beam_file,M,_,_,_,Fs} = beam_disasm:file(Compiled),
ShapeTests = fun(Name) ->
    [Instructions] = [Is || {function,N,2,_,Is} <- Fs,N =:= Name],
    length([1 || {test,is_nonempty_list,_,_} <- Instructions])
end,
0 = ShapeTests(known),
1 = ShapeTests(escaped),
1 = ShapeTests(unknown),
1 = ShapeTests(public),
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    {atom,tail} = M:run(atom),
    {fallback,[42|tail]} = M:run(42),
    {fallback,[]} = M:run_unknown([]),
    {seed,[]} = M:seed(),
    Capture = M:capture(),
    {fallback,[]} = Capture([],fallback),
    {fallback,not_a_list} = M:public(not_a_list,fallback),
    true = code:delete(M),
    code:purge(M)
end,
Check(Reference),
Check(Compiled),
io:format("Only the proven private entry drops its cons test. Captured and exported calls preserve their fallbacks.~n"),
halt().
'@

$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $GeneratedBeam
exit $LASTEXITCODE
