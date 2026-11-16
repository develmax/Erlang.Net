param(
    [Parameter(Mandatory)] [string] $OtpRoot,
    [Parameter(Mandatory)] [string] $CorpusToolDirectory,
    [Parameter(Mandatory)] [string] $GeneratedBeam
)

$ErrorActionPreference = 'Stop'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $CorpusToolDirectory).Path 'Erlang.Compiler.dll'))
$source = Join-Path $PSScriptRoot 'split_list_frame_probe.erl'
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
[IO.File]::WriteAllBytes($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($GeneratedBeam), $bytes)

$check = @'
[Source, Generated] = init:get_plain_arguments(),
M = split_list_frame_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Compiled} = file:read_file(Generated),
{beam_file,M,_,_,_,Functions} = beam_disasm:file(Compiled),
[Read] = [Is || {function,read,2,_,Is} <- Functions],
[Walk] = [Is || {function,walk,2,_,Is} <- Functions],
[1] = [Slots || {allocate,Slots,_} <- Read],
[] = [Slots || {allocate,Slots,_} <- Walk],
true = lists:any(fun({call_only,2,{_,walk,2}}) -> true; (_) -> false end, Walk),
true = lists:any(fun({test,is_nonempty_list,_,_}) -> true; (_) -> false end, Read),
true = lists:any(fun({test,is_map,_,_}) -> true; (_) -> false end, Read),
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    {answer,[tail]} = M:read([#{key => answer},tail],key),
    empty = M:read([],ignored),
    invalid = M:read([not_a_map],ignored),
    invalid = M:read(not_a_list,ignored),
    last = M:walk([first,last],initial),
    initial = M:walk([],initial),
    invalid = M:walk([first,42],initial),
    {badkey,missing} = try M:read([#{key => answer}],missing)
                       catch error:Reason -> Reason end,
    true = code:delete(M),
    code:purge(M)
end,
Check(Reference),
Check(Compiled),
io:format("Guarded list clauses use one saved slot when needed and tail-call without a frame. Results and errors agree with OTP.~n"),
halt().
'@

$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $GeneratedBeam
exit $LASTEXITCODE
