param([string] $OtpRoot, [string] $Tool, [string] $Output)

$ErrorActionPreference = 'Stop'
$compiler = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$source = Join-Path $PSScriptRoot 'receive_forward_probe.erl'
$bytes = $compiler.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile').Invoke(
    $null, [object[]]@([string]$source, $null, $null, $null)
)
[IO.File]::WriteAllBytes($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output), $bytes)

$check = @"
[Source, Output] = init:get_plain_arguments(),
M = receive_forward_probe,
{ok,M,Reference} = compile:file(Source,[binary,deterministic]),
{ok,Generated} = file:read_file(Output),
{beam_file,M,_,_,_,Fs} = beam_disasm:file(Generated),
[Is] = [I || {function,run,0,_,I} <- Fs],
CallsAndMoves = [I || I <- Is, element(1, case is_tuple(I) of true -> I; false -> {I} end) =:= call_ext
    orelse (is_tuple(I) andalso element(1,I) =:= move)],
[_,{call_ext,1,{extfunc,M,second,1}}|_] = lists:dropwhile(
    fun(I) -> I =/= {call_ext,0,{extfunc,M,first,0}} end, CallsAndMoves),
Check = fun(Beam) ->
    {module,M} = code:load_binary(M,Source,Beam),
    self() ! {run},
    done = M:run(),
    [first,second,[{message,{wrapped,value}}]] =
        [receive Message -> Message after 0 -> missing end || _ <- [1,2,3]],
    self() ! {retained},
    value = M:retained(),
    [first,second] = [receive Event -> Event after 0 -> missing end || _ <- [1,2]],
    true = code:delete(M),
    code:purge(M)
end,
Check(Reference),
Check(Generated),
io:format("Receive call forwarding preserves call order and retained bindings.~n"),
halt().
"@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $source $Output
exit $LASTEXITCODE





