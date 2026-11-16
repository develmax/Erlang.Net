param([string] $OtpRoot, [string] $PreviousTool, [string] $Tool, [string] $Output, [switch] $CompileOnly)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'literal_body_probe.erl'
$outputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
function Write-ProbeBeam {
    param([string] $CompilerDirectory, [string] $SourcePath, [string] $OutputPath)
    $compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $CompilerDirectory).Path 'Erlang.Compiler.dll'))
    $compileBeam = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile')
    [IO.File]::WriteAllBytes($OutputPath, $compileBeam.Invoke($null, [object[]]@($SourcePath, $null, $null, $null)))
}

if ($CompileOnly) {
    Write-ProbeBeam $Tool $source $outputPath
    exit 0
}

$previousPath = $outputPath + '.previous.beam'
& (Get-Process -Id $PID).Path -NoProfile -File $PSCommandPath -Tool $PreviousTool -Output $previousPath -CompileOnly
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-ProbeBeam $Tool $source $outputPath

$check = @'
[Source, Previous, Output] = init:get_plain_arguments(),
M = literal_body_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Old} = file:read_file(Previous),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    skipped = M:dispatch(skip,bad,ignored),
    {unexpected,value} = M:dispatch(other,value,ignored),
    put(trace,[]), {value,keep} = M:dispatch(run,value,keep), [value,value] = erase(trace),
    put(trace,[]), bad = try M:dispatch(run,bad,keep) catch throw:R1 -> R1 end, [bad] = erase(trace),
    put(trace,[]), {b,done} = M:recycle(run,a,b), [b,b,a] = erase(trace),
    a = M:recycle(other,a,ignored),
    ok = M:protected(default),
    put(trace,[]), {ok,value} = M:protected(value), [value] = erase(trace),
    put(trace,[]), ignored = M:protected(bad), [bad] = erase(trace),
    put(trace,[]), {ran,1} = M:guarded(run,1), [2,1] = erase(trace),
    {skipped,value} = M:guarded(run,value),
    value = M:guarded(other,value),
    true = code:delete(M),
    code:purge(M)
end,
lists:foreach(Check,[Reference,Old,Generated]),
Frames = fun(Beam) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    maps:from_list([{F,lists:max([0 | [element(2,I) || I <- Is, is_tuple(I),
        lists:member(element(1,I),[allocate,allocate_heap,allocate_zero,allocate_heap_zero])]])}
        || {function,F,_,_,Is} <- Fs])
end,
R = Frames(Reference), O = Frames(Old), G = Frames(Generated),
Rows = [{F,maps:get(F,O),maps:get(F,G),maps:get(F,R)}
    || F <- [dispatch,recycle,protected,guarded]],
io:format("Function, previous stack slots, current stack slots, OTP stack slots: ~p~n",[Rows]),
ReadInstructions = fun(Beam, Name, Arity) ->
    {beam_file,M,_,_,_,Functions} = beam_disasm:file(Beam),
    [Instructions] = [Is || {function,F,A,_,Is} <- Functions, F =:= Name, A =:= Arity],
    Labels = [L || {label,L} <- Instructions],
    Map = maps:from_list(lists:zip(Labels,lists:seq(1,length(Labels)))),
    Normalize = fun Fix({label,L}) -> {label,maps:get(L,Map,L)};
        Fix({f,L}) -> {f,maps:get(L,Map,L)};
        Fix({literal,_}=Literal) -> Literal;
        Fix(Tuple) when is_tuple(Tuple) -> list_to_tuple([Fix(X) || X <- tuple_to_list(Tuple)]);
        Fix([H|T]) -> [Fix(H)|Fix(T)];
        Fix(Value) -> Value
    end,
    [Normalize(I) || I <- Instructions]
end,
Targets = [{dispatch,3},{recycle,3},{protected,1},{guarded,2}],
First = fun(Beam, {Name,Arity}) ->
    hd([I || I <- ReadInstructions(Beam,Name,Arity), is_tuple(I),
        not lists:member(element(1,I),[label,line,func_info])])
end,
io:format("First instructions by function, previous, current and OTP: ~p~n",
    [[{Target,First(Old,Target),First(Generated,Target),First(Reference,Target)}
        || Target <- Targets]]),
Matches = [Name || {Name,Arity} <- Targets,
    ReadInstructions(Generated,Name,Arity) =:= ReadInstructions(Reference,Name,Arity)],
io:format("Functions with matching instructions: ~p~n",[Matches]),
case {[First(Generated,Target) || Target <- Targets],
    lists:all(fun(Name) -> lists:member(Name,Matches) end,[dispatch,recycle])} of
    {[{select_val,_,_,_},{test,is_eq_exact,_,_},{test,is_eq_exact,_,_},{test,is_eq_exact,_,_}],true} -> halt();
    _ -> halt(1)
end.
'@

$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $previousPath $outputPath
exit $LASTEXITCODE
