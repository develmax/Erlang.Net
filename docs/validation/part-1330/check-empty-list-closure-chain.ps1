param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'shared_cons_lookahead_probe.erl'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
$outputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
[IO.File]::WriteAllBytes($outputPath, $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = shared_cons_lookahead_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    {module,M} = code:load_binary(M,Source,Beam),
    [y,z] = M:run([x,y,z,drop],[]),
    [x,z,y] = M:run([x,z,y],[]),
    [x] = M:run([x],[]),
    [] = M:run([],[]),
    [z] = M:captured([],[a,a,z]),
    expected = try M:run([x,y|bad],[]) of _ -> unexpected
        catch error:function_clause:Stack -> [{M,run,[bad,[y]],_}|_] = Stack, expected
    end,
    true = code:delete(M),
    code:purge(M),
    [Is] = [Instructions || {function,run,2,_,Instructions} <- Fs],
    [{label,_},_,_,{label,_},{test,is_nonempty_list,{f,Fallback},[{x,0}]}|_] = Is,
    [{label,Fallback},
     {test,is_nil,_,[{x,0}]},
     {allocate_heap,0,{alloc,[{words,0},{floats,0},{funs,1}]},2},
     {make_fun3,_,_,_,{x,0},{list,[]}},
     {line,_},
     {call_ext,2,{extfunc,lists,dropwhile,2}},
     {call_ext_last,1,{extfunc,lists,reverse,1},0}] =
        lists:dropwhile(fun(I) -> I =/= {label,Fallback} end,Is),
    Prefix = lists:takewhile(fun(I) -> I =/= {label,Fallback} end,lists:nthtail(4,Is)),
    Labels = [L || {label,L} <- Prefix],
    Map = maps:from_list([{Fallback,fallback}|[{L,branch}||L<-Labels]]),
    [case I of
        {label,L} -> {label,maps:get(L,Map)};
        {test,Name,{f,L},Args} -> {test,Name,{f,maps:get(L,Map)},Args};
        _ -> I
    end || I<-Prefix]
end,
Prefix = Check(Reference),
Prefix = Check(Generated),
io:format("~p~n",[Prefix]),
halt().
"@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $outputPath
exit $LASTEXITCODE
