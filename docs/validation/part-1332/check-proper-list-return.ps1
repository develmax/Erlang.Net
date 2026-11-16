param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'proper_list_return_probe.erl'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
$outputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
[IO.File]::WriteAllBytes($outputPath, $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = proper_list_return_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    {module,M} = code:load_binary(M,Source,Beam),
    ok = M:start([a,b],ok),
    {finished,ok} = M:start_tail([a,b],ok),
    {finished,ok} = M:start_tail([],ok),
    ok = M:public([a],ok),
    expected = try M:public([a|bad],ok) of _ -> unexpected
        catch error:function_clause:Stack -> [{M,public,[bad,ok],_}|_] = Stack, expected
    end,
    Callback = M:callback(),
    {finished,ok} = Callback([a],ok),
    expected = try Callback([a|bad],ok) of _ -> unexpected
        catch error:function_clause -> expected
    end,
    true = code:delete(M),
    code:purge(M),
    lists:foreach(fun({Name,NilCount}) ->
        [Instructions] = [Is || {function,N,2,_,Is} <- Fs, N =:= Name],
        NilCount = length([I || I={test,is_nil,_,_} <- Instructions]),
        io:format("~p nil tests ~p~n",[Name,NilCount])
    end,[{private_return,0},{private_tail,0},{public,1},{indirect,1}]),
    ok
end,
ok = Check(Reference),
ok = Check(Generated),
halt().
"@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $outputPath
exit $LASTEXITCODE
