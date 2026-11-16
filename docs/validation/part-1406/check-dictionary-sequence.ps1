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
Read=fun(B) ->
  {beam_file,_,_,_,_,Fs}=beam_disasm:file(B),
  maps:from_list([{N,[I || I<-Is,element(1,I)=/=label]} || {function,N,_,_,Is}<-Fs])
end,
lists:foreach(fun(N)->A=maps:get(N,Read(R)),B=maps:get(N,Read(C)),io:format("~p ~p~n",[N,A=:=B]),
  case A=:=B of true->ok; false->io:format("OTP ~p~nC#  ~p~n",[A,B]) end
end,[resolve,pair]),
lists:foreach(fun(Bin)->
  {module,M}=code:load_binary(M,S,Bin),
  erase(current), erase({undefined,imports}),
  {type,undefined,undefined}=M:resolve(type),
  put(current,library), put({library,imports},[item]),
  {type,library,[item]}=M:resolve(type),
  put(key,token), put({token,extra},42),
  {input,token,42}=M:pair(key,input),
  true=code:delete(M), code:purge(M)
end,[R,C]),
io:format("Complete image match ~p~n",[R=:=C]),true=R=:=C,halt().
'@
$env:ERL_CRASH_DUMP_SECONDS='0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $e -extra $s $outputPath ($outputPath + '.otp.beam')
exit $LASTEXITCODE
