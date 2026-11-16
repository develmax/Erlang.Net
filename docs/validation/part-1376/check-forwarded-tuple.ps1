param([string] $OtpRoot, [string] $PreviousTool, [string] $Tool, [string] $Output, [switch] $CompileOnly)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'forwarded_tuple_probe.erl'
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
M = forwarded_tuple_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Old} = file:read_file(Previous),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    {imports,[b,a]} = M:wrap(a,b),
    {[b,a],keep} = M:keep(a,b,keep),
    {rev,[b,a]} = M:external(a,b),
    {[a,b],[b,a]} = M:retain(a,b),
    {tag,[b,a]} = M:nested(a,b),
    true = code:delete(M),
    code:purge(M)
end,
lists:foreach(Check,[Reference,Old,Generated]),
Read = fun(Beam, Name) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    [Is] = [Instructions || {function,F,_,_,Instructions} <- Fs, F =:= Name],
    [I || I <- Is, not is_tuple(I) orelse not lists:member(element(1,I),[label,func_info])]
end,
Targets = [wrap,keep,external,retain,nested],
Sizes = [{N,lists:keyfind(allocate,1,Read(Old,N)),lists:keyfind(allocate,1,Read(Generated,N)),
    lists:keyfind(allocate,1,Read(Reference,N))} || N <- Targets],
io:format("Allocations by function, previous, current and OTP: ~p~n",[Sizes]),
Matches = [N || N <- Targets, Read(Generated,N) =:= Read(Reference,N)],
io:format("Functions matching OTP instructions: ~p~n",[Matches]),
Targets = Matches,
halt().
'@

$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $previousPath $outputPath
exit $LASTEXITCODE


