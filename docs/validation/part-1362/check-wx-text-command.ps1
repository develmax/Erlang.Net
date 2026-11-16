param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'wx_text_command_probe.erl'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$compile = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile')
$outputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
[IO.File]::WriteAllBytes($outputPath, $compile.Invoke($null, [object[]]@([string]$source, $null, $null, $null)))

$pinDir = Join-Path (Split-Path $outputPath) 'part1362-pinned-environment'
$null = New-Item -ItemType Directory -Force $pinDir
$pinnedSource = Join-Path $pinDir (Split-Path $source -Leaf)
$pinnedOutput = [IO.Path]::ChangeExtension($pinnedSource, '.beam')
$sourceText = Get-Content -LiteralPath $source -Raw
$startOfOverload = $sourceText.IndexOf("`noverload(", [StringComparison]::Ordinal)
[IO.File]::WriteAllText($pinnedSource, $sourceText.Substring(0, $startOfOverload) +
    $sourceText.Substring($startOfOverload).Replace('{wx_env, Ref, _, _} -> Ref;', '{wx_env, Parent, _, _} -> Parent;'))
[IO.File]::WriteAllBytes($pinnedOutput, $compile.Invoke($null, [object[]]@([string]$pinnedSource, $null, $null, $null)))

$check = @'
[Source, Output, Stub, PinnedSource, PinnedOutput] = init:get_plain_arguments(),
{ok,wxe_util,StubBeam} = compile:file(Stub,[binary]),
{module,wxe_util} = code:load_binary(wxe_util,Stub,StubBeam),
M = wx_text_command_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    {module,M} = code:load_binary(M,Source,Beam),
    missing_environment = try M:two("a","b",[]) of _ -> unexpected
        catch error:{wx,unknown_env} -> missing_environment end,
    undefined = put(wx_env,{wx_env,token,none,none}),
    {received,900} = M:one("a",[{size,2}]),
    {<<"a">>,[{size,2}],token,900} = erase(wx_probe_command),
    {received,901} = M:two("a",<<"b">>,[{size,3}]),
    {<<"a">>,<<"b">>,[{size,3}],token,901} = erase(wx_probe_command),
    queued = M:set("name",12),
    {<<"name">>,12,token,902} = erase(wx_probe_command),
    queued = M:set(<<"name">>,"value"),
    {<<"name">>,<<"value">>,token,903} = erase(wx_probe_command),
    Parent = {wx_ref,parent,wxWindow,none},
    Data = {wx_ref,data,wxPrintData,none},
    Options = [{data,{wx_ref,option,wxPrintDialogData,none}}],
    {received,904} = M:overload(Parent,Options),
    {Parent,Options,token,904} = erase(wx_probe_command),
    {received,905} = M:overload(Parent,Data),
    {Parent,Data,token,905} = erase(wx_probe_command),
    bad_option = try M:two("a","b",[bad]) of _ -> unexpected
        catch error:{badoption,bad} -> bad_option end,
    bad_guard = try M:two("a",invalid,[bad]) of _ -> unexpected
        catch error:function_clause -> bad_guard end,
    bad_record = try M:overload(Parent,{wrong,data,wxPrintData,none}) of _ -> unexpected
        catch error:function_clause -> bad_record end,
    undefined = erase(wx_probe_command),
    {wx_env,token,none,none} = erase(wx_env),
    true = code:delete(M),
    code:purge(M),
    lists:map(fun({Name,Arity}) ->
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
    end,[{one,2},{two,3},{set,2},{overload,2}])
end,
Expected = Check(Reference),
Actual = Check(Generated),
{ok,M,PinnedReference} = compile:file(PinnedSource,[binary,deterministic]),
{ok,PinnedGenerated} = file:read_file(PinnedOutput),
lists:foreach(fun(Beam) ->
    {module,M} = code:load_binary(M,PinnedSource,Beam),
    Parent = {wx_ref,parent,wxWindow,none},
    Data = {wx_ref,data,wxPrintData,none},
    undefined = put(wx_env,{wx_env,token,none,none}),
    lists:foreach(fun(Value) ->
        missing_environment = try M:overload(Parent,Value) of _ -> unexpected
            catch error:{wx,unknown_env} -> missing_environment end
    end,[[],Data]),
    {wx_env,token,none,none} = put(wx_env,{wx_env,Parent,none,none}),
    {received,904} = M:overload(Parent,[]),
    {Parent,[],Parent,904} = erase(wx_probe_command),
    {received,905} = M:overload(Parent,Data),
    {Parent,Data,Parent,905} = erase(wx_probe_command),
    {wx_env,Parent,none,none} = erase(wx_env),
    true = code:delete(M),
    code:purge(M)
end,[PinnedReference,PinnedGenerated]),
Different = [Key || {Key,Instructions} <- Actual, Instructions =/= proplists:get_value(Key,Expected)],
case Actual of
    Expected -> io:format("~p~n",[Actual]), halt();
    _ -> io:format("OTP ~p~nC# ~p~nDiffering functions ~p~n",[Expected,Actual,Different]), halt(1)
end.
'@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $outputPath (Join-Path $PSScriptRoot 'wxe_util.erl') $pinnedSource $pinnedOutput
exit $LASTEXITCODE
