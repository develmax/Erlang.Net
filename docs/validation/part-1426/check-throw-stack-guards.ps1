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
Targets=[{hidden,raise},{seen,throw},{checked,throw},{escaped_scan,throw},{cleanup_scan,throw},{choice_scan,throw}],
Check=fun(Bin) ->
  {beam_file,_,_,_,_,Fs}=beam_disasm:file(Bin),
  lists:foreach(fun({Name,Expected}) ->
    [Code]=[Is || {function,N,2,_,Is} <- Fs, N=:=Name],
    [Expected]=[F || {Op,_,{extfunc,erlang,F,_}} <- Code,
                     (Op=:=call_ext_only orelse Op=:=call_ext), (F=:=throw orelse F=:=raise)],
    io:format("~p ~p~n",[Name,Expected])
  end,Targets),
  {module,M}=code:load_binary(M,S,Bin),
  handled=M:safe([]),
  true=M:observed([]),
  handled=M:bound([],{error,missing}),
  try M:bound([],wrong) of _ -> erlang:error(unexpected_return)
  catch throw:{error,missing}:Stack -> true=(Stack=/=[]) end,
  true=M:mismatch(), true=M:escaped(),
  {error,missing}=M:cleanup(), true=erase(cleaned),
  first=M:choices(),
  true=code:delete(M), code:purge(M)
end,
lists:foreach(Check,[R,C]),
io:format("Execution checks passed~n"),halt().
'@
$env:ERL_CRASH_DUMP_SECONDS='0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $e -extra $s $outputPath ($outputPath + '.otp.beam')
exit $LASTEXITCODE
























