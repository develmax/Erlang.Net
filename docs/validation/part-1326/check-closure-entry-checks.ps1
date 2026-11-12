param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'closure_entry_checks_probe.erl'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
$outputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
[IO.File]::WriteAllBytes($outputPath, $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = closure_entry_checks_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    {module,M} = code:load_binary(M,Source,Beam),
    Single = M:make(saved),
    {{called,a},saved} = Single({tag,a}),
    expected = try Single({bad,a}) of _ -> unexpected catch error:function_clause -> expected end,
    Dispatch = M:make_dispatch(saved),
    {called,{a,saved}} = Dispatch({tag,a}),
    no = Dispatch({tag,1}),
    no = Dispatch(bad),
    true = code:delete(M),
    code:purge(M),
    [Is || {function,Name,2,_,Is} <- Fs, atom_to_list(Name) =:= "-make/1-fun-0-"]
end,
lists:foreach(fun(Beam) ->
    [[{label,_},_,_,{label,_},{test,is_tagged_tuple,{f,_},[{x,0},2,{atom,tag}]}|_]=Is] =
        Check(Beam),
    io:format("~p~n",[lists:sublist(Is,5)])
end,[Reference,Generated]),
halt().
"@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $outputPath
exit $LASTEXITCODE
