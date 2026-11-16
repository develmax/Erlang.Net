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
    [{tag,first},{tag,second}]=M:map_after_call([first,second],tag),
    []=M:map_after_call([],tag),
    {'EXIT',{{bad_generator,improper},_}}=(catch M:map_after_call([first|improper],tag)),
    {'EXIT',{{bad_generator,improper},_}}=(catch M:map_after_call(improper,tag)),
    {'EXIT',{invalid_item,_}}=(catch M:map_after_call([invalid],tag)),
    {'EXIT',{invalid_item,_}}=(catch M:map_after_call([invalid|improper],tag)),
    [{tag,first},{recovered,tag}]=M:map_caught([first,invalid],tag),
    lists:foreach(fun(Name)->
        Helper=list_to_atom("-"++atom_to_list(Name)++"/2-lc$^0/1-0-"),
        Parent=Read(Bin,Name,2),
        Code=Read(Bin,Helper,2),
        io:format("~s ~p parent ~0p~n~s ~p helper ~0p~n",[Label,Name,Parent,Label,Name,Code]),
        [{allocate,1,2}]=[I||I={allocate,_,_}<-Parent],
        [{call_last,2,{M,Helper,2},1}]=[I||I={call_last,_,_,_}<-Parent]
    end,[map_after_call,map_caught]),
    true=code:delete(M), code:purge(M)
end,[{"OTP",R},{"C#",C}]),
Normalize=fun(Instructions)->[I||I<-Instructions,not (is_tuple(I) andalso element(1,I)=:=label)] end,
lists:foreach(fun(Name)->
    Body=Normalize(Read(R,Name,2)),
    Body=Normalize(Read(C,Name,2))
end,[map_after_call,map_caught]),
io:format("Execution and normalized parent instruction checks passed~n"),halt().
'@
$env:ERL_CRASH_DUMP_SECONDS='0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $e -extra $s $outputPath ($outputPath + '.otp.beam')
exit $LASTEXITCODE
