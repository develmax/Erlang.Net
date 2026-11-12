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
  maps:from_list([{N,[case I of
      {make_fun3,F,_,_,D,Caps} -> {make_fun3,F,D,Caps};
      Other -> Other
    end || I<-Is,element(1,I)=/=label]} || {function,N,_,_,Is}<-Fs])
end,
lists:foreach(fun(N)->A=maps:get(N,Read(R)),B=maps:get(N,Read(C)),io:format("~p ~p~n",[N,A=:=B]),
  case A=:=B of true->ok; false->io:format("OTP ~p~nC#  ~p~n",[A,B]) end
end,[merge]),
lists:foreach(fun(Bin)->
  {module,M}=code:load_binary(M,S,Bin),
  []=M:merge([],[]),
  lists:foreach(fun(Mode) ->
    First={item,first,flags,Mode,old},
    Second={item,second,other,final,new},
    [{item,first,flags,final,new},tail]=M:merge([First,Second,tail],[]),
    [First]=M:merge([First],[]),
    {'EXIT',{{badrecord,bad},_}}=(catch M:merge([First,bad],[]))
  end,[extend,{default,extend},{default,auto}]),
  [{item,first,flags,{other,extend},old},tail]=M:merge([{item,first,flags,{other,extend},old},tail],[]),
  {'EXIT',{function_clause,_}}=(catch M:merge(bad,[])),
  true=code:delete(M), code:purge(M)
end,[R,C]),
{ok,_,RC}=beam_lib:all_chunks(R),{ok,_,CC}=beam_lib:all_chunks(C),io:format("Differing chunks ~p~n",[[K||{K,V}<-RC,proplists:get_value(K,CC)=/=V]]),io:format("Complete image match ~p~n",[R=:=C]),true=(R=:=C),io:format("Execution checks passed~n"),halt().
'@
$env:ERL_CRASH_DUMP_SECONDS='0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $e -extra $s $outputPath ($outputPath + '.otp.beam')
exit $LASTEXITCODE
























