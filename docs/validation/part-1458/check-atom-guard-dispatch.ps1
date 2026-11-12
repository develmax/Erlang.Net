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
end,[atom_guard_table,atom_guard_if,literal_case_outer]),
lists:foreach(fun(Bin) ->
    {module,M}=code:load_binary(M,S,Bin),
    0=M:atom_guard_table(default),
    0=M:atom_guard_table(7),
    1=M:atom_guard_table(alpha),
    2=M:atom_guard_table(beta),
    2=M:atom_guard_table(gamma),
    3=M:atom_guard_table(delta),
    first=M:atom_guard_if(beta),
    second=M:atom_guard_if(delta),
    other=M:atom_guard_if(7),
    1=M:literal_case_outer(alpha),
    2=M:literal_case_outer(beta),
    7=M:literal_case_outer(7),
    {'EXIT',{{case_clause,unknown},_}}=(catch M:literal_case_outer(unknown)),
    true=code:delete(M), code:purge(M)
end,[R,C]),
io:format("Execution and normalized literal-result dispatch instructions passed~n"),halt().
'@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $oracle -extra $sourcePath $outputPath
exit $LASTEXITCODE
