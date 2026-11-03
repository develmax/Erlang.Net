using System.Numerics;
using System.Runtime.CompilerServices;
using Erlang;
namespace Erlang.Tests;

internal static class CountersHost421Checks
{
    internal static async Task UnusedInitializer()
    {
        await using var runtime = await CountersHost421Access.Create();
        var actor = new RuntimeNodeFoundationHostActor(runtime);
        try
        {
            _ = await actor.InvokeAsync(async context =>
            {
                object gate = typeof(ProcessRuntime).GetField(CountersHost421Names.RuntimeGate, CountersHost421Names.Members)!.GetValue(runtime)!;
                var initialOwner = typeof(ProcessRuntime).GetField(CountersHost421Names.Owner, CountersHost421Names.Members)!;
                lock (gate) CountersHost421Access.Require(initialOwner.GetValue(runtime) is null);
                await CountersInitializer356Access.KillAndAssert(
                    runtime,
                    () =>
                {
                    var field = typeof(ProcessRuntime).GetField(CountersHost421Names.Owner, CountersHost421Names.Members)!;
                    // Keep the actual shared gate until the surviving actor has
                    // tested admission, before initializer global cleanup runs.
                    bool refused = false;
                    try
                    {
                        _ = typeof(ProcessRuntime).GetProperty(CountersHost421Names.OwnerProperty, CountersHost421Names.Members)!.GetValue(runtime);
                    }
                    catch (System.Reflection.TargetInvocationException error) when (error.InnerException is ObjectDisposedException)
                    {
                        refused = true;
                    }
                    CountersHost421Access.Require(refused && field.GetValue(runtime) is null
                        && !runtime.IsAlive(runtime.InitializationPid) && runtime.CountersStorage421.Closed);
                }
                );

                return true;
            });
        }
        finally
        {
            await runtime.DisposeAsync();
            await actor.DisposeAsync();
        }
    }

    internal static async Task Values()
    {
        await using var runtime = await CountersHost421Access.Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term reference = await CountersHost421Access.Call(
            actor,
            runtime,
            CountersHost421Names.New,
            Term.I(9)
        );
        CountersHost421Access.Require(reference is ResourceReferenceTerm { ResourceKind: ReferenceResourceKind.Counters421 });
        _ = await CountersHost421Access.Call(
            actor,
            runtime,
            CountersHost421Names.Put,
            reference,
            Term.I(9),
            Term.I(long.MaxValue)
        );
        _ = await CountersHost421Access.Call(
            actor,
            runtime,
            CountersHost421Names.Add,
            reference,
            Term.I(9),
            Term.I(1)
        );
        CountersHost421Access.Require((await CountersHost421Access.Call(
            actor,
            runtime,
            CountersHost421Names.Get,
            reference,
            Term.I(9)
        )).Equals(Term.I(long.MinValue)));
        var info = (MapTerm)await CountersHost421Access.Call(
            actor,
            runtime,
            CountersHost421Names.Info,
            reference
        );
        CountersHost421Access.Require(info.TryGet(Term.A(CountersHost421Names.Memory), out var memory)
            && memory is Integer originalBytes
            && originalBytes.Value + System.Runtime.InteropServices.Marshal.OffsetOf(
                typeof(ProcessRuntime).Assembly.GetType(CountersPrefix591Names.PrefixType, true)!,
                CountersPrefix591Names.DestructorField
            ).ToInt64() == runtime.CountersStorage421.OwnedNativeBytes);
        foreach (Term index in new Term[] { Term.I(0), Term.I(-1), Term.I(10), Nil.Value, new Integer(BigInteger.One << 64) })
            await CountersHost421Access.BadArgument(
                actor,
                runtime,
                CountersHost421Names.Get,
                reference,
                index
            );
        foreach (Term value in new Term[] { new Integer((BigInteger)long.MaxValue + 1), new Integer((BigInteger)long.MinValue - 1), Nil.Value })
            await CountersHost421Access.BadArgument(
                actor,
                runtime,
                CountersHost421Names.Put,
                reference,
                Term.I(1),
                value
            );
        await CountersHost421Access.BadArgument(
            actor,
            runtime,
            CountersHost421Names.Add,
            reference,
            Term.I(1),
            new Integer(BigInteger.One << 64)
        );
        await CountersHost421Access.BadArgument(
            actor,
            runtime,
            CountersHost421Names.Get,
            runtime.ReferenceResources.CreateBinary(new byte[] { 1 }),
            Term.I(1)
        );
        Term restored = await actor.InvokeAsync(async context =>
        {
            Term wire = await runtime.Modules.Call(
                context,
                CountersHost421Names.ErLang,
                CountersHost421Names.Encode,
                reference
            );

            return await runtime.Modules.Call(
                context,
                CountersHost421Names.ErLang,
                CountersHost421Names.Decode,
                wire
            );
        });
        CountersHost421Access.Require(restored is ResourceReferenceTerm { ResourceKind: ReferenceResourceKind.Counters421 } && restored.Equals(reference));
        await using var foreign = await CountersHost421Access.Create();
        await using var other = new RuntimeNodeFoundationHostActor(foreign);
        await CountersHost421Access.BadArgument(
            other,
            foreign,
            CountersHost421Names.Get,
            reference,
            Term.I(1)
        );
        CountersHost421Access.Require(foreign.CountersStorage421.Created == 0);
    }

