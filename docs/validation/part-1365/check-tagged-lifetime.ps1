param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'tagged_lifetime_probe.erl'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$compile = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile')
$outputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
[IO.File]::WriteAllBytes($outputPath, $compile.Invoke($null, [object[]]@([string]$source, $null, $null, $null)))

$check = @'
[Source, Output] = init:get_plain_arguments(),
M = tagged_lifetime_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    {module,M} = code:load_binary(M,Source,Beam),
    put(tagged_lifetime_value,{ok,payload}),
    {{step,first},payload,first} = M:delay(input),
    {{step,payload},payload,payload} = M:repeat(input),
    {{step,payload},payload,payload} = M:late_repeat(input),
    {input,{step,first},payload,first} = M:retain_input(input),
    {payload,payload,{payload,carry}} = M:trim(input,carry),
    input = erase(tagged_lifetime_checked),
    {payload,carry} = erase(tagged_lifetime_step),
    lists:foreach(fun(Bad) ->
        put(tagged_lifetime_value,Bad),
        bad_match = try M:delay(input) of _ -> unexpected
            catch error:{badmatch,Bad} -> bad_match end,
        bad_match = try M:repeat(input) of _ -> unexpected
            catch error:{badmatch,Bad} -> bad_match end,
        undefined = erase(tagged_lifetime_step),
        input = erase(tagged_lifetime_checked)
    end,[wrong,{ok},{other,payload}]),
    erase(tagged_lifetime_value),
    true = code:delete(M),
    code:purge(M),
    [begin
        [Instructions] = [Is || {function,N,A,_,Is} <- Fs, {N,A} =:= {Name,Arity}],
        Labels = [L || {label,L} <- Instructions],
        Map = maps:from_list(lists:zip(Labels,lists:seq(1,length(Labels)))),
        Rewrite = fun Fix({label,L}) -> {label,maps:get(L,Map,L)};
            Fix({f,L}) -> {f,maps:get(L,Map,L)};
            Fix({literal,_}=Literal) -> Literal;
            Fix(Tuple) when is_tuple(Tuple) -> list_to_tuple([Fix(X) || X <- tuple_to_list(Tuple)]);
            Fix([H|T]) -> [Fix(H)|Fix(T)];
            Fix(Value) -> Value
        end,
        {Name,[Rewrite(I) || I <- Instructions]}
    end || {Name,Arity} <- [{delay,1},{repeat,1},{late_repeat,1},{retain_input,1},{trim,2}]]
end,
Expected = Check(Reference),
Actual = Check(Generated),
Different = [{Name,proplists:get_value(Name,Expected),Is}
    || {Name,Is} <- Actual, Is =/= proplists:get_value(Name,Expected)],
case Different of
    [] -> io:format("~p~n",[Actual]), halt();
    _ -> io:format("~p~n",[Different]), halt(1)
end.
'@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $outputPath
exit $LASTEXITCODE
