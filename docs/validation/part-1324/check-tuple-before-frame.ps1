param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'tuple_before_frame_probe.erl'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
$outputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
[IO.File]::WriteAllBytes($outputPath, $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = tuple_before_frame_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    {module,M} = code:load_binary(M,Source,Beam),
    {{called,a},b} = M:run(tag,{a,b}),
    {{called,a},a} = M:tagged(7,{kind,a}),
    no = M:run(tag,other),
    no = M:run(tag,{a,b,c}),
    no = M:tagged(7,{other,a}),
    {{called,a},b} = M:same(a,{a,b}),
    no = M:same(a,{other,b}),
    CheckFailure = fun(F, Args) ->
        try apply(M,F,Args) of _ -> unexpected
        catch error:function_clause:Stack -> [{M,F,Args,_}|_] = Stack, expected
        end
    end,
    expected = CheckFailure(run,[other,{a,b}]),
    expected = CheckFailure(tagged,[8,{kind,a}]),
    true = code:delete(M),
    code:purge(M),
    [{Name,Is} || {function,Name,2,_,Is} <- Fs]
end,
Lists = [Check(Reference), Check(Generated)],
lists:foreach(fun(Functions) ->
    [{label,Info},_,_,{label,_},{test,is_eq_exact,{f,Info},[{x,0},{atom,tag}]},
     {test,is_tuple,{f,Fallback},[{x,1}]},{test,test_arity,{f,Fallback},[{x,1},2]},
     {allocate,_,_}|_] = Instructions = proplists:get_value(run,Functions),
    [{label,Fallback},{move,{atom,no},{x,0}},return] =
        lists:dropwhile(fun(I) -> I =/= {label,Fallback} end,Instructions),
    io:format("~p~n",[Instructions])
end,Lists),
halt().
"@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $outputPath
exit $LASTEXITCODE





