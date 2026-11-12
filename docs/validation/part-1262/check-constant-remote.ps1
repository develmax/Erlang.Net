param(
    [Parameter(Mandatory)] [string] $OtpRoot,
    [Parameter(Mandatory)] [string] $CorpusToolDirectory,
    [Parameter(Mandatory)] [string] $GeneratedBeam
)

$ErrorActionPreference = 'Stop'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $CorpusToolDirectory).Path 'Erlang.Compiler.dll'))
$source = Join-Path $PSScriptRoot 'constant_remote_probe.erl'
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
[IO.File]::WriteAllBytes($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($GeneratedBeam), $bytes)

$check = @'
[Source, Generated] = init:get_plain_arguments(),
M = constant_remote_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Compiled} = file:read_file(Generated),
{beam_file,M,_,_,_,Fs} = beam_disasm:file(Compiled),
CheckInstructions = fun(Name) ->
    [Instructions] = [Is || {function,N,2,_,Is} <- Fs,N =:= Name],
    [{allocate,0,2}] = [A || A = {allocate,_,_} <- Instructions],
    [{call_ext,2,{extfunc,maps,get,2}}] = [I || I = {call_ext,2,{extfunc,maps,get,2}} <- Instructions],
    false = lists:any(fun({apply,_}) -> true; (_) -> false end, Instructions)
end,
CheckInstructions(check),
CheckInstructions(check_and_finish),
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    ok = M:check(key,#{key => value}),
    {done,42} = M:check_and_finish(key,#{key => value}),
    {badkey,missing} = try M:check(missing,#{}) catch error:Missing -> Missing end,
    function_clause = try M:check(key,invalid) catch error:Guard -> Guard end,
    true = code:delete(M),
    code:purge(M)
end,
Check(Reference),
Check(Compiled),
io:format("Constant module calls use call_ext with zero saved slots. Return, local tail and both errors agree with OTP.~n"),
halt().
'@

$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $GeneratedBeam
exit $LASTEXITCODE
