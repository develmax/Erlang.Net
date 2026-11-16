param([string] $OtpRoot, [string] $PreviousTool, [string] $Tool, [string] $Output, [switch] $CompileOnly)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'list_case_arithmetic_probe.erl'
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
M = list_case_arithmetic_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Old} = file:read_file(Previous),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    [97,92,125,98] = M:encode([97,125,98],unused,0),
    [] = M:encode([],unused,bad),
    badarith = try M:encode([125],unused,bad) catch error:E1 -> E1 end,
    badarith = try M:encode([97],unused,bad) catch error:E2 -> E2 end,
    2 = M:select([miss,miss,hit],hit,0),
    2.5 = M:select([miss,hit],hit,1.5),
    keep = M:select([hit],hit,keep),
    [8,9] = M:swapped([first,other],4,8),
    8.5 = M:add_heads([1,2,stop,100],5.5),
    badarith = try M:add_heads([bad],0) catch error:E3 -> E3 end,
    true = code:delete(M),
    code:purge(M)
end,
lists:foreach(Check,[Reference,Old,Generated]),
Read = fun(Beam, Name) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    [Is] = [Instructions || {function,F,_,_,Instructions} <- Fs, F =:= Name],
    [I || I <- Is, not is_tuple(I) orelse not lists:member(element(1,I),[label,line,func_info])]
end,
Frames = fun(Beam, Name) -> [N || {allocate,N,_} <- Read(Beam,Name)] end,
Targets = [encode,select,swapped,add_heads],
io:format("Allocated slots by arm, previous, current and OTP: ~p~n",
    [[{N,Frames(Old,N),Frames(Generated,N),Frames(Reference,N)} || N <- Targets]]),
[0,1] = Frames(Generated,encode),
[] = Frames(Generated,select),
[1,1] = Frames(Generated,swapped),
[] = Frames(Generated,add_heads),
Select = Read(Generated,select),
true = lists:any(fun({call_only,3,{M0,select,3}}) -> M0 =:= M; (_) -> false end,Select),
Encode = Read(Generated,encode),
{BeforeAllocation,[{allocate,0,_}|_]} =
    lists:splitwith(fun(I) -> not is_tuple(I) orelse element(1,I) =/= allocate end,Encode),
true = lists:any(fun({gc_bif,'+',_,_,_,_}) -> true; (_) -> false end,BeforeAllocation),
halt().
'@

$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $previousPath $outputPath
exit $LASTEXITCODE


