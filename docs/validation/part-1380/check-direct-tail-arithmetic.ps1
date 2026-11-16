param([string] $OtpRoot, [string] $PreviousTool, [string] $Tool, [string] $Output, [switch] $CompileOnly)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'direct_tail_arithmetic_probe.erl'
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
M = direct_tail_arithmetic_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Old} = file:read_file(Previous),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    [0,4,root] = M:count([one,two],2,root),
    [0,other,root] = M:count([],other,root),
    badarith = try M:count([one],bad,root) catch error:E1 -> E1 end,
    function_clause = try M:count([one|bad],0,root) catch error:E2 -> E2 end,
    3 = M:index(hit,[{entry,miss},{entry,hit}],2),
    {hit,{unknown,hit}} = M:index(hit,[{entry,miss}],2),
    {5.5,keep,4.5} = M:forward(keep,4.5),
    2 = M:seeded([one,two]),
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
Targets = [count,index,forward,seed_count],
io:format("Allocated slots, previous, current and OTP: ~p~n",
    [[{N,Frames(Old,N),Frames(Generated,N),Frames(Reference,N)} || N <- Targets]]),
lists:foreach(fun(N) -> [] = Frames(Generated,N) end,Targets),
lists:foreach(fun(N) ->
    [{test,is_nonempty_list,_,_}|_] = Read(Generated,N),
    true = lists:any(fun({call_only,3,{M0,F,3}}) -> M0 =:= M andalso F =:= N; (_) -> false end,Read(Generated,N))
end,[count,index]),
true = lists:any(fun
    ({gc_bif,'+',_,_,[{tr,{x,1},{t_integer,{0,'+inf'}}},_],_}) -> true;
    (_) -> false
end,Read(Generated,seed_count)),
halt().
'@

$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $previousPath $outputPath
exit $LASTEXITCODE


