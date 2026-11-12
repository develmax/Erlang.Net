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
Read=fun(Bin,Name) ->
    {beam_file,_,_,_,_,Fs}=beam_disasm:file(Bin),
    [{function,Name,2,_,Is}]=[F||F={function,N,2,_,_}<-Fs,N=:=Name],
    Is
end,
lists:foreach(fun({Label,Bin}) ->
    lists:foreach(fun(Name) ->
        Is=Read(Bin,Name),
        io:format("~s ~p ~0p~n",[Label,Name,Is]),
        [_]=[Y || {put_tuple2,{y,Y},{list,[{atom,state},{y,Y},{x,0}]}} <- Is]
    end,[run,retain]),
    {module,M}=code:load_binary(M,S,Bin),
    {state,payload,{rule,payload}}=M:run(1,payload),
    no=M:run(2,payload),
    {{state,payload,{rule,payload}},{rule,payload}}=M:retain(1,payload),
    true=code:delete(M), code:purge(M)
end,[{"OTP",R},{"C#",C}]),
io:format("Execution checks passed~n"),halt().
'@
$env:ERL_CRASH_DUMP_SECONDS='0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $e -extra $s $outputPath ($outputPath + '.otp.beam')
exit $LASTEXITCODE
