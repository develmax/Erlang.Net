param([string] $OtpRoot, [string] $PreviousTool, [string] $Tool, [string] $Output, [switch] $CompileOnly)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'wrapped_operands_probe.erl'
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
M = wrapped_operands_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Old} = file:read_file(Previous),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    put(trace,[]), {a,[{name,b},literal],marker} = M:reuse(a,b), [second,first] = erase(trace),
    put(trace,[]), {a,[{name,b}],b} = M:retain(a,b), [second,first] = erase(trace),
    put(trace,[]), {a,{tag,b,c},[b]} = M:nested(a,b,c), [third,second,first] = erase(trace),
    put(trace,[]), first = try M:reuse(fail,b) catch error:K1 -> K1 end, [first] = erase(trace),
    put(trace,[]), second = try M:nested(a,fail,c) catch error:K2 -> K2 end, [second,first] = erase(trace),
    put(trace,[]), third = try M:nested(a,b,fail) catch error:K3 -> K3 end, [third,second,first] = erase(trace),
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
    || F <- [reuse,retain,nested]],
io:format("Function, previous stack slots, current stack slots, OTP stack slots: ~p~n",[Rows]),
ReadInstructions = fun(Beam, Name, Arity) ->
    {beam_file,M,_,_,_,Functions} = beam_disasm:file(Beam),
    [Instructions] = [Is || {function,F,A,_,Is} <- Functions, F =:= Name, A =:= Arity],
    [I || I <- Instructions, not (is_tuple(I) andalso element(1,I) =:= label)]
end,
Matched = [F || F <- [reuse,retain],
    ReadInstructions(Generated,F,2) =:= ReadInstructions(Reference,F,2)],
io:format("Functions with matching instructions: ~p~n",[Matched]),
case Rows of
    [{reuse,P0,P1,P1},{retain,R0,R1,R1},{nested,N0,N1,N1}]
        when P0 > P1, R0 >= R1, N0 >= N1, length(Matched) =:= 2 -> halt();
    _ -> halt(1)
end.
'@

$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $previousPath $outputPath
exit $LASTEXITCODE
