param(
    [Parameter(Mandatory)] [string] $OtpRoot,
    [Parameter(Mandatory)] [string] $CorpusToolDirectory,
    [Parameter(Mandatory)] [string] $GeneratedBeam
)

$ErrorActionPreference = 'Stop'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $CorpusToolDirectory).Path 'Erlang.Compiler.dll'))
$source = Join-Path $PSScriptRoot 'computed_atom_tail_probe.erl'
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
[IO.File]::WriteAllBytes($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($GeneratedBeam), $bytes)

$check = @'
[Source, Generated] = init:get_plain_arguments(),
M = computed_atom_tail_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Compiled} = file:read_file(Generated),
{beam_file,M,_,_,_,Functions} = beam_disasm:file(Compiled),
[Run] = [Is || {function,run,3,_,Is} <- Functions],
[pair,same,wrap] = lists:sort([Name || {call_last,_,{_,Name,_},_} <- Run]),
[Keys] = [L || {select_val,{x,0},_,{list,L}} <- Run],
Labels = fun Get([{atom,K},{f,L}|Rest]) -> [{K,L}|Get(Rest)];
             Get([]) -> []
         end,
Label = proplists:get_value(keep,Labels(Keys)),
true = is_integer(Label),
Label = proplists:get_value(retain,Labels(Keys)),
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    local = M:run(#{route => local}, bad, bad),
    {wrapped,{first,[second]}} = M:run(#{route => tuple}, first, second),
    {pair,first,second} = M:run(#{route => pair}, first, second),
    {same,first,second} = M:run(#{route => keep}, first, second),
    {same,first,second} = M:run(#{route => retain}, first, second),
    {invalid,left} = try M:run(#{route => pair}, bad, bad)
                     catch error:LeftReason -> LeftReason end,
    {invalid,right} = try M:run(#{route => pair}, first, bad)
                      catch error:RightReason -> RightReason end,
    {case_clause,other} = try M:run(#{route => other}, bad, bad)
                         catch error:CaseReason -> CaseReason end,
    true = code:delete(M),
    code:purge(M)
end,
Check(Reference),
Check(Compiled),
io:format("Computed tails preserve order and failure precedence. Identical simple tails share a label.~n"),
halt().
'@

$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $GeneratedBeam
exit $LASTEXITCODE
