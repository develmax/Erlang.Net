param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'computed_cons_head_probe.erl'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
$outputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
[IO.File]::WriteAllBytes($outputPath, $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = computed_cons_head_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
Check = fun(Beam) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    {module,M} = code:load_binary(M,Source,Beam),
    [{a,k},{b,k}] = M:carry([a,b],k),
    [{value,a},{value,b}] = M:unary([a,b]),
    [{[b],k,{a,k}},{[],k,{b,k}}] = M:packed([a,b],k),
    undefined = put(M,[]),
    [a,b,c] = M:visit([a,b,c],M),
    [c,b,a] = erase(M),
    undefined = put(M,[]),
    expected = try M:visit([a,stop,c],M) of _ -> unexpected
        catch error:head_failure -> expected
    end,
    [a] = erase(M),
    Large = lists:seq(1,5000),
    ExpectedLarge = [{N,context} || N <- Large],
    ExpectedLarge = M:carry(Large,context),
    expected = try M:carry([a|bad],k) of _ -> unexpected
        catch error:function_clause -> expected
    end,
    true = code:delete(M),
    code:purge(M),
    IsByName = fun(Name) -> [Is || {function,N,_,_,Is} <- Fs, N =:= Name] end,
    Normalize = fun(Instructions) ->
        Labels = [L || {label,L} <- Instructions],
        Map = maps:from_list(lists:zip(Labels,lists:seq(1,length(Labels)))),
        Rewrite = fun Fix({label,L}) -> {label,maps:get(L,Map,L)};
            Fix({f,L}) -> {f,maps:get(L,Map,L)};
            Fix({literal,_}=Literal) -> Literal;
            Fix(Tuple) when is_tuple(Tuple) -> list_to_tuple([Fix(X) || X <- tuple_to_list(Tuple)]);
            Fix([H|T]) -> [Fix(H)|Fix(T)];
            Fix(Value) -> Value
        end,
        [Rewrite(I) || I <- Instructions]
    end,
    [{Name,Normalize(hd(IsByName(Name)))} || Name <- [carry,unary,visit]]
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




