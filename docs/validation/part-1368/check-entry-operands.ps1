param([string] $OtpRoot, [string] $PreviousTool, [string] $Tool, [string] $Output, [switch] $CompileOnly)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'entry_operand_probe.erl'
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
M = entry_operand_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Old} = file:read_file(Previous),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    put(trace,[]), {a,{tag,b}} = M:pair(a,b), [second,first] = erase(trace),
    put(trace,[]), {marker,a,{tag,b}} = M:with_literal(a,b), [second,first] = erase(trace),
    put(trace,[]), [a,b] = M:remote_tail([a],b), [second,first] = erase(trace),
    put(trace,[]), {keep,a,{tag,b,keep}} = M:retain(a,b,keep), [second,first] = erase(trace),
    put(trace,[]), first = try M:pair(fail,b) catch error:K1 -> K1 end, [first] = erase(trace),
    put(trace,[]), second = try M:pair(a,fail) catch error:K2 -> K2 end, [second,first] = erase(trace),
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
    || F <- [pair,with_literal,remote_tail,retain]],
io:format("Function, previous stack slots, current stack slots, OTP stack slots: ~p~n",[Rows]),
case Rows of
    [{pair,P0,P1,_},{with_literal,T0,T1,_},{remote_tail,B0,B1,_},{retain,C0,C1,_}]
        when P0 > P1, T0 > T1, B0 > B1, C0 > C1 -> halt();
    _ -> halt(1)
end.
'@

$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $previousPath $outputPath
exit $LASTEXITCODE
