param([string] $OtpRoot, [string] $PreviousTool, [string] $Tool, [string] $Output, [switch] $CompileOnly)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'clause_entry_probe.erl'
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
M = clause_entry_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Old} = file:read_file(Previous),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    {passed,42} = M:dispatch(param,42),
    {error,{asn1,{undefined_type,wrong}}} =
        try M:dispatch(wrong,ignored) catch exit:R1 -> R1 end,
    {{tag,value},value} = M:retain(ok,value),
    {{unknown,other},other} = M:retain(other,ignored),
    {passed,plain} = M:conditional(plain,plain),
    undefined = get(trace),
    {passed,{probed,computed}} = M:conditional(compute,computed),
    computed = erase(trace),
    {unknown,wrong} = try M:conditional(wrong,ignored) catch exit:R2 -> R2 end,
    [b,a] = M:test_last([a,b],run),
    {bad,wrong} = try M:test_last(ignored,wrong) catch throw:R3 -> R3 end,
    {passed,value} = M:only(one,value),
    {two,value} = M:only(two,value),
    {three,value} = M:only(three,value),
    rejected = try M:only(missing,ignored) catch error:function_clause -> rejected end,
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
    || F <- [dispatch,retain,conditional,test_last,only]],
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
Matched = [F || F <- [dispatch,retain,conditional,test_last],
    ReadInstructions(Generated,F,2) =:= ReadInstructions(Reference,F,2)],
io:format("Functions with matching instructions: ~p~n",[Matched]),
[First|_] = [I || I <- ReadInstructions(Generated,conditional,2),
    is_tuple(I), not lists:member(element(1,I),[label,line,func_info])],
case Rows of
    [{dispatch,P0,0,0},{retain,_,0,0},{conditional,_,0,0},{test_last,_,0,0},{only,_,0,0}]
        when P0 > 0 ->
        case {First,lists:all(fun(F) -> lists:member(F,Matched) end,[conditional,dispatch,test_last])} of
            {{select_val,_,_,_},true} -> halt();
            _ -> halt(1)
        end;
    _ -> halt(1)
end.
'@

$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $previousPath $outputPath
exit $LASTEXITCODE
