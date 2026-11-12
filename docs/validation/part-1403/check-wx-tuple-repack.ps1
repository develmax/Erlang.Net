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
end,[options,set,range,'-options/1-fun-0-']),
Mock="-module(wxe_util). -export([queue_cmd/3,queue_cmd/4,queue_cmd/5,rec/1]). queue_cmd(A,Env,Cmd)->put(tuple_probe,{A,Env,Cmd}),ok. queue_cmd(A,B,Env,Cmd)->put(tuple_probe,{A,B,Env,Cmd}),ok. queue_cmd(A,B,C,Env,Cmd)->put(tuple_probe,{A,B,C,Env,Cmd}),ok. rec(Cmd)->{Cmd,get(tuple_probe)}.",
{ok,Tokens,_}=erl_scan:string(Mock),
Parse=fun F([],[],Acc)->lists:reverse(Acc);
  F([{dot,_}=Dot|Tail],Reverse,Acc)->{ok,Form}=erl_parse:parse_form(lists:reverse([Dot|Reverse])),F(Tail,[],[Form|Acc]);
  F([Token|Tail],Reverse,Acc)->F(Tail,[Token|Reverse],Acc)
end,
{ok,wxe_util,Stub}=compile:forms(Parse(Tokens,[],[]),[binary]),
{module,wxe_util}=code:load_binary(wxe_util,"probe",Stub),
lists:foreach(fun(Bin)->
  {module,M}=code:load_binary(M,S,Bin),
  Receiver={wx_ref,1,wxCalendarCtrl,[]},
  Value={{2026,11,6},{9,8,7}}, Packed={6,11,2026,9,8,7},
  Malformed={date,{{2026,11,6},{9,8}}},
  erase(wx_env),
  {wx,unknown_env}=try M:set(Receiver,Value) catch error:E1->E1 end,
  function_clause=try M:set(Receiver,{{bad,11,6},{9,8,7}}) catch error:E2->E2 end,
  {badoption,Malformed}=try M:options([Malformed]) catch error:E3->E3 end,
  put(wx_env,{wx_env,env,none,false}),
  {1506,{Receiver,Packed,env,1506}}=M:set(Receiver,Value),
  {1505,{[{date,Packed},{position,{1,2}},{style,3}],env,1505}}=
    M:options([{date,Value},{position,{1,2}},{style,3}]),
  ok=M:range(Receiver,Value,{{2027,12,31},{6,5,4}}),
  {Receiver,Packed,{31,12,2027,6,5,4},env,1507}=get(tuple_probe),
  true=code:delete(M), code:purge(M)
end,[R,C]),
io:format("Complete image match ~p~n",[R=:=C]),true=R=:=C,halt().
'@
$env:ERL_CRASH_DUMP_SECONDS='0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $e -extra $s $outputPath ($outputPath + '.otp.beam')
exit $LASTEXITCODE
