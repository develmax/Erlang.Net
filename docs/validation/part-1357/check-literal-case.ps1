param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'literal_case_probe.erl'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
$outputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
[IO.File]::WriteAllBytes($outputPath, $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = literal_case_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    {module,M} = code:load_binary(M,Source,Beam),
    unicode = M:encoding([]),
    unicode = M:encoding([{encoding,utf8}]),
    try M:encoding([{encoding,other}]) of _ -> error(expected_case_clause)
    catch error:{case_clause,other} -> ok end,
    found = M:numeric(2),
    2 = erlang:erase(last_read),
    3 = M:numeric(3),
    3 = erlang:erase(last_read),
    try M:numeric(1.0) of _ -> error(expected_case_clause)
    catch error:{case_clause,1.0} -> ok end,
    1.0 = erlang:erase(last_read),
    yes = M:single(yes),
    yes = erlang:erase(last_read),
    try M:single(no) of _ -> error(expected_case_clause)
    catch error:{case_clause,no} -> ok end,
    no = erlang:erase(last_read),
    accepted = M:partial(true),
    accepted = erlang:erase(last_report),
    rejected = M:partial(false),
    undefined = erlang:erase(last_report),
    try M:partial(other) of _ -> error(expected_case_clause)
    catch error:{case_clause,other} -> ok end,
    undefined = erlang:erase(last_report),
    one = M:identity(one),
    unknown = M:identity(other),
    ready = M:exhaustive([one,two]),
    failed = M:exhaustive(other),
    ready = M:throwing([]),
    try M:throwing(other) of _ -> error(expected_failed)
    catch error:failed -> ok end,
    true = code:delete(M),
    code:purge(M),
    lists:map(fun({Name,Arity}) ->
        [Instructions] = [Is || {function,F,A,_,Is} <- Fs, F =:= Name, A =:= Arity],
        Labels = [L || {label,L} <- Instructions],
        Map = maps:from_list(lists:zip(Labels,lists:seq(1,length(Labels)))),
        Rewrite = fun Fix({label,L}) -> {label,maps:get(L,Map,L)};
            Fix({f,L}) -> {f,maps:get(L,Map,L)};
            Fix({literal,_}=Literal) -> Literal;
            Fix({make_fun3,Target,Index,_,Dst,Args}) -> {make_fun3,Target,Index,uniq,Fix(Dst),Fix(Args)};
            Fix(Tuple) when is_tuple(Tuple) -> list_to_tuple([Fix(X) || X <- tuple_to_list(Tuple)]);
            Fix([H|T]) -> [Fix(H)|Fix(T)];
            Fix(Value) -> Value
        end,
        {{Name,Arity},[Rewrite(I) || I <- Instructions]}
    end,[{encoding,1},{numeric,1},{single,1},{partial,1},{exhaustive,1},{throwing,1}])
end,
Expected = Check(Reference),
Actual = Check(Generated),
case Actual of
    Expected -> io:format("~p~n",[Actual]), halt();
    _ -> io:format("OTP ~p~nC# ~p~n",[Expected,Actual]), halt(1)
end.
"@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $outputPath
exit $LASTEXITCODE










