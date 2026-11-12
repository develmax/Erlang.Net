param([string] $OtpRoot, [string] $PreviousTool, [string] $Tool, [string] $Output, [switch] $CompileOnly)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'tuple_arity_fallback_probe.erl'
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
    a = M:pick(fallback,{left,a}),
    b = M:pick(fallback,{right,unused,b}),
    fallback = M:pick(fallback,{wrong,a}),
    fallback = M:pick(fallback,{left,a,b}),
    fallback = M:pick(fallback,not_a_tuple),
    exact = M:read(key,{pair,key}),
    {key,value} = M:read(key,{triple,key,value}),
    {key,{pair,other}} = M:read(key,{pair,other}),
    {key,{triple,key,3}} = M:read(key,{triple,key,3}),
    {key,not_a_tuple} = M:read(key,not_a_tuple),
    a = M:forward(key,{left,a}),
    true = M:forward(key,[other,key]),
    false = M:forward(key,[other]),
    {left,a} = M:keep(key,{left,a}),
    {right,a,b} = M:keep(key,{right,a,b}),
    key = M:keep(key,{wrong,a}),
    {first,second,a} = M:relay(key,{left,a}),
    {first,second,b} = M:relay(key,{right,a,b}),
    key = M:relay(key,{wrong,a}),
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
        F({literal,_}=Literal) -> Literal;
        F(T) when is_tuple(T) -> list_to_tuple([F(X) || X <- tuple_to_list(T)]);
        F([H|T]) -> [F(H)|F(T)];
        F(V) -> V
    end,
    [Fix(I) || I <- Is]
end,
Entry = fun(Is) ->
    [Instruction, Next | _] = lists:nthtail(4,Is),
    {Instruction,Next}
end,
IsTupleDispatch = fun({{test,is_tuple,_,[{x,1}]},{select_tuple_arity,_,_,_}}) -> true; (_) -> false end,
lists:foreach(fun(Name) ->
    O = Read(Reference,Name), B = Read(Old,Name), A = Read(New,Name),
    io:format("~p entry OTP ~0p, previous ~0p, current ~0p~n",[Name,Entry(O),Entry(B),Entry(A)]),
    io:format("~p normalized instructions match: ~p~n",[Name,O =:= A]),
    true = IsTupleDispatch(Entry(O)),
    true = IsTupleDispatch(Entry(A)),
    case Name of pick -> true = O =:= A; forward -> true = O =:= A; _ -> ok end,
    false = IsTupleDispatch(Entry(B))
end,[pick,read,forward,keep,relay]),
io:format("Values and fallback arguments agree with OTP.~n"),
halt().
'@

$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $previousPath $outputPath
exit $LASTEXITCODE
