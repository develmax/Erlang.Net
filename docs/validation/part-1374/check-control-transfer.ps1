param([string] $OtpRoot, [string] $PreviousTool, [string] $Tool, [string] $Output, [switch] $CompileOnly)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'control_transfer_probe.erl'
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
M = control_transfer_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Old} = file:read_file(Previous),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    true = M:check([a,b], [[a],[b]], []),
    true = M:check([a|tail], [[a]], tail),
    false = try M:check([a], [[b]], []) catch throw:Reason1 -> Reason1 end,
    false = try M:check([a], [], []) catch throw:Reason2 -> Reason2 end,
    good = M:unwrap([good|tail], ignored),
    {invalid,{bad,context}} = try M:unwrap(bad, context) catch exit:Reason3 -> Reason3 end,
    good = M:local_throw({wrapped,good}),
    {local,bad} = M:local_throw(bad),
    true = code:delete(M),
    code:purge(M)
end,
lists:foreach(Check,[Reference,Old,Generated]),
Read = fun(Beam, Name) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    [Is] = [Instructions || {function,F,_,_,Instructions} <- Fs, F =:= Name],
    [I || I <- Is, is_tuple(I), not lists:member(element(1,I),[label,line,func_info])]
end,
Targets = [check,unwrap,local_throw],
io:format("First instruction by function, previous, current and OTP: ~p~n",
    [[{Name,hd(Read(Old,Name)),hd(Read(Generated,Name)),hd(Read(Reference,Name))} || Name <- Targets]]),
[{test,is_nonempty_list,_,_}|_] = Read(Generated, check),
[{test,is_nonempty_list,_,_}|_] = Read(Generated, unwrap),
{call_ext_only,1,{extfunc,erlang,throw,1}} = lists:last(Read(Generated, check)),
{call_ext_only,1,{extfunc,erlang,exit,1}} = lists:last(Read(Generated, unwrap)),
halt().
'@

$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $previousPath $outputPath
exit $LASTEXITCODE

