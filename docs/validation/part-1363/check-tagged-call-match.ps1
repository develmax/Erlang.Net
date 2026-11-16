param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'tagged_call_match_probe.erl'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$compile = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile')
$outputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
[IO.File]::WriteAllBytes($outputPath, $compile.Invoke($null, [object[]]@([string]$source, $null, $null, $null)))

$check = @'
[Source, Output, Stub] = init:get_plain_arguments(),
{ok,wxe_util,StubBeam} = compile:file(Stub,[binary]),
{module,wxe_util} = code:load_binary(wxe_util,Stub,StubBeam),
M = tagged_call_match_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    {module,M} = code:load_binary(M,Source,Beam),
    Parent = {wx_ref,parent,wxWindow,none},
    undefined = put(match_probe_value,{ready,payload}),
    {finished,input} = M:direct(input),
    input = erase(match_probe_finished),
    yes = erase(match_probe_checked),
    {finished,{produced,input}} = M:retained(input),
    input = erase(match_probe_produced),
    {produced,input} = erase(match_probe_finished),
    yes = erase(match_probe_checked),
    [b,a] = M:external([a,b]),
    yes = erase(match_probe_checked),
    payload = M:payload(),
    yes = erase(match_probe_checked),
    undefined = put(wx_env,{wx_env,token,none,none}),
    {received,906} = M:mapped(Parent,[{value,42}]),
    {Parent,[{value,42}],token,906} = erase(wx_probe_command),
    yes = erase(match_probe_checked),
    queued = M:mapped_post(Parent,[{value,7}]),
    {Parent,[{value,7}],token,907} = erase(wx_probe_command),
    yes = erase(match_probe_checked),
    {ready,payload} = put(match_probe_value,{wrong,42}),
    lists:foreach(fun({Bad,Run}) ->
        put(match_probe_value,Bad),
        bad_match = try Run() of _ -> unexpected
            catch error:{badmatch,Bad} -> bad_match end,
        yes = erase(match_probe_checked),
        undefined = erase(match_probe_finished),
        undefined = erase(wx_probe_command)
    end,[{not_tuple,fun() -> M:direct(input) end},
         {{ready},fun() -> M:external([]) end},
         {{wrong,payload},fun M:payload/0},
         {{wrong,42},fun() -> M:retained(other) end},
         {{wrong,42},fun() -> M:mapped_post(Parent,[]) end},
         {{wrong,42},fun() -> M:mapped(Parent,[]) end}]),
    other = erase(match_probe_produced),
    bad_option = try M:mapped(Parent,[invalid]) of _ -> unexpected
        catch error:{badoption,invalid} -> bad_option end,
    undefined = erase(match_probe_checked),
    {wx_env,token,none,none} = erase(wx_env),
    {wrong,42} = put(match_probe_value,{ready,payload}),
    missing_environment = try M:mapped_post(Parent,[]) of _ -> unexpected
        catch error:{wx,unknown_env} -> missing_environment end,
    yes = erase(match_probe_checked),
    undefined = erase(wx_probe_command),
    {ready,payload} = erase(match_probe_value),
    true = code:delete(M),
    code:purge(M),
    [begin
        [Instructions] = [Is || {function,N,A,_,Is} <- Fs, {N,A} =:= {Name,Arity}],
        Labels = [L || {label,L} <- Instructions],
        Map = maps:from_list(lists:zip(Labels,lists:seq(1,length(Labels)))),
        Rewrite = fun Fix({label,L}) -> {label,maps:get(L,Map,L)};
            Fix({f,L}) -> {f,maps:get(L,Map,L)};
            Fix({literal,_}=Literal) -> Literal;
            Fix({make_fun3,F,I,_,Dest,Env}) -> {make_fun3,F,I,uniq,Dest,Env};
            Fix(Tuple) when is_tuple(Tuple) -> list_to_tuple([Fix(X) || X <- tuple_to_list(Tuple)]);
            Fix([H|T]) -> [Fix(H)|Fix(T)];
            Fix(Value) -> Value
        end,
        {{Name,Arity},[Rewrite(I) || I <- Instructions]}
    end || {Name,Arity} <- [{direct,1},{retained,1},{external,1},{payload,0},{mapped,2},{mapped_post,2}]]
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
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $outputPath (Join-Path $PSScriptRoot '../part-1362/wxe_util.erl')
exit $LASTEXITCODE
