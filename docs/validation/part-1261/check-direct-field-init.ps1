param(
    [Parameter(Mandatory)] [string] $OtpRoot,
    [Parameter(Mandatory)] [string] $CorpusToolDirectory,
    [Parameter(Mandatory)] [string] $GeneratedBeam
)

$ErrorActionPreference = 'Stop'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $CorpusToolDirectory).Path 'Erlang.Compiler.dll'))
$source = Join-Path $PSScriptRoot 'direct_field_init_probe.erl'
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
[IO.File]::WriteAllBytes($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($GeneratedBeam), $bytes)

$check = @'
[Source, Generated] = init:get_plain_arguments(),
M = direct_field_init_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Compiled} = file:read_file(Generated),
{beam_file,M,_,_,_,Fs} = beam_disasm:file(Compiled),
CheckInstructions = fun(Name) ->
    [Instructions] = [Is || {function,N,1,_,Is} <- Fs,N =:= Name],
    BeforeFrame = lists:takewhile(fun({allocate,_,_}) -> false;
                                     ({allocate_heap,_,_,_}) -> false;
                                     (_) -> true end, Instructions),
    [_] = [Test || Test = {test,is_tagged_tuple,_,_} <- BeforeFrame],
    [_] = [Test || Test = {test,is_tagged_tuple,_,_} <- Instructions]
end,
CheckInstructions(single),
CheckInstructions(shared),
[SingleCode] = [Is || {function,single,1,_,Is} <- Fs],
Prefix = lists:takewhile(fun({get_tuple_element,{x,1},1,{y,_}}) -> false; (_) -> true end, SingleCode),
[Field] = [Slot || {get_tuple_element,{x,1},1,{y,Slot}} <- SingleCode],
false = lists:member({y,Field}, lists:append([Slots || {init_yregs,{list,Slots}} <- Prefix])),
[UnusedCode] = [Is || {function,unused,2,_,Is} <- Fs],
[1] = [Index || {get_tuple_element,{x,2},Index,{y,_}} <- UnusedCode],
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    {value,[tail],1} = M:single([{tag,value},tail]),
    {a,value,1} = M:shared([{tag,a,value},tail]),
    {b,value,0} = M:shared([{tag,b,value}]),
    {value,0} = M:repeat([{tag,value,value}]),
    empty = M:optional([]),
    {value,[],0} = M:optional([{tag,value}]),
    {value,1} = M:unused([{tag,value,ignored},tail],ignored),
    Captured = M:captured([{tag,value,ignored},tail],argument),
    {value,[tail],argument} = Captured(),
    lists:foreach(fun(Input) ->
        function_clause = try M:single(Input) catch error:Reason -> Reason end
    end, [[{other,value}],[{tag,value,extra}],[]]),
    function_clause = try M:shared([{tag,c,value}]) catch error:Shared -> Shared end,
    function_clause = try M:repeat([{tag,value,other}]) catch error:Repeat -> Repeat end,
    true = code:delete(M),
    code:purge(M)
end,
Check(Reference),
Check(Compiled),
io:format("Direct field slot is written without an initial clear. Entry behavior agrees with OTP.~n"),
halt().
'@

$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $GeneratedBeam
exit $LASTEXITCODE
