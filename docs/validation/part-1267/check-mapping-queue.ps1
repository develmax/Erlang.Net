param(
    [Parameter(Mandatory)] [string] $OtpRoot,
    [Parameter(Mandatory)] [string] $CorpusToolDirectory,
    [Parameter(Mandatory)] [string] $GeneratedBeam
)

$ErrorActionPreference = 'Stop'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $CorpusToolDirectory).Path 'Erlang.Compiler.dll'))
$source = Join-Path $PSScriptRoot 'mapping_queue_probe.erl'
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
[IO.File]::WriteAllBytes($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($GeneratedBeam), $bytes)

$check = @'
[Source, Generated] = init:get_plain_arguments(),
M = mapping_queue_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Compiled} = file:read_file(Generated),
{beam_file,M,_,_,_,Fs} = beam_disasm:file(Compiled),
[Run] = [I || {function,run,2,_,I} <- Fs],
true = lists:member({allocate,2,2},Run),
true = lists:member({trim,2,0},Run),
true = lists:member({call_ext,4,{extfunc,wxe_util,queue_cmd,4}},Run),
[Dispatch] = [I || {function,dispatch,1,_,I} <- Fs],
true = lists:any(fun({select_val,_,_,_}) -> true; (_) -> false end,Dispatch),
[Forward] = [I || {function,dispatch_forward,1,_,I} <- Fs],
[Target] = [I || I = {call_only,2,_} <- Forward],
true = is_tuple(Target),
[Reason] = [I || {function,reason,1,_,I} <- Fs],
true = lists:member({move,{literal,{nif_not_loaded,line,17}},{x,0}},Reason),
Vars = [{var,1,V} || V <- ['A','B','C','D']],
Stub = [{attribute,1,module,wxe_util},{attribute,1,export,[{queue_cmd,4}]},
        {function,1,queue_cmd,4,[{clause,1,Vars,[],
          [{call,1,{atom,1,put},[{atom,1,probe_queue},{tuple,1,Vars}]}]}]}],
{ok,wxe_util,StubBeam} = compile:forms(Stub,[binary]),
{module,wxe_util} = code:load_binary(wxe_util,"local test stub",StubBeam),
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    ok = M:run(42,["a",[233]]),
    {42,2,[<<97,0>>,<<195,169,0>>],7} = get(probe_queue),
    {bad_generator,improper} = try M:run(42,["a"|improper]) catch error:Bad -> Bad end,
    {42,2,[<<97,0>>,<<195,169,0>>],7} = erase(probe_queue),
    {ok,1} = M:dispatch(first),
    {ok,2} = M:dispatch(second),
    {first,3} = M:dispatch_forward(first),
    {second,3} = M:dispatch_forward(second),
    function_clause = try M:dispatch(other) catch error:DispatchError -> DispatchError end,
    {nif_not_loaded,line,17} = try M:fail() catch error:StubError -> StubError end,
    true = code:delete(M),
    code:purge(M)
end,
Check(Reference),
Check(Compiled),
io:format("Mapping, length, queue operands, atom dispatch and the constant NIF error agree with OTP.~n"),
halt().
'@

$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $GeneratedBeam
exit $LASTEXITCODE
