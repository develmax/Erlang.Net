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
end,[scan]),
lists:foreach(fun(Bin)->
  {module,M}=code:load_binary(M,S,Bin),
  lists:foreach(fun persistent_term:erase/1,[k0,k1,k2]),
  ok=M:scan(k0,[],[]),
  []=M:observed(k0),
  {k1,[rest]}=M:scan(k1,[ordinary,{entry,skip,kept},{entry,emit,payload},rest],[]),
  [ordinary,{entry,skip,kept},{entry,emit,payload}]=M:observed(k1),
  ok=M:scan(k2,[{other,emit,payload},{entry,skip,kept}],[]),
  [{other,emit,payload},{entry,skip,kept}]=M:observed(k2),
  {'EXIT',{function_clause,_}}=(catch M:scan(k3,[ordinary|bad],[])),
  true=code:delete(M), code:purge(M)
end,[R,C]),
io:format("Complete image match ~p~n",[R=:=C]),true=maps:get(scan,Read(R))=:=maps:get(scan,Read(C)),io:format("Execution checks passed~n"),halt().
'@
$env:ERL_CRASH_DUMP_SECONDS='0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $e -extra $s $outputPath ($outputPath + '.otp.beam')
exit $LASTEXITCODE
























