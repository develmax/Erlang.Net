param([string] $OtpRoot, [string] $PreviousTool, [string] $Tool, [string] $Output, [switch] $CompileOnly)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'list_clause_probe.erl'
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
M = list_clause_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Old} = file:read_file(Previous),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    put(trace,[]), 6 = M:sum([1,2,3],0), [3,2,1] = erase(trace),
    put(trace,[]), 7 = M:private(7), [7] = erase(trace),
    put(trace,[]), {4,[6]} = M:classify([4,6],take), [4] = erase(trace),
    put(trace,[]), ignored = M:classify([8],drop), [8] = erase(trace),
    other = M:classify(wrong,take),
    put(trace,[]), 10 = M:repeated([5,5],5,0), [5,5] = erase(trace),
    put(trace,[]), 7 = M:follow([3,7],0), [7,3] = erase(trace),
    put(trace,[]),
    rejected = try M:sum([1|wrong],0) catch error:function_clause -> rejected end,
    [1] = erase(trace),
    put(trace,[]),
    rejected = try M:repeated([1],2,0) catch error:function_clause -> rejected end,
    [] = erase(trace),
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
    || F <- [sum,proper,classify,repeated,follow]],
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
First = fun(Beam, Name) ->
    hd([I || I <- ReadInstructions(Beam,Name,2), is_tuple(I),
        not lists:member(element(1,I),[label,line,func_info])])
end,
io:format("First instructions for sum, proper, classify: ~p~n",
    [[{Name,First(Old,Name),First(Generated,Name),First(Reference,Name)}
       || Name <- [sum,proper,classify]]]),
HasNil = fun(Name) ->
    lists:any(fun(I) -> case I of {test,is_nil,_,_} -> true; _ -> false end end,
        ReadInstructions(Generated,Name,2))
end,
Follow = ReadInstructions(Generated,follow,2),
Match = Follow =:= ReadInstructions(Reference,follow,2),
io:format("follow/2 matches OTP instructions: ~p~n",[Match]),
case {First(Generated,sum),First(Generated,proper),First(Generated,classify),HasNil(sum),HasNil(proper),
    lists:member({get_list,{x,0},{x,0},{y,0}},Follow),Match} of
    {{test,is_nonempty_list,_,_},{test,is_nonempty_list,_,_},{test,is_nonempty_list,_,_},true,false,true,true} -> halt();
    _ -> halt(1)
end.
'@

$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $previousPath $outputPath
exit $LASTEXITCODE
