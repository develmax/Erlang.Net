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
Check=fun(Bin) ->
  {beam_file,_,_,_,_,Fs}=beam_disasm:file(Bin),
  lists:foreach(fun({Name,Expected}) ->
    [Code]=[Is || {function,N,_,_,Is} <- Fs, N=:=Name],
    [Expected]=[F || I <- Code, is_tuple(I), tuple_size(I)>=3,
                     {extfunc,erlang,F,_} <- [element(3,I)], (F=:=throw orelse F=:=raise)],
    io:format("~p ~p~n",[Name,Expected])
  end,[{choose,raise},{keep_stack,throw},{switch_scan,throw},{nested_scan,throw}]),
  {module,M}=code:load_binary(M,S,Bin),
  "42"=M:caught(42),
  {"-7",true}=M:observed(-7),
  {'EXIT',{badarg,_}}=(catch M:caught(invalid)),
  {first,"42"}=M:tuple_switch(42),
  "42"=M:nested(42),
  invalid=M:nested(invalid),
  true=code:delete(M), code:purge(M)
end,
lists:foreach(Check,[R,C]),
io:format("Execution checks passed~n"),halt().
'@
$env:ERL_CRASH_DUMP_SECONDS='0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $e -extra $s $outputPath ($outputPath + '.otp.beam')
exit $LASTEXITCODE
