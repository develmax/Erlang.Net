param(
    [Parameter(Mandatory)] [string] $OtpRoot,
    [Parameter(Mandatory)] [string] $CorpusToolDirectory,
    [Parameter(Mandatory)] [string] $GeneratedBeam
)

$ErrorActionPreference = 'Stop'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $CorpusToolDirectory).Path 'Erlang.Compiler.dll'))
$source = Join-Path $PSScriptRoot 'terminal_atom_tail_probe.erl'
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
[IO.File]::WriteAllBytes($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($GeneratedBeam), $bytes)

$check = @'
[Source, Generated] = init:get_plain_arguments(),
M = terminal_atom_tail_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Compiled} = file:read_file(Generated),
{beam_file,M,_,_,_,Functions} = beam_disasm:file(Compiled),
[Route] = [Is || {function,route,3,_,Is} <- Functions],
[Lookup] = [Is || {function,lookup,3,_,Is} <- Functions],
[remote] = [Name || {call_last,2,{_,Name,2},2} <- Route],
[inet_result,socket_result] = lists:sort([Name || {call_last,2,{_,Name,2},2} <- Lookup]),
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    host = M:route(#{route => local},host,fallback),
    ignored = M:route(#{route => ignored},host,fallback),
    {remote,host,fallback} = M:route(#{route => remote},host,fallback),
    {inet,80,tcp} = M:lookup(#{backend => inet},80,tcp),
    {socket,53,udp} = M:lookup(#{backend => socket},53,udp),
    {case_clause,other} = try M:lookup(#{backend => other},80,tcp)
                          catch error:Reason -> Reason end,
    true = code:delete(M),
    code:purge(M)
end,
Check(Reference),
Check(Compiled),
io:format("Three local tail calls, argument order, direct returns and unmatched selector agree with OTP behavior.~n"),
halt().
'@

$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $GeneratedBeam
exit $LASTEXITCODE
