param([string] $OtpRoot, [string] $PreviousTool, [string] $Tool, [string] $Output, [switch] $CompileOnly)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'forwarded_tail_probe.erl'
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
[Source, OldPath, NewPath] = init:get_plain_arguments(),
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Old} = file:read_file(OldPath),
{ok,New} = file:read_file(NewPath),
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    {[a],left,right} = M:forward(#{key=>a},left,right),
    {{captured,state},captured} = M:captured(captured,state),
    {[{fixed,b},{fixed,a}],5} = M:fixed([a,b],3),
    {[{one,b},{one,a}],5} = M:variable([a,b],one,3),
    {[{two,b},{two,a}],5} = M:variable([a,b],two,3),
    {[{fixed,b},{fixed,a}],5} = M:variable_fixed([a,b],3),
    {[{pattern,b},{pattern,a}],5} = M:pattern_fixed({wrapped,pattern},[a,b],3),
    true = code:delete(M),
    code:purge(M)
end,
lists:foreach(Check,[Reference,Old,New]),
Read = fun(Beam, Name) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    [Is] = [Instructions || {function,F,_,_,Instructions} <- Fs, F =:= Name],
    Labels = [L || {label,L} <- Is],
    LabelMap = maps:from_list(lists:zip(Labels,lists:seq(1,length(Labels)))),
    Fix = fun F({label,L}) -> {label,maps:get(L,LabelMap,L)};
        F({f,L}) -> {f,maps:get(L,LabelMap,L)};
        F({make_fun3,_,_,_,Dst,Args}) -> {make_fun3,F(Dst),F(Args)};
        F({literal,_}=Literal) -> Literal;
        F(T) when is_tuple(T) -> list_to_tuple([F(X) || X <- tuple_to_list(T)]);
        F([H|T]) -> [F(H)|F(T)];
        F(V) -> V
    end,
    [Fix(I) || I <- Is]
end,
Frames = fun(Ops) -> lists:max([0|[N || {allocate,N,_} <- Ops]]) end,
lists:foreach(fun(Name) ->
    O = Read(Reference,Name), B = Read(Old,Name), A = Read(New,Name),
    io:format("~p frame OTP ~p, previous ~p, current ~p. Normalized instructions match: ~p~n",
              [Name,Frames(O),Frames(B),Frames(A),O =:= A]),
    true = Frames(A) =:= Frames(O),
    true = Frames(A) < Frames(B)
end,[forward,captured,worker,worker_pattern]),
io:format("Values, X0 captures and differing public inputs agree with OTP.~n"),
halt().
'@

$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $previousPath $outputPath
exit $LASTEXITCODE
