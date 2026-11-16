param([string] $OtpRoot, [string] $PreviousTool, [string] $Tool, [string] $Output, [switch] $CompileOnly)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'list_register_case_probe.erl'
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
M = list_register_case_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Old} = file:read_file(Previous),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    empty = M:select([],hit,keep),
    {found,keep,7} = M:select([{entry,miss,0},{entry,hit,7}],hit,keep),
    empty = M:select([{entry,miss,0}],hit,keep),
    {badrecord,wrong} = try M:select([wrong],hit,keep) catch error:E1 -> E1 end,
    function_clause = try M:select([{entry,miss,0}|wrong],hit,keep) catch error:E2 -> E2 end,
    {yes,hit} = M:check([miss,hit],hit),
    empty = M:check([miss],hit),
    true = code:delete(M),
    code:purge(M)
end,
lists:foreach(Check,[Reference,Old,Generated]),
RecordFailureLocation = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    Location = try M:select([wrong],hit,keep)
        catch error:{badrecord,wrong}:Stack ->
            [{M,select,3,Where}|_] = Stack,
            proplists:get_value(line,Where)
        end,
    true = code:delete(M),
    code:purge(M),
    Location
end,
ReferenceLocation = RecordFailureLocation(Reference),
true = is_integer(ReferenceLocation),
ReferenceLocation = RecordFailureLocation(Generated),
Read = fun(Beam, Name) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    [Is] = [Instructions || {function,F,_,_,Instructions} <- Fs, F =:= Name],
    [I || I <- Is, not is_tuple(I) orelse not lists:member(element(1,I),[label,line,func_info])]
end,
Summarize = fun(Beam, Name) ->
    Is = Read(Beam,Name),
    First = hd(Is),
    Slots = [N || {allocate,N,_} <- Is],
    {First,Slots}
end,
Targets = [select,check],
io:format("First instruction and allocated slots, previous, current and OTP: ~p~n",
    [[{N,Summarize(Old,N),Summarize(Generated,N),Summarize(Reference,N)} || N <- Targets]]),
{{test,is_nonempty_list,_,_},[1]} = Summarize(Generated,select),
{{test,is_nonempty_list,_,_},[]} = Summarize(Generated,check),
SelectOps = Read(Generated,select),
{BeforeAllocation,[{allocate,1,_}|_]} =
    lists:splitwith(fun(I) -> not is_tuple(I) orelse element(1,I) =/= allocate end,SelectOps),
true = lists:any(fun({test,is_tagged_tuple,_,_}) -> true; (_) -> false end,BeforeAllocation),
true = lists:any(fun({test,is_eq_exact,_,_}) -> true; (_) -> false end,BeforeAllocation),
halt().
'@

$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $previousPath $outputPath
exit $LASTEXITCODE


