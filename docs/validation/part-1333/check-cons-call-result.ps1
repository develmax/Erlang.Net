param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'cons_call_result_probe.erl'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
$outputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
[IO.File]::WriteAllBytes($outputPath, $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = cons_call_result_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    {module,M} = code:load_binary(M,Source,Beam),
    [b,a] = M:run([a,b]),
    [a,b] = M:public([a,b]),
    [item,item] = M:literal_head([a,b]),
    [] = M:run([]),
    expected = try M:public([a|bad]) of _ -> unexpected
        catch error:function_clause -> expected
    end,
    true = code:delete(M),
    code:purge(M),
    lists:foreach(fun({Name,Frame,NilCount}) ->
        [Instructions] = [Is || {function,N,1,_,Is} <- Fs, N =:= Name],
        [Frame] = [Slots || {allocate,Slots,_} <- Instructions],
        NilCount = length([I || I={test,is_nil,_,_} <- Instructions]),
        [{put_list,_,{x,0},{x,0}}] = [I || I={put_list,_,_,_} <- Instructions],
        io:format("~p frame slots ~p nil tests ~p~n",[Name,Frame,NilCount])
    end,[{rebuild,1,0},{public,1,1},{literal_head,0,1}]),
    ok
end,
ok = Check(Reference),
ok = Check(Generated),
halt().
"@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $outputPath
exit $LASTEXITCODE
