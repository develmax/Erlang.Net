param([string] $OtpRoot, [string] $PreviousTool, [string] $Tool, [string] $Output, [switch] $CompileOnly)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'tuple_arithmetic_probe.erl'
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
M = tuple_arithmetic_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Old} = file:read_file(Previous),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    {value,13} = M:add({next,{next,value}},10),
    {value,2.5} = M:add(value,1.5),
    {12,value,3} = M:keep_both({next,{next,value}},4),
    {value,3} = M:guarded(value,2),
    function_clause = try M:guarded(value,bad) catch error:E1 -> E1 end,
    badarith = try M:add(value,bad) catch error:E2 -> E2 end,
    {badmatch,bad} = try M:unknown_shape(bad) catch error:E3 -> E3 end,
    {value,1} = M:unknown_shape(value),
    true = code:delete(M),
    code:purge(M)
end,
lists:foreach(Check,[Reference,Old,Generated]),
Read = fun(Beam, Name) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    [Is] = [Instructions || {function,F,_,_,Instructions} <- Fs, F =:= Name],
    [I || I <- Is, not is_tuple(I) orelse not lists:member(element(1,I),[label,func_info])]
end,
[Before,After,Expected] = [Read(Beam,add) || Beam <- [Old,Generated,Reference]],
io:format("add/2 before: ~p~nadd/2 after: ~p~nadd/2 OTP: ~p~n",[Before,After,Expected]),
true = lists:member({allocate,1,2},After),
{_,_,_,_,[_,{tr,{x,1},{t_integer,{1,'+inf'}}}],{x,1}} = lists:keyfind(gc_bif,1,After),
After = Expected,
halt().
'@

$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $previousPath $outputPath
exit $LASTEXITCODE


