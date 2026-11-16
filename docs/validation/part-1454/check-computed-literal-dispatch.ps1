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
Read=fun(Bin,Name,Arity) ->
    {beam_file,_,_,_,_,Fs}=beam_disasm:file(Bin),
    [{function,Name,Arity,_,Is}]=[F||F={function,N,A,_,_}<-Fs,N=:=Name,A=:=Arity],
    Is
end,
lists:foreach(fun({Label,Bin}) ->
    {module,M}=code:load_binary(M,S,Bin),
    <<"one">>=M:dispatch_binary(alpha),
    <<"one">>=M:dispatch_binary(beta),
    <<"two">>=M:dispatch_binary(gamma),
    <<"prefix:other">>=M:dispatch_binary(<<"other">>),
    {'EXIT',{badarg,_}}=(catch M:dispatch_binary(7)),
    one=M:dispatch_computed(alpha),
    two=M:dispatch_computed(beta),
    {mapped,{other,7}}=M:dispatch_computed({other,7}),
    [{select_val,{x,0},_,{list,[{atom,alpha},_,{atom,beta},_]}}]=
        [I||I={select_val,_,_,_}<-Read(Bin,dispatch_computed,1)],
    one=M:dispatch_throw(alpha),
    {error,{bad,other}}=(catch M:dispatch_throw(other)),
    []=[I||I={allocate,_,_}<-Read(Bin,dispatch_throw,1)],
    true=code:delete(M), code:purge(M)
end,[{"OTP",R},{"C#",C}]),
Normalize=fun([{label,Start}|_]=Is) ->
    Rebase=fun F({f,0}) -> {f,0};
               F({f,L}) -> {f,L-Start};
               F({label,L}) -> {label,L-Start};
               F(T) when is_tuple(T) -> list_to_tuple([F(X)||X<-tuple_to_list(T)]);
               F(L) when is_list(L) -> [F(X)||X<-L];
               F(X) -> X
           end,
    [Rebase(I)||I<-Is]
end,
Expected=Read(R,dispatch_binary,1),
Actual=Read(C,dispatch_binary,1),
io:format("OTP dispatch_binary ~0p~nC# dispatch_binary ~0p~n",[Expected,Actual]),
ReferenceBody=Normalize(Expected),
ReferenceBody=Normalize(Actual),
io:format("Execution and normalized dispatch instructions passed~n"),halt().
'@
$env:ERL_CRASH_DUMP_SECONDS='0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $e -extra $s $outputPath ($outputPath + '.otp.beam')
exit $LASTEXITCODE
