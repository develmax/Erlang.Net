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
    [key,key]=M:filter_known([key,other,key]),
    [other]=M:filter_negated([key,other,key]),
    [{mapped,key}]=M:filter_mapped([other,key]),
    [true]=M:filter_general([true,false]),
    [false]=M:filter_general_not([true,false]),
    []=M:filter_mapped([]),
    {'EXIT',{{bad_filter,other},_}}=(catch M:filter_general([other|improper])),
    {'EXIT',{badarg,_}}=(catch M:filter_general_not([other])),
    {'EXIT',{invalid_item,_}}=(catch M:filter_general([invalid])),
    {'EXIT',{{bad_generator,improper},_}}=(catch M:filter_known([other|improper])),
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
lists:foreach(fun(Name)->
    Helper=list_to_atom("-"++atom_to_list(Name)++"/1-lc$^0/1-0-"),
    Expected=Read(R,Helper,1),
    Actual=Read(C,Helper,1),
    io:format("OTP ~p ~0p~nC# ~p ~0p~n",[Helper,Expected,Helper,Actual]),
    ReferenceBody=Normalize(Expected),
    ReferenceBody=Normalize(Actual)
end,[filter_known,filter_negated,filter_general,filter_general_not,filter_mapped]),
io:format("Execution and normalized helper instruction checks passed~n"),halt().
'@
$env:ERL_CRASH_DUMP_SECONDS='0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $e -extra $s $outputPath ($outputPath + '.otp.beam')
exit $LASTEXITCODE
