param([string] $OtpRoot, [string] $Tool, [string] $Output, [string] $Source)

$ErrorActionPreference='Stop'
$sourcePath=(Resolve-Path $Source).Path
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$compile=$assembly.GetType('Erlang.Compiler.CodeGeneration',$true).GetMethod('CompileBeamFile')
$outputPath=$ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
[IO.File]::WriteAllBytes($outputPath,$compile.Invoke($null,[object[]]@($sourcePath,$null,$null,$null)))
$oracle=@'
[S,P]=init:get_plain_arguments(),
{ok,M,R}=compile:file(S,[binary,deterministic]),
{ok,C}=file:read_file(P),
Read=fun(Bin,Name) ->
    {beam_file,_,_,_,_,Fs}=beam_disasm:file(Bin),
    [{function,Name,2,_,Is}]=[F||F={function,N,2,_,_}<-Fs,N=:=Name],
    Is
end,
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
lists:foreach(fun(Name) ->
    Expected=Read(R,Name),
    Actual=Read(C,Name),
    io:format("OTP ~p ~0p~nC# ~p ~0p~n",[Name,Expected,Name,Actual]),
    Normalized=Normalize(Expected),
    Normalized=Normalize(Actual)
end,[grouped_arity,grouped_arity_tail,grouped_arity_class,grouped_arity_default]),
lists:foreach(fun(Bin) ->
    {module,M}=code:load_binary(M,S,Bin),
    true=M:grouped_arity(alpha,1),
    true=M:grouped_arity(gamma,1),
    true=M:grouped_arity(beta,2),
    false=M:grouped_arity(gamma,2),
    false=M:grouped_arity(unknown,-1),
    {'EXIT',{function_clause,_}}=(catch M:grouped_arity(beta,2.0)),
    {'EXIT',{function_clause,_}}=(catch M:grouped_arity(2,1)),
    true=M:grouped_arity_tail(alpha,2),
    true=M:grouped_arity_tail(delta,3),
    false=M:grouped_arity_tail(delta,2),
    {'EXIT',{function_clause,_}}=(catch M:grouped_arity_tail(beta,wrong)),
    first=M:grouped_arity_class(gamma,1),
    first=M:grouped_arity_class(gamma,2),
    second=M:grouped_arity_class(delta,2),
    {'EXIT',{function_clause,_}}=(catch M:grouped_arity_class(delta,1)),
    true=M:grouped_arity_default(beta,2),
    true=M:grouped_arity_default(gamma,1),
    false=M:grouped_arity_default(beta,wrong),
    false=M:grouped_arity_default(1,1),
    true=code:delete(M), code:purge(M)
end,[R,C]),
io:format("Execution and normalized arity dispatch instructions passed~n"),halt().
'@
$env:ERL_CRASH_DUMP_SECONDS='0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $oracle -extra $sourcePath $outputPath
exit $LASTEXITCODE
