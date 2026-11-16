param([string] $OtpRoot, [string] $PreviousTool, [string] $Tool, [string] $Output, [switch] $CompileOnly)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot '../../../tests/Erlang.Tests/Resources/Beam/list_entry_fallback_probe.erl'
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
    {[b,a],[]} = M:scan([[a,b],[b,a]], []),
    {[],[[a],[b]]} = M:scan([[b],[a]], []),
    {[],[seed]} = M:scan([], [seed]),
    [] = M:fold([[a,b]],fun([b,a]) -> ok end),
    [] = M:fold([],fun(_) -> ok end),
    function_clause = try M:fold([], fun() -> ok end) catch error:E1 -> E1 end,
    function_clause = try M:fold([], wrong) catch error:E2 -> E2 end,
    function_clause = try M:scan(wrong, []) catch error:E3 -> E3 end,
    true = code:delete(M),
    code:purge(M)
end,
lists:foreach(Check,[Reference,Old,New]),
Read = fun(Beam, Name) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    [Is] = [Instructions || {function,F,_,_,Instructions} <- Fs, F =:= Name],
    Is
end,
First = fun(Is) ->
    [First | _] = lists:dropwhile(fun(I) -> element(1,I) =/= func_info end, [I || I <- Is,is_tuple(I)]),
    {func_info,_,_,_} = First,
    [_,_,Instruction|_] = lists:dropwhile(fun(I) -> I =/= First end, Is),
    Instruction
end,
IsCons = fun({test,is_nonempty_list,_,[{x,0}]}) -> true; (_) -> false end,
IsFunArity = fun({test,is_function2,_,[{x,1},{integer,1}]}) -> true; (_) -> false end,
lists:foreach(fun(Name) ->
    O = Read(Reference,Name), B = Read(Old,Name), A = Read(New,Name),
    io:format("~p entry OTP ~p, previous ~p, current ~p~n",[Name,First(O),First(B),First(A)]),
    true = IsCons(First(O)),
    true = IsCons(First(A)),
    case Name of fold -> true = lists:any(IsFunArity,A); scan -> ok end
end,[scan,fold]),
io:format("Values, function_clause cases, list entry and guarded nil return match.~n"),
halt().
'@

$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $previousPath $outputPath
exit $LASTEXITCODE
