param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference='Stop'
$s=(Resolve-Path 'tests/Erlang.Tests/Resources/Beam/beam_dynamic_apply_tail_probe.erl').Path
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
end,[invoke,explicit,syntax,dynamic_function,variadic,foreign]),
lists:foreach(fun(Bin)->
  {module,M}=code:load_binary(M,S,Bin),
  A=[a], B=[b],
  [a,b]=M:invoke(lists,A,B),
  [b,a]=M:explicit(A,lists,B),
  [a,b]=M:syntax(lists,A,B),
  [b,a]=M:dynamic_function(lists,reverse,[a,b]),
  [a,b]=M:variadic(lists,append,[A,B]),
  badarg=try M:invoke(42,A,B) catch error:E1->E1 end,
  undef=try M:foreign(lists,A,B) catch error:E2->E2 end,
  true=code:delete(M), code:purge(M)
end,[R,C]),
io:format("Complete image match ~p~n",[R=:=C]),true=R=:=C,halt().
'@
$env:ERL_CRASH_DUMP_SECONDS='0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $e -extra $s $outputPath ($outputPath + '.otp.beam')
exit $LASTEXITCODE
