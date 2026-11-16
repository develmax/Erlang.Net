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
    {fallback,kept,marker}=M:final_field(fallback,{parity_record,old,kept}),
    {'EXIT',{{badrecord,wrong_record},_}}=(catch M:final_field(fallback,wrong_record)),
    Final=Read(Bin,final_field,2),
    io:format("~s final record field ~0p~n",[Label,Final]),
    [_]=[I||I={get_tuple_element,_,2,{x,1}}<-Final],
    true=code:delete(M), code:purge(M)
end,[{"OTP",R},{"C#",C}]),
io:format("Execution checks passed~n"),halt().
'@
$env:ERL_CRASH_DUMP_SECONDS='0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $e -extra $s $outputPath ($outputPath + '.otp.beam')
exit $LASTEXITCODE
