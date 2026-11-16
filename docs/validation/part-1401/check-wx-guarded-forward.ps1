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
  maps:from_list([{N,[I || I<-Is,element(1,I)=/=label]} || {function,N,A,_,Is}<-Fs, (N=/=new orelse A=:=2), (N=/=cursor orelse A=:=1)])
end,
lists:foreach(fun(N)->A=maps:get(N,Read(R)),B=maps:get(N,Read(C)),io:format("~p ~p~n",[N,A=:=B]),
  case A=:=B of true->ok; false->io:format("OTP ~p~nC#  ~p~n",[A,B]) end
end,[new,text_options,cursor]),
lists:foreach(fun(Bin)->
  {module,M}=code:load_binary(M,S,Bin),
  Parent={wx_ref,1,wxWindow,[]},
  Box={wx_ref,2,wxStaticBox,[]},
  This={wx_ref,3,wxStyledTextCtrl,[]},
  {1,Parent,[]}=M:new(1,Parent),
  function_clause=try M:new(1,bad) catch error:E1->E1 end,
  {wx,unknown_env}=try M:new(Box,1) catch error:E2->E2 end,
  {wx,unknown_env}=try M:text_options(This,"face",10,false,[]) catch error:E3->E3 end,
  {badoption,bad}=try M:text_options(This,<<"face">>,10,true,[bad]) catch error:E4->E4 end,
  function_clause=try M:text_options(This,42,10,false,[]) catch error:E5->E5 end,
  {"cursor",[]}=M:cursor("cursor"),
  {wx,unknown_env}=try M:cursor(1) catch error:E6->E6 end,
  {badarg,bad}=try M:cursor({wx_ref,1,bad,[]}) catch error:E7->E7 end,
  true=code:delete(M), code:purge(M)
end,[R,C]),
io:format("Complete image match ~p~n",[R=:=C]),true=R=:=C,halt().
'@
$env:ERL_CRASH_DUMP_SECONDS='0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $e -extra $s $outputPath ($outputPath + '.otp.beam')
exit $LASTEXITCODE
