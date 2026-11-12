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
    [{function,Name,3,_,Is}]=[F||F={function,N,3,_,_}<-Fs,N=:=Name],
    Is
end,
lists:foreach(fun({Label,Bin}) ->
    Is=Read(Bin,run),
    io:format("~s ~0p~n",[Label,Is]),
    [{call,1,{M,select,1}},{test,Shape,_,[{x,0}|_]}|_]=
        lists:dropwhile(fun(I)->I=/={call,1,{M,select,1}} end,Is),
    true=lists:member(Shape,[is_tuple,is_tagged_tuple]),
    {module,M}=code:load_binary(M,S,Bin),
    {match,{state,state},value}=M:run({entry,key,value},key,state),
    {fallback,{state,state},value}=M:run({entry,fallback,value},key,state),
    {error,reason}=M:run({error,reason},key,state),
    Invalid={entry,other,value},
    {'EXIT',{{case_clause,Invalid},_}}=(catch M:run(Invalid,key,state)),
    {'EXIT',{{case_clause,invalid},_}}=(catch M:run(invalid,key,state)),
    {value,state,key}=M:tail({entry,key,value},key,state),
    no=M:tail({entry,other,value},key,state),
    Tail=Read(Bin,tail),
    io:format("~s tail ~0p~n",[Label,Tail]),
    lists:foreach(fun(Code) ->
        true=lists:any(fun(I)->case I of
            {get_tuple_element,{x,0},2,{x,_}} -> true;
            _ -> false
        end end,Code)
    end,[Is,Tail]),
    true=code:delete(M), code:purge(M)
end,[{"OTP",R},{"C#",C}]),
io:format("Execution checks passed~n"),halt().
'@
$env:ERL_CRASH_DUMP_SECONDS='0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $e -extra $s $outputPath ($outputPath + '.otp.beam')
exit $LASTEXITCODE
