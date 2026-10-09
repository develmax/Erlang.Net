using Erlang;

await HelloHybrid.CSharpFeatures.Verify();
await using var runtime = new ProcessRuntime();
Erlang.Generated.ErlangModule_arithmetic.Register(runtime.Modules);
Erlang.Generated.ErlangModule_map_source.Register(runtime.Modules);
var mapCheck = runtime.Spawn(async erlangProcess =>
{
    var value = await runtime.Modules.Call(erlangProcess, "map_source", "run");
    if (!value.Equals(Term.I(42))) throw new InvalidOperationException("Map module generation failed");
    var picked = await runtime.Modules.Call(erlangProcess, "map_source", "pick", new MapTerm([new(Term.A("value"), Term.Tuple(Term.I(21), Term.I(21)))]));
    if (!picked.Equals(Term.I(21))) throw new InvalidOperationException("Map function pattern generation failed");
    var bitValue = await runtime.Modules.Call(erlangProcess, "map_source", "bits");
    if (!bitValue.Equals(new BitString([52,18,177,128],25))) throw new InvalidOperationException("Bit segment module generation failed");
    var guarded = await runtime.Modules.Call(erlangProcess, "map_source", "bit_guard", bitValue);
    if (!guarded.Equals(Term.A("ok"))) throw new InvalidOperationException("Bit guard module generation failed");
    var hybridBits = case <<1:3,2:5>> of <<A:3,B:5>> -> <<A:3,B:5>> end.
    if (!hybridBits.Equals(new BitString([34]))) throw new InvalidOperationException("Hybrid bit construction failed");
    var packed = case <<3,-257:16/little,5:3,2:2>> of P -> P end.
    var unpacked = await runtime.Modules.Call(erlangProcess, "map_source", "unpack", packed);
    if (!unpacked.Equals(Term.Tuple(Term.I(-257),new BitString([160],3),new BitString([128],2)))) throw new InvalidOperationException("Bit pattern module generation failed");
    var hybridMap = case #{1 => int, 1.0 => float, value => 41}#{value := 42} of
        #{1 := int, 1.0 := float, value := X} -> X;
        _ -> no
    end.
    if (!hybridMap.Equals(Term.I(42))) throw new InvalidOperationException("Hybrid map generation failed");
    return Term.A("ok");
});
if (!(await mapCheck.Completion).Equals(Term.A("normal"))) throw new InvalidOperationException("Map integration failed");
var helloPrinted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
var process = runtime.Spawn(async erlangProcess =>
{
    while (true)
    {
        var result = receive
            {hello, Name} when is_list(Name) ->
                io:format("Hello ~s~n", [Name]),
                ok;
            {stop} ->
                stop
        end.
        if (result.Equals(Term.A("stop"))) return Term.A("ok");
        helloPrinted.TrySetResult();
    }
});
runtime.Send(process.Pid, Term.Tuple(Term.A("hello"), Term.String("World")));
await helloPrinted.Task.WaitAsync(TimeSpan.FromSeconds(5));
runtime.Send(process.Pid, Term.Tuple(Term.A("stop")));
var reason = await process.Completion.WaitAsync(TimeSpan.FromSeconds(5));
if (!reason.Equals(Term.A("normal"))) throw new InvalidOperationException(reason.ToString());
var arithmetic = runtime.Spawn(async context =>
{
    var value = await runtime.Modules.Call(context, "arithmetic", "double", Term.I(21));
    if (!value.Equals(Term.I(42))) throw new InvalidOperationException("Erlang module compilation failed");
    var zero = await runtime.Modules.Call(context, "arithmetic", "zero");
    if (!zero.Equals(new FloatTerm(-0.0))) throw new InvalidOperationException("Signed zero changed during compilation");
    var sign = await runtime.Modules.Call(context, "arithmetic", "classify", zero);
    if (!sign.Equals(Term.A("negative"))) throw new InvalidOperationException("Signed-zero pattern emission failed");
    return Term.A("ok");
});
if (! (await arithmetic.Completion).Equals(Term.A("normal"))) throw new InvalidOperationException("Module failed");
