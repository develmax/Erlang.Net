param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'private_case_constant_probe.erl'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$compile = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile')
$outputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
[IO.File]::WriteAllBytes($outputPath, $compile.Invoke($null, [object[]]@([string]$source, $null, $null, $null)))

$disabled = Join-Path (Split-Path $outputPath) 'private_case_constant_probe.erl'
[IO.File]::WriteAllText($disabled, (Get-Content -LiteralPath $source -Raw).Replace(
    '-module(private_case_constant_probe).', "-module(private_case_constant_probe).`n-compile([no_module_opt])."))
$disabledOutput = [IO.Path]::ChangeExtension($disabled, '.disabled.beam')
[IO.File]::WriteAllBytes($disabledOutput, $compile.Invoke($null, [object[]]@([string]$disabled, $null, $null, $null)))

$check = @'
[Source, Output, DisabledOutput] = init:get_plain_arguments(),
M = private_case_constant_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
{ok,Disabled} = file:read_file(DisabledOutput),
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    Items = [{state,first},{other,second}],
    {{state,first},[{other,second}]} = M:run(Items,missing),
    {missing,[]} = M:run_again([],missing),
    {{state,first},[{other,second}]} = M:alternate(state,Items,missing),
    {{other,second},[{state,first}]} = M:alternate(other,Items,missing),
    Handler = M:get_handler(),
    {{other,second},[{state,first}]} = Handler(second,2,Items,missing),
    true = code:delete(M),
    code:purge(M)
end,
lists:foreach(Check,[Reference,Generated,Disabled]),
Read = fun(Beam,Name,Arity) ->
    {beam_file,M,_,_,_,Fs} = beam_disasm:file(Beam),
    [Is] = [Instructions || {function,F,A,_,Instructions} <- Fs, F =:= Name, A =:= Arity],
    Labels = [L || {label,L} <- Is],
    Map = maps:from_list(lists:zip(Labels,lists:seq(1,length(Labels)))),
    Fix = fun Rewrite({label,L}) -> {label,maps:get(L,Map,L)};
        Rewrite({f,L}) -> {f,maps:get(L,Map,L)};
        Rewrite({literal,_}=Literal) -> Literal;
        Rewrite(T) when is_tuple(T) -> list_to_tuple([Rewrite(X) || X <- tuple_to_list(T)]);
        Rewrite([H|T]) -> [Rewrite(H)|Rewrite(T)];
        Rewrite(Value) -> Value
    end,
    [Fix(I) || I <- Is]
end,
Expected = Read(Reference,take_entry,4),
Actual = Read(Generated,take_entry,4),
Peak = fun(Is) -> lists:max([0|[Size || {allocate,Size,_} <- Is]]) end,
3 = Peak(Actual),
4 = Peak(Read(Generated,pick,4)),
5 = Peak(Read(Generated,escaped,4)),
5 = Peak(Read(Disabled,take_entry,4)),
case Actual of
    Expected -> io:format("~p~n",[Actual]), halt();
    _ -> io:format("OTP ~p~nC# ~p~n",[Expected,Actual]), halt(1)
end.
'@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $outputPath $disabledOutput
exit $LASTEXITCODE
