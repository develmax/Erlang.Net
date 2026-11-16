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
Read=fun(Bin) ->
    {beam_file,_,_,_,_,Fs}=beam_disasm:file(Bin),
    [{function,dispatch_entry_tail,3,_,Is}]=[F||F={function,dispatch_entry_tail,3,_,_}<-Fs],
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
Expected=Read(R),
Actual=Read(C),
io:format("OTP ~0p~nC# ~0p~n",[Expected,Actual]),
Normalized=Normalize(Expected),
Normalized=Normalize(Actual),
lists:foreach(fun(Bin) ->
    {module,M}=code:load_binary(M,S,Bin),
    {start,1,done}=M:dispatch_entry_tail(start,alpha,done),
    {start,1,done}=M:dispatch_entry_tail(start,beta,done),
    {start,2,done}=M:dispatch_entry_tail(start,gamma,done),
    {missing,unknown,done}=M:dispatch_entry_tail(start,unknown,done),
    {missing,7,done}=M:dispatch_entry_tail(start,7,done),
    true=code:delete(M), code:purge(M)
end,[R,C]),
io:format("Execution and normalized entry tail dispatch instructions passed~n"),halt().
'@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $oracle -extra $sourcePath $outputPath
exit $LASTEXITCODE
