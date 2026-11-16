param([string] $OtpRoot, [string] $PreviousTool, [string] $Tool, [string] $Output, [switch] $CompileOnly)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'bif_tail_probe.erl'
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
    {tag,<<"abc">>,3,suffix} = M:size(tag,[97,<<"bc">>],suffix),
    {tag,<<>>,0,suffix} = M:size(tag,[],suffix),
    {code,flag,3,vendor,[97,<<"bc">>],1} = M:pad(code,flag,vendor,[97,<<"bc">>]),
    {code,flag,4,vendor,<<"abcd">>,0} = M:pad(code,flag,vendor,<<"abcd">>),
    badarg = try M:size(tag, invalid, suffix) catch error:E1 -> E1 end,
    badarg = try M:pad(code,flag,vendor,[256]) catch error:E2 -> E2 end,
    {5,3} = M:split(<<"12345">>),
    {<<"ab">>,16} = M:bits([97,98]),
    undef = try M:other([]) catch error:E3 -> E3 end,
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
Frames = fun(Ops) -> lists:max([0|[N || {allocate,N,_} <- Ops]]) end,
lists:foreach(fun(Name) ->
    O = Read(Reference,Name), B = Read(Old,Name), A = Read(New,Name),
    io:format("~p frame OTP ~p, previous ~p, current ~p. Normalized instructions match: ~p~n",
              [Name,Frames(O),Frames(B),Frames(A),O =:= A]),
    true = Frames(A) =:= Frames(O),
    true = Frames(A) < Frames(B),
    case O =:= A of true -> ok; false -> io:format("OTP ~p~nC# ~p~n",[O,A]) end
end,[size,pad]),
io:format("Values and badarg behavior agree with OTP.~n"),
halt().
'@

$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $previousPath $outputPath
exit $LASTEXITCODE
