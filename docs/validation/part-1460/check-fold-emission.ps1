param([string] $OtpRoot, [string] $Tool, [string] $Output, [string] $Source)

$ErrorActionPreference = 'Stop'
$sourcePath = (Resolve-Path $Source).Path
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$compile = $assembly.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile')
$outputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
[IO.File]::WriteAllBytes($outputPath, $compile.Invoke($null, [object[]]@($sourcePath, $null, $null, $null)))
$oracle = @'
[S,P]=init:get_plain_arguments(),
{ok,M,R}=compile:file(S,[binary,deterministic]),
{ok,C}=file:read_file(P),
Read=fun(Bin,Name) ->
    {beam_file,_,_,_,_,Fs}=beam_disasm:file(Bin),
    [{function,Name,1,_,Is}]=[F||F={function,N,1,_,_}<-Fs,N=:=Name],
    Is
end,
Normalize=fun([{label,Start}|_]=Is) ->
    Rebase=fun F({f,0}) -> {f,0};
               F({f,L}) -> {f,L-Start};
               F({make_fun3,Target,_,_,Register,Environment}) -> {make_fun3,Target,Register,Environment};
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
end,[record_fold]),
lists:foreach(fun(Bin) ->
    {module,M}=code:load_binary(M,S,Bin),
    [14,12]=M:run([2,skip,4],10),
    [14,12]=M:single([2,4],10),
    []=M:single([],10),
    [{first,value},{second,value},extra]=M:record_fold({fold_record,value,[extra]}),
    {'EXIT',{badarith,Stack}}=(catch M:single([bad],10)),
    true=lists:keymember('-single/2-fun-0-',2,Stack),
    true=code:delete(M), code:purge(M)
end,[R,C]),
io:format("Execution and normalized record fold entry instructions passed~n"),halt().
'@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $oracle -extra $sourcePath $outputPath
exit $LASTEXITCODE
