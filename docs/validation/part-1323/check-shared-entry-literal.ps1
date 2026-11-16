param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'shared_entry_literal_probe.erl'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
$outputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
[IO.File]::WriteAllBytes($outputPath, $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = shared_entry_literal_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    {module,M} = code:load_binary(M,Source,Beam),
    {{called,a},b} = M:run(tag,{a,b}),
    {{called,a},b} = M:run_integer(7,{a,b}),
    no = M:run(tag,other),
    no = M:run_integer(7,other),
    CheckFailure = fun(F, Args) ->
        try apply(M,F,Args) of _ -> unexpected
        catch error:function_clause:Stack -> [{M,F,Args,_}|_] = Stack, expected
        end
    end,
    expected = CheckFailure(run,[other,{a,b}]),
    expected = CheckFailure(run_integer,[8,{a,b}]),
    true = code:delete(M),
    code:purge(M),
    [{Name,Is} || {function,Name,2,_,Is} <- Fs]
end,
Lists = [Check(Reference), Check(Generated)],
lists:foreach(fun(Functions) ->
    lists:foreach(fun({Name, Expected}) ->
        Instructions = proplists:get_value(Name,Functions),
        [{label,Info},_,_,{label,_},
         {test,is_eq_exact,{f,Info},[{x,0},Expected]}|_] = Instructions,
        io:format("~p ~p~n",[Name,lists:sublist(Instructions,5)])
    end,[{run,{atom,tag}},{run_integer,{integer,7}}])
end,Lists),
halt().
"@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $outputPath
exit $LASTEXITCODE




