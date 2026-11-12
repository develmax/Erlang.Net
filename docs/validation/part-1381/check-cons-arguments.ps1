param([string] $OtpRoot, [string] $PreviousTool, [string] $Tool, [string] $Output, [switch] $CompileOnly)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'cons_argument_probe.erl'
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
M = cons_argument_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Old} = file:read_file(Previous),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    {retry,[a,1],[]} = M:collect([a,1],keep,[]),
    {retry,[a,2],[{number,1}]} = M:collect([1,a,2],keep,[]),
    {done,[{number,1},{number,2}]} = M:collect([1,a,2],skip,[]),
    {done,[]} = M:collect([],keep,[]),
    {done,[{seen,[a,b]},{seen,[b]}]} = M:accumulate([a,b],[]),
    function_clause = try M:collect([1|bad],skip,[]) catch error:E1 -> E1 end,
    function_clause = try M:accumulate([a,1],[]) catch error:E2 -> E2 end,
    7 = M:field(match,[{entry,match}],7),
    other = M:field(match,[{entry,other}],7),
    {matched,a,[b]} = M:alias_match([a,b],[a,b]),
    different = M:alias_match([a],[b]),
    3 = M:find_index(match,[{entry,other},{entry,match}],2),
    missing = M:find_index(match,[{entry,other}],2),
    [{b,value},{a,value}] = M:plain([a,b],value,[]),
    true = code:delete(M),
    code:purge(M)
end,
lists:foreach(Check,[Reference,Old,Generated]),
Read = fun(Beam, Name) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    [Is] = [Instructions || {function,F,_,_,Instructions} <- Fs, F =:= Name],
    [I || I <- Is, not is_tuple(I) orelse not lists:member(element(1,I),[label,line,func_info])]
end,
Targets = [collect,accumulate],
io:format("First instructions, previous, current and OTP: ~p~n",
    [[{N,hd(Read(Old,N)),hd(Read(Generated,N)),hd(Read(Reference,N))} || N <- Targets]]),
lists:foreach(fun(N) ->
    [{test,is_nonempty_list,_,_}|_] = Read(Generated,N),
    true = lists:any(fun({call_only,_,{M0,F,_}}) -> M0 =:= M andalso F =:= N; (_) -> false end,Read(Generated,N)),
    true = lists:any(fun({put_list,_,_,_}) -> true; (_) -> false end,Read(Generated,N))
end,Targets),
true = lists:any(fun
    ({get_tuple_element,{x,R},1,{x,R}}) -> true;
    (_) -> false
end,Read(Generated,find_index)),
[{test,is_nonempty_list,_,_},{test_heap,5,3},{get_list,_,_,_}|_] = Read(Generated,plain),
halt().
'@

$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $previousPath $outputPath
exit $LASTEXITCODE


