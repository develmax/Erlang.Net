param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'proper_list_entry_probe.erl'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
$outputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
[IO.File]::WriteAllBytes($outputPath, $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = proper_list_entry_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    {module,M} = code:load_binary(M,Source,Beam),
    [y,z] = M:run([x,y,z,drop],[]),
    [x,z,y] = M:run([x,z,y],[]),
    [x] = M:run([x],[]),
    [] = M:run([],[]),
    [y,z] = M:start([drop,z,y,x]),
    Callback = M:callback(),
    [y,z] = Callback([x,y,z,drop],[]),
    expected = try Callback([x,y|bad],[]) of _ -> unexpected
        catch error:function_clause -> expected
    end,
    expected = try M:run([x,y|bad],[]) of _ -> unexpected
        catch error:function_clause:Stack -> [{M,run,[bad,[y]],_}|_] = Stack, expected
    end,
    true = code:delete(M),
    code:purge(M),
    lists:foreach(fun({Name,NilCount}) ->
        [Instructions] = [Is || {function,N,2,_,Is} <- Fs, N =:= Name],
        NilCount = length([I || I={test,is_nil,_,_} <- Instructions]),
        io:format("~p nil tests ~p~n",[Name,NilCount])
    end,[{run,1},{inner,0},{indirect,1}]),
    ok
end,
ok = Check(Reference),
ok = Check(Generated),
halt().
"@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $outputPath
exit $LASTEXITCODE
