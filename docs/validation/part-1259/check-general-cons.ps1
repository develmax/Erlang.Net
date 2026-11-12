param(
    [Parameter(Mandatory)] [string] $OtpRoot,
    [Parameter(Mandatory)] [string] $CorpusToolDirectory,
    [Parameter(Mandatory)] [string] $GeneratedBeam
)

$ErrorActionPreference = 'Stop'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $CorpusToolDirectory).Path 'Erlang.Compiler.dll'))
$source = Join-Path $PSScriptRoot 'general_cons_probe.erl'
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
[IO.File]::WriteAllBytes($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($GeneratedBeam), $bytes)

$check = @'
[Source, Generated] = init:get_plain_arguments(),
M = general_cons_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Compiled} = file:read_file(Generated),
{beam_file,M,_,_,_,Fs} = beam_disasm:file(Compiled),
Tests = fun(Name) ->
    [Instructions] = [Is || {function,N,_,_,Is} <- Fs,N =:= Name],
    length([1 || {test,is_nonempty_list,_,_} <- Instructions])
end,
1 = Tests(single),
0 = Tests(both),
[Tail] = [Instructions || {function,tail,1,_,Instructions} <- Fs],
[{get_list,{x,0},{x,1},{x,0}}] = [I || I = {get_list,_,_,_} <- Tail],
true = lists:any(fun({call_only,2,{_,finish,2}}) -> true; (_) -> false end, Tail),
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    {head,second,[extra],1} = M:run_single([second,extra]),
    function_clause = try M:run_single([]) catch error:Single -> Single end,
    {first,found} = M:run_shared(first,#{key => found}),
    {second,found} = M:run_shared(second,#{key => found}),
    function_clause = try M:run_shared(other,#{}) catch error:Shared -> Shared end,
    {improper,head} = M:run_tail(head,improper),
    true = code:delete(M),
    code:purge(M)
end,
Check(Reference),
Check(Compiled),
io:format("General entry preserves deeper checks and clause errors. The proven cons tail path uses call_only.~n"),
halt().
'@

$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $GeneratedBeam
exit $LASTEXITCODE
