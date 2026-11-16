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
end,[leading,trailing,middle]),
Mock="-module(wxe_util). -export([color/1,queue_cmd/5,queue_cmd/6,rec/1]). color({R,G,B})->{R,G,B,255}; color(RGBA)->RGBA. queue_cmd(A,B,C,Env,Cmd)->put(colour_probe,{A,B,C,Env,Cmd}),ok. queue_cmd(A,B,C,D,Env,Cmd)->put(colour_probe,{A,B,C,D,Env,Cmd}),ok. rec(Cmd)->{Cmd,get(colour_probe)}.",
{ok,Tokens,_}=erl_scan:string(Mock),
Parse=fun F([],[],Acc)->lists:reverse(Acc);
  F([{dot,_}=Dot|Tail],Reverse,Acc)->{ok,Form}=erl_parse:parse_form(lists:reverse([Dot|Reverse])),F(Tail,[],[Form|Acc]);
  F([Token|Tail],Reverse,Acc)->F(Tail,[Token|Reverse],Acc)
end,
{ok,wxe_util,Stub}=compile:forms(Parse(Tokens,[],[]),[binary]),
{module,wxe_util}=code:load_binary(wxe_util,"probe",Stub),
lists:foreach(fun(Bin)->
  {module,M}=code:load_binary(M,S,Bin),
  Font={wx_ref,1,wxFont,[]},
  Calendar={wx_ref,2,wxCalendarCtrl,[]},
  DC={wx_ref,3,wxDC,[]},
  A={1,2,3}, B={4,5,6,7}, RGBA={1,2,3,255},
  erase(wx_env),
  {wx,unknown_env}=try M:leading(A,B,Font) catch error:E1->E1 end,
  function_clause=try M:leading(A,{bad,5,6},Font) catch error:E2->E2 end,
  function_clause=try M:trailing(Calendar,{1,2},B) catch error:E3->E3 end,
  function_clause=try M:middle(DC,A,B,{1,bad}) catch error:E4->E4 end,
  put(wx_env,{wx_env,env,none,false}),
  {1756,{RGBA,B,Font,env,1756}}=M:leading(A,B,Font),
  ok=M:trailing(Calendar,A,B),
  {Calendar,RGBA,B,env,773}=get(colour_probe),
  ok=M:middle(DC,A,B,{8,9}),
  {DC,RGBA,B,{8,9},env,705}=get(colour_probe),
  true=code:delete(M), code:purge(M)
end,[R,C]),
io:format("Complete image match ~p~n",[R=:=C]),true=R=:=C,halt().
'@
$env:ERL_CRASH_DUMP_SECONDS='0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $e -extra $s $outputPath ($outputPath + '.otp.beam')
exit $LASTEXITCODE
