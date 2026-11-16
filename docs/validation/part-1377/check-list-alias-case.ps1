param([string] $OtpRoot, [string] $PreviousTool, [string] $Tool, [string] $Output, [switch] $CompileOnly)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'list_alias_case_probe.erl'
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
M = list_alias_case_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Old} = file:read_file(Previous),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    empty = M:scan([],bad,bad),
    {tag,{tag,mode},mode,7} = M:scan([miss,hit],{tag,mode},{hit,7}),
    empty = M:scan([miss],{tag,mode},{hit,7}),
    function_clause = try M:scan([hit],bad,{hit,7}) catch error:E1 -> E1 end,
    function_clause = try M:scan([hit|bad],{tag,mode},{other,7}) catch error:E2 -> E2 end,
    {found,1} = M:choose([4,1],3),
    empty = M:choose([4,5],3),
    {found,{hit,7},7} = M:from_proper_list({hit,7}),
    function_clause = try M:from_proper_list(bad) catch error:E3 -> E3 end,
    true = code:delete(M),
    code:purge(M)
end,
lists:foreach(Check,[Reference,Old,Generated]),
Read = fun(Beam, Name) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    [Is] = [Instructions || {function,F,_,_,Instructions} <- Fs, F =:= Name],
    [I || I <- Is, not is_tuple(I) orelse not lists:member(element(1,I),[label,line,func_info])]
end,
Targets = [scan,choose,scan_private],
io:format("First instructions by function, previous, current and OTP: ~p~n",
    [[{N,hd(Read(Old,N)),hd(Read(Generated,N)),hd(Read(Reference,N))} || N <- Targets]]),
true = lists:all(fun(N) -> case Read(Generated,N) of
    [{test,is_nonempty_list,_,_}|_] -> true;
    _ -> false
end end,[scan,choose]),
true = lists:any(fun({test,is_nil,_,_}) -> true; (_) -> false end, Read(Generated,scan_private)),
halt().
'@

$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $previousPath $outputPath
exit $LASTEXITCODE


