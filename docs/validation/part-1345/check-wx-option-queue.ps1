param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'wx_option_queue_probe.erl'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
$outputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
[IO.File]::WriteAllBytes($outputPath, $bytes)

$check = @"
[Source, Output, Stub] = init:get_plain_arguments(),
{ok,wxe_util,StubBeam} = compile:file(Stub,[binary]),
{module,wxe_util} = code:load_binary(wxe_util,Stub,StubBeam),
M = wx_option_queue_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    {module,M} = code:load_binary(M,Source,Beam),
    expected = try M:post([]) of _ -> unexpected
        catch error:{wx,unknown_env} -> expected
    end,
    undefined = put(wx_env,{wx_env,token,none,none}),
    queued = M:post([{text,"hello"}]),
    {[{text,<<"hello">>}],token,575} = erase(wx_probe_command),
    {received,576} = M:request([{text,"request"}]),
    {[{text,<<"request">>}],token,576} = erase(wx_probe_command),
    expected = try M:post([bad]) of _ -> unexpected
        catch error:{badoption,bad} -> expected
    end,
    undefined = erase(wx_probe_command),
    {wx_env,token,none,none} = erase(wx_env),
    true = code:delete(M),
    code:purge(M),
    [Instructions] = [Is || {function,post,1,_,Is} <- Fs],
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
    [Rewrite(I) || I <- Instructions]
end,
Expected = Check(Reference),
Actual = Check(Generated),
case Actual of
    Expected -> io:format("~p~n",[Actual]), halt();
    _ -> io:format("OTP ~p~nC# ~p~n",[Expected,Actual]), halt(1)
end.
"@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $outputPath (Join-Path $PSScriptRoot 'wxe_util.erl')
exit $LASTEXITCODE




