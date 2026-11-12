param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'tagged_sequence_probe.erl'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$compile = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile')
$outputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
[IO.File]::WriteAllBytes($outputPath, $compile.Invoke($null, [object[]]@([string]$source, $null, $null, $null)))

$check = @'
[Source, Output] = init:get_plain_arguments(),
M = tagged_sequence_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    {module,M} = code:load_binary(M,Source,Beam),
    put(tagged_probe_result,{ok,value}),
    {finished,{step,value},value} = M:advance(input),
    {finished,input,{step,value},value} = M:carry(input),
    {finished,{step,constant},constant} = M:ignored(input),
    {step,value} = M:branch(input,true),
    {kept,input,value} = M:branch(input,false),
    {ok,value} = M:whole(input),
    value = erase(tagged_probe_step),
    input = erase(tagged_probe_checked),
    bad_match = try M:pinned(input) of _ -> unexpected
        catch error:{badmatch,{ok,value}} -> bad_match end,
    input = erase(tagged_probe_checked),
    lists:foreach(fun(Bad) ->
        put(tagged_probe_result,Bad),
        bad_match = try M:advance(input) of _ -> unexpected
            catch error:{badmatch,Bad} -> bad_match end,
        bad_match = try M:branch(input,true) of _ -> unexpected
            catch error:{badmatch,Bad} -> bad_match end,
        bad_match = try M:whole(input) of _ -> unexpected
            catch error:{badmatch,Bad} -> bad_match end,
        undefined = erase(tagged_probe_step),
        input = erase(tagged_probe_checked)
    end,[wrong,{ok},{other,value}]),
    erase(tagged_probe_result),
    true = code:delete(M),
    code:purge(M),
    [begin
        [Instructions] = [Is || {function,N,1,_,Is} <- Fs, N =:= Name],
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
    end || Name <- [advance,carry,ignored]]
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
