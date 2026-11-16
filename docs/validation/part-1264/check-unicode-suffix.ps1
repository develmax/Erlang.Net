param(
    [Parameter(Mandatory)] [string] $OtpRoot,
    [Parameter(Mandatory)] [string] $CorpusToolDirectory,
    [Parameter(Mandatory)] [string] $GeneratedBeam
)

$ErrorActionPreference = 'Stop'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $CorpusToolDirectory).Path 'Erlang.Compiler.dll'))
$source = Join-Path $PSScriptRoot 'unicode_suffix_probe.erl'
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
[IO.File]::WriteAllBytes($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($GeneratedBeam), $bytes)

$check = @'
[Source, Generated] = init:get_plain_arguments(),
M = unicode_suffix_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Compiled} = file:read_file(Generated),
{beam_file,M,_,_,_,Fs} = beam_disasm:file(Compiled),
[Helper] = [I || {function,N,1,_,I} <- Fs, atom_to_list(N) =:= "-run/1-lc$^0/1-0-"],
true = lists:member({allocate_heap,1,2,1},Helper),
true = lists:member({put_list,{x,0},{literal,[0]},{x,0}},Helper),
[LengthCode] = [I || {function,run_length,1,_,I} <- Fs],
BeforeFrame = lists:takewhile(fun({allocate,_,_}) -> false; (_) -> true end, LengthCode),
true = lists:any(fun({gc_bif,length,{f,0},1,_,{x,1}}) -> true; (_) -> false end, BeforeFrame),
true = lists:member({swap,{x,0},{x,1}},LengthCode),
Stub = [{attribute,1,module,wxe_util},{attribute,1,export,[{queue_cmd,1}]},
        {function,1,queue_cmd,1,[{clause,1,[{var,1,'Value'}],[],[{var,1,'Value'}]}]}],
{ok,wxe_util,StubBeam} = compile:forms(Stub,[binary]),
{module,wxe_util} = code:load_binary(wxe_util,"local test stub",StubBeam),
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    [<<97,0>>,<<195,169,0>>] = M:run(["a",[233]]),
    [] = M:run([]),
    {bad_generator,improper} = try M:run(["a"|improper]) catch error:Reason -> Reason end,
    ok = M:run_length([first,last]),
    badarg = try M:run_length([first|improper]) catch error:LengthError -> LengthError end,
    true = code:delete(M),
    code:purge(M)
end,
Check(Reference),
Check(Compiled),
io:format("Unicode mapping and pre-frame length preserve output, argument order and improper-list errors.~n"),
halt().
'@

$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $GeneratedBeam
exit $LASTEXITCODE
