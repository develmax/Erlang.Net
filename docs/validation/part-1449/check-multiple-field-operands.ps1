param([string] $OtpRoot, [string] $Tool, [string] $Output, [string] $Source)

$ErrorActionPreference='Stop'
$s=(Resolve-Path $Source).Path
$a=[Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$compile=$a.GetType('Erlang.Compiler.CodeGeneration',$true).GetMethod('CompileBeamFile')
$outputPath=$ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
[IO.File]::WriteAllBytes($outputPath,$compile.Invoke($null,[object[]]@($s,$null,$null,$null)))
$e=@'
[S,P,O]=init:get_plain_arguments(),
{ok,M,R}=compile:file(S,[binary,deterministic]),
ok=file:write_file(O,R),
{ok,C}=file:read_file(P),
Read=fun(Bin,Name,Arity) ->
    {beam_file,_,_,_,_,Fs}=beam_disasm:file(Bin),
    [{function,Name,Arity,_,Is}]=[F||F={function,N,A,_,_}<-Fs,N=:=Name,A=:=Arity],
    Is
end,
lists:foreach(fun({Label,Bin}) ->
    {module,M}=code:load_binary(M,S,Bin),
    Left={parity_record,left,other_left},
    Right={parity_record,other_right,right},
    {left,marker,right}=M:checked_fields(Left,Right),
    {'EXIT',{{badrecord,invalid_left},_}}=(catch M:checked_fields(invalid_left,invalid_right)),
    {'EXIT',{{badrecord,invalid_right},_}}=(catch M:checked_fields(Left,invalid_right)),
    Final=Read(Bin,checked_fields,2),
    io:format("~s checked fields ~0p~n",[Label,Final]),
    [{1,{x,0}},{2,{x,2}}]=lists:sort([{Index,Dest}||{get_tuple_element,_,Index,Dest}<-Final]),
    {left,other_left,left}=M:record_args(Left),
    Repeated=Read(Bin,record_args,1),
    io:format("~s repeated source ~0p~n",[Label,Repeated]),
    [_]=[I||I={badrecord,_}<-Repeated],
    true=code:delete(M), code:purge(M)
end,[{"OTP",R},{"C#",C}]),
io:format("Execution checks passed~n"),halt().
'@
$env:ERL_CRASH_DUMP_SECONDS='0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $e -extra $s $outputPath ($outputPath + '.otp.beam')
exit $LASTEXITCODE
