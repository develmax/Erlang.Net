param(
    [Parameter(Mandatory)] [string] $OtpRoot,
    [Parameter(Mandatory)] [string] $CorpusToolDirectory,
    [Parameter(Mandatory)] [string] $GeneratedBeam
)

$ErrorActionPreference = 'Stop'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $CorpusToolDirectory).Path 'Erlang.Compiler.dll'))
$source = Join-Path $PSScriptRoot 'type_or_tuple_probe.erl'
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
[IO.File]::WriteAllBytes($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($GeneratedBeam), $bytes)

$check = @'
[Source, Generated] = init:get_plain_arguments(),
M = type_or_tuple_probe,
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
[Guarded] = [Is || {function,guarded,3,_,Is} <- Fs],
true = lists:any(fun({select_val,{x,0},_,_}) -> true; (_) -> false end, Guarded),
true = lists:any(fun({test,test_arity,_,[{x,1},2]}) -> true; (_) -> false end, Guarded),
[Tail] = [Is || {function,tuple_tail,1,_,Is} <- Fs],
true = lists:member({get_tuple_element,{x,0},2,{x,0}},Tail),
[Prefixed] = [Is || {function,prefixed_tail,1,_,Is} <- Fs],
true = lists:member({get_tuple_element,{x,0},2,{x,3}},Prefixed),
[TypeOr] = [Is || {function,type_or,2,_,Is} <- Fs],
[Integer,Tuple,Binary] = [Opcode || {test,Opcode,_,[{x,0}]} <- TypeOr],
[is_integer,is_tuple,is_binary] = [Integer,Tuple,Binary],
[Second] = [Is || {function,second_tuple,2,_,Is} <- Fs],
true = lists:member({get_tuple_element,{x,1},0,{x,1}},Second),
[Rotate] = [Is || {function,rotate,1,_,Is} <- Fs],
[{swap,{x,2},{x,1}},{swap,{x,3},{x,2}},{swap,{x,0},{x,3}}] = [I || I = {swap,_,_} <- Rotate],
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    ok = M:check(key,#{key => value}),
    {done,42} = M:check_and_finish(key,#{key => value}),
    {badkey,missing} = try M:check(missing,#{}) catch error:Missing -> Missing end,
    function_clause = try M:check(key,invalid) catch error:Guard -> Guard end,
    ok = M:guarded(0,{left,right},#{0 => value}),
    ok = M:guarded(1,{left,right},#{1 => value}),
    function_clause = try M:guarded(0.0,{left,right},#{}) catch error:Float -> Float end,
    function_clause = try M:guarded(0,{left},#{}) catch error:Arity -> Arity end,
    {first,second,third} = M:tuple_tail({first,second,third}),
    {prefix,first,second,third} = M:prefixed_tail({first,second,third}),
    ok = M:type_or(42,#{42 => present}),
    ok = M:type_or({tag},#{{tag} => present}),
    ok = M:type_or(<<1>>,#{<<1>> => present}),
    function_clause = try M:type_or(other,#{other => present}) catch error:Type -> Type end,
    {prefix,value} = M:second_tuple(prefix,{value}),
    function_clause = try M:second_tuple(prefix,{value,extra}) catch error:SecondArity -> SecondArity end,
    {a,b,c,d} = M:rotate({a,b,c,d}),
    true = code:delete(M),
    code:purge(M)
end,
Check(Reference),
Check(Compiled),
io:format("Type alternatives and tuple forwarding agree with OTP, including second-argument extraction and four-register rotation.~n"),
halt().
'@

$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $GeneratedBeam
exit $LASTEXITCODE
