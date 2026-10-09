using Erlang;

await HelloHybrid.CSharpFeatures.Verify();
await using var runtime = new ProcessRuntime();
Erlang.Generated.ErlangModule_arithmetic.Register(runtime.Modules);
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
