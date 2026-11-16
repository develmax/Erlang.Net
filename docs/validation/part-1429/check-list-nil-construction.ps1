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
Read=fun(Bin) ->
    {beam_file,_,_,_,_,Fs}=beam_disasm:file(Bin),
    [{function,scan,3,_,Is}]=[F||F={function,scan,3,_,_}<-Fs],
    [I || I <- Is, not is_tuple(I) orelse element(1,I) =/= label]
end,
A=Read(R), B=Read(C),
io:format("OTP ~0p~nC#  ~0p~n",[A,B]),
io:format("Whole image match ~p~n",[R=:=C]),
true=(R=:=C),
lists:foreach(fun(Bin) ->
    {module,M}=code:load_binary(M,S,Bin),
    {[tail],4,other}=M:scan([ignored,{mark,1,2},tail],4,other),
    [0,4,0]=M:scan([ignored],4,other),
    [0,value,0]=M:scan([],value,other),
    {'EXIT',{function_clause,_}}=(catch M:scan([ignored|improper],4,other)),
    true=code:delete(M), code:purge(M)
end,[R,C]),
io:format("Execution checks passed~n"),halt().
'@
$env:ERL_CRASH_DUMP_SECONDS='0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $e -extra $s $outputPath ($outputPath + '.otp.beam')
exit $LASTEXITCODE