    internal static async Task Concurrency()
    {
        await using var runtime = await CountersHost421Access.Create();
        var actors = Enumerable.Range(0, CountersHost421Names.Workers).Select(_ => new RuntimeNodeFoundationHostActor(runtime)).ToArray();
        try
        {
            Term reference = await CountersHost421Access.Call(
                actors[0],
                runtime,
                CountersHost421Names.New,
                Term.I(9)
            );
            await Task.WhenAll(actors.Select((actor, index) => actor.InvokeAsync(async context =>
            {
                _ = await runtime.Modules.Call(
                    context,
                    CountersHost421Names.ErLang,
                    CountersHost421Names.ProcessFlag,
                    Term.A(CountersHost421Names.Scheduler),
                    Term.I(index % CountersHost421Names.Lanes + 1)
                );
                for (int n = 0; n < CountersHost421Names.Iterations; n++)
                    _ = await runtime.Modules.Call(
                        context,
                        CountersHost421Names.Internal,
                        CountersHost421Names.Add,
                        reference,
                        Term.I(9),
                        Term.I(1)
                    );

                return true;
            })));
            CountersHost421Access.Require((await CountersHost421Access.Call(
                actors[0],
                runtime,
                CountersHost421Names.Get,
                reference,
                Term.I(9)
            ))
                .Equals(Term.I(CountersHost421Names.Workers * CountersHost421Names.Iterations)));
            _ = await CountersHost421Access.Call(
                actors[0],
                runtime,
                CountersHost421Names.Put,
                reference,
                Term.I(9),
                Term.I(-7)
            );
            _ = await CountersHost421Access.Call(
                actors[1],
                runtime,
                CountersHost421Names.Add,
                reference,
                Term.I(9),
                Term.I(4)
            );
            CountersHost421Access.Require((await CountersHost421Access.Call(
                actors[0],
                runtime,
                CountersHost421Names.Get,
                reference,
                Term.I(9)
            )).Equals(Term.I(-3)));
        }
        finally
        {
            await Task.WhenAll(actors.Select(actor => actor.DisposeAsync().AsTask()));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<WeakReference> Drop(ProcessRuntime runtime, RuntimeNodeFoundationHostActor actor)
    {
        Term reference = await CountersHost421Access.Call(
            actor,
            runtime,
            CountersHost421Names.New,
            Term.I(1)
        );

        return new WeakReference(reference);
    }

    internal static async Task Gc()
    {
        await using var runtime = await CountersHost421Access.Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        WeakReference weak = await Drop(runtime, actor);
        _ = await actor.Invoke(_ => true);
        using var stop = new CancellationTokenSource(CountersHost421Names.Deadline);
        while (runtime.CountersStorage421.LiveArrays != 0)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            await Task.Delay(1, stop.Token);
        }
        CountersHost421Access.Require(!weak.IsAlive && runtime.CountersStorage421.OwnedNativeBytes == 0
            && runtime.CountersStorage421.Created == runtime.CountersStorage421.Freed);
    }

    internal static async Task Shutdown()
    {
        for (int route = 0; route < 3; route++)
            foreach (bool invocation in new[] { true, false })
            {
                var runtime = await CountersHost421Access.Create();
                var actor = new RuntimeNodeFoundationHostActor(runtime);
                IDisposable? held = null;
                Task? closing = null;
                try
                {
                    Term reference = await CountersHost421Access.Call(
                        actor,
                        runtime,
                        CountersHost421Names.New,
                        Term.I(1)
                    );
                    held = CountersHost421Access.Hold(runtime, reference, invocation);
                    if (route == 0) closing = runtime.DisposeAsync().AsTask();
                    else if (route == 1) closing = Task.WhenAll(runtime.RequestImmediateHalt(0), runtime.HaltQuiescence);
                    else
                    {
                        _ = await actor.Invoke(context =>
 {
     runtime.Exit(context, runtime.InitializationPid, Term.A(CountersHost421Names.Kill));

     return true;
 });
                        closing = runtime.DisposeAsync().AsTask();
                    }
                    using var stop = new CancellationTokenSource(CountersHost421Names.Deadline);
                    while (!runtime.CountersStorage421.Closed) await Task.Delay(1, stop.Token);
                    // Global invocation and actual array pins have different
                    // actor-retirement dependencies. Both must keep their real
                    // VM resource alive until this held lease is released.
                    var heldReport = runtime.CountersStorage421;
                    CountersHost421Access.Require(!closing.IsCompleted
                        && (invocation ? heldReport.ActiveInvocations > 0
                            : heldReport.LiveArrays > 0 && heldReport.OwnedNativeBytes > 0));
                    held.Dispose();
                    held = null;
                    await closing.WaitAsync(CountersHost421Names.Deadline);
                    await runtime.DisposeAsync();
                    var report = runtime.CountersStorage421;
                    CountersHost421Access.Require(report.Closed && report.ActiveInvocations == 0 && report.LiveArrays == 0
                        && report.OwnedNativeBytes == 0 && report.Created == report.Freed);
                    GC.KeepAlive(reference);
                }
                finally
                {
                    held?.Dispose();
                    if (closing is not null) await closing.WaitAsync(CountersHost421Names.Deadline);
                    await runtime.DisposeAsync();
                    await actor.DisposeAsync();
                }
            }
    }
}
