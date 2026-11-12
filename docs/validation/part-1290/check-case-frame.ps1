param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$source = Join-Path $PSScriptRoot 'case_result_probe.erl'
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
[IO.File]::WriteAllBytes($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output), $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = case_result_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
{beam_file,M,_,_,_,Fs} = beam_disasm:file(Generated),
[Is] = [I || {function,run,1,_,I} <- Fs],
true = lists:any(fun({call_ext_last,2,{extfunc,case_result_probe,pack,2},_}) -> true; (_) -> false end, Is),
true = lists:any(fun({{line,[{location,_,5}]},{case_end,{x,0}}}) -> true; (_) -> false end,
    lists:zip(lists:droplast(Is),tl(Is))),
2 = length([ok || {{init_yregs,_},{line,_},{call_ext,2,{extfunc,case_result_probe,pair,2}}} <-
    lists:zip3(lists:sublist(Is,length(Is)-2),lists:sublist(tl(Is),length(Is)-2),tl(tl(Is)))]),
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    {left,marker} = M:run(a),
    {right,marker} = M:run(1),
    5 = try M:run(invalid) catch error:{case_clause,other}:Stack ->
        [Caller] = [Loc || {Module,run,1,Loc} <- Stack, Module =:= M],
        proplists:get_value(line,Caller)
    end,
    true = code:delete(M),
    code:purge(M)
end,
Check(Reference),
Check(Generated),
io:format("Both case arms clear dead frame slots before calling and preserve their results.~n"),
halt().
"@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $Output
exit $LASTEXITCODE













