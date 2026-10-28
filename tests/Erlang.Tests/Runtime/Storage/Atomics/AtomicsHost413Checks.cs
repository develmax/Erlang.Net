using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Erlang;
namespace Erlang.Tests;

internal static class AtomicsHost413Checks
{
    internal static async Task AdmissionAndValues()
    {
        await using var runtime = await AtomicsHost413Access.Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term reference = await AtomicsHost413Access.Call(
            actor,
            runtime,
            AtomicsHost413Names.New,
            Term.I(3),
            Nil.Value
        );
        AtomicsHost413Access.Require(reference is ResourceReferenceTerm { ResourceKind: ReferenceResourceKind.Atomics413 });
        _ = await AtomicsHost413Access.Call(
            actor,
            runtime,
            AtomicsHost413Names.Put,
            reference,
            Term.I(1),
            Term.I(long.MaxValue)
        );
        Term wrapped = await AtomicsHost413Access.Call(
            actor,
            runtime,
            AtomicsHost413Names.AddGet,
            reference,
            Term.I(1),
            Term.I(1)
        );
        AtomicsHost413Access.Require(wrapped.Equals(Term.I(long.MinValue)));
        MapTerm info = (MapTerm)await AtomicsHost413Access.Call(
            actor,
            runtime,
            AtomicsHost413Names.Info,
            reference
        );
        AtomicsHost413Access.Require(info.TryGet(Term.A(AtomicsHost413Names.Memory), out Term? memory)
            && memory is Integer originalBytes
            && originalBytes.Value + System.Runtime.InteropServices.Marshal.OffsetOf(
                typeof(ProcessRuntime).Assembly.GetType(AtomicsHost413Names.MagicHeaderType586, true)!,
                AtomicsHost413Names.DestructorField586
            ).ToInt64() == runtime.AtomicsStorage413.OwnedNativeBytes);
        AtomicsHost413Access.Require(runtime.AtomicsStorage413.Created == 1 && runtime.AtomicsStorage413.LiveArrays == 1
            && runtime.AtomicsStorage413.ActiveInvocations == 0);
    }

    internal static async Task ConcurrentTickets()
    {
        await using var runtime = await AtomicsHost413Access.Create();
        var actors = Enumerable.Range(0, AtomicsHost413Names.Workers).Select(_ => new RuntimeNodeFoundationHostActor(runtime)).ToArray();
        try
        {
            Term reference = await AtomicsHost413Access.Call(
                actors[0],
                runtime,
                AtomicsHost413Names.New,
                Term.I(1),
                Nil.Value
            );
            var tasks = actors.Select(actor => actor.InvokeAsync(async context =>
            {
                var values = new long[AtomicsHost413Names.Iterations];
                for (int index = 0; index < values.Length; index++)
                    values[index] = (long)((Integer)await runtime.Modules.Call(
                        context,
                        AtomicsHost413Names.Atomics,
                        AtomicsHost413Names.AddGet,
                        reference,
                        Term.I(1),
                        Term.I(1)
                    )).Value;

                return values;
            })).ToArray();
            long[] tickets = (await Task.WhenAll(tasks)).SelectMany(values => values).Order().ToArray();
            AtomicsHost413Access.Require(tickets.SequenceEqual(Enumerable.Range(1,
                AtomicsHost413Names.Workers * AtomicsHost413Names.Iterations).Select(value => (long)value)));
            Term actual = await AtomicsHost413Access.Call(
                actors[0],
                runtime,
                AtomicsHost413Names.Get,
                reference,
                Term.I(1)
            );
            AtomicsHost413Access.Require(actual.Equals(Term.I(tickets.Length)));
        }
        finally
        {
            var failures = new List<Exception>();
            foreach (var actor in actors) try
                {
                    await actor.DisposeAsync();
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            if (failures.Count != 0) throw new AggregateException(failures);
        }
    }

    internal static async Task ResourceLifetime()
    {
        await using var runtime = await AtomicsHost413Access.Create();
        Term reference;
        await using (var creator = new RuntimeNodeFoundationHostActor(runtime))
        {
            reference = await AtomicsHost413Access.Call(
                creator,
                runtime,
                AtomicsHost413Names.New,
                Term.I(1),
                Nil.Value
            );
            _ = await AtomicsHost413Access.Call(
                creator,
                runtime,
                AtomicsHost413Names.Put,
                reference,
                Term.I(1),
                Term.I(42)
            );
        }
        await using var reader = new RuntimeNodeFoundationHostActor(runtime);
        AtomicsHost413Access.Require((await AtomicsHost413Access.Call(
            reader,
            runtime,
            AtomicsHost413Names.Get,
            reference,
            Term.I(1)
        )).Equals(Term.I(42)));
        Term restored = await reader.InvokeAsync(async context =>
        {
            Term bytes = await runtime.Modules.Call(
                context,
                AtomicsHost413Names.Runtime,
                AtomicsHost413Names.Encode,
                reference
            );

            return await runtime.Modules.Call(
                context,
                AtomicsHost413Names.Runtime,
                AtomicsHost413Names.Decode,
                bytes
            );
        });
        AtomicsHost413Access.Require(restored is ResourceReferenceTerm { ResourceKind: ReferenceResourceKind.Atomics413 } && restored.Equals(reference));
        AtomicsHost413Access.Require((await AtomicsHost413Access.Call(
            reader,
            runtime,
            AtomicsHost413Names.Get,
            restored,
            Term.I(1)
        )).Equals(Term.I(42)));
        Term wrongKind = runtime.ReferenceResources.CreateBinary(new byte[] { 1 });
        bool wrongRejected = false;
        try
        {
            _ = await AtomicsHost413Access.Call(
                reader,
                runtime,
                AtomicsHost413Names.Get,
                wrongKind,
                Term.I(1)
            );
        }
        catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
        {
            wrongRejected = true;
        }
        AtomicsHost413Access.Require(wrongRejected);
        await using var foreign = await AtomicsHost413Access.Create();
        await using var other = new RuntimeNodeFoundationHostActor(foreign);
        bool rejected = false;
        try
        {
            _ = await AtomicsHost413Access.Call(
                other,
                foreign,
                AtomicsHost413Names.Get,
                reference,
                Term.I(1)
            );
        }
        catch (ArgumentException)
        {
            rejected = true;
        }
        catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
        {
            rejected = true;
        }
        AtomicsHost413Access.Require(rejected && runtime.AtomicsStorage413.LiveArrays == 1 && foreign.AtomicsStorage413.LiveArrays == 0);
    }

    private static IDisposable Hold(ProcessRuntime runtime, Term reference, bool invocation)
    {
        if (invocation)
        {
            object owner = typeof(ProcessRuntime).GetField(AtomicsHost413Names.Owner, AtomicsHost413Names.Members)!.GetValue(runtime)!;

            return (IDisposable)owner.GetType().GetMethod(AtomicsHost413Names.Enter, AtomicsHost413Names.Members)!.Invoke(owner, null)!;
        }
        object entry = typeof(ResourceReferenceTerm).GetProperty(AtomicsHost413Names.Entry, AtomicsHost413Names.Members)!.GetValue(reference)!;
        Type rootType = typeof(ProcessRuntime).Assembly.GetType(AtomicsHost413Names.RootType)!;
        object root = entry.GetType().GetMethod(AtomicsHost413Names.Read, AtomicsHost413Names.Members)!.MakeGenericMethod(rootType).Invoke(entry, null)!;
        object array = rootType.GetProperty(AtomicsHost413Names.Array, AtomicsHost413Names.Members)!.GetValue(root)!;

        return (IDisposable)array.GetType().GetMethod(AtomicsHost413Names.Acquire, AtomicsHost413Names.Members)!.Invoke(array, null)!;
    }

    internal static async Task CloseBarriers()
    {
        foreach (bool invocation in new[] { true, false })
        {
            ProcessRuntime runtime = await AtomicsHost413Access.Create();
            IDisposable? held = null;
            Task? closing = null;
            try
            {
                Term reference;
                await using (var actor = new RuntimeNodeFoundationHostActor(runtime))
                    reference = await AtomicsHost413Access.Call(
                        actor,
                        runtime,
                        AtomicsHost413Names.New,
                        Term.I(1),
                        Nil.Value
                    );
                held = Hold(runtime, reference, invocation);
                closing = Task.Run(async () => await runtime.DisposeAsync());
                using var deadline = new CancellationTokenSource(AtomicsHost413Names.Deadline);
                while (!runtime.AtomicsStorage413.Closed) await Task.Delay(1, deadline.Token);
                AtomicsHost413Access.Require(!closing.IsCompleted);
                held.Dispose();
                held = null;
                await closing.WaitAsync(AtomicsHost413Names.Deadline);
                var report = runtime.AtomicsStorage413;
                AtomicsHost413Access.Require(report.Closed && report.ActiveInvocations == 0 && report.LiveArrays == 0
                    && report.OwnedNativeBytes == 0 && report.Created == report.Freed);
                GC.KeepAlive(reference);
            }
            finally
            {
                held?.Dispose();
                if (closing is not null) await closing.WaitAsync(AtomicsHost413Names.Deadline);
                await runtime.DisposeAsync();
            }
        }
    }

    internal static async Task RollbackAndBinding()
    {
        await using (var runtime = new ProcessRuntime())
        {
            bool rollback = false;
            try
            {
                runtime.Modules.Apply(registry =>
                {
                    RuntimeBootstrapAtomics.Register(runtime, registry);
                    throw new InvalidOperationException(AtomicsHost413Names.Rollback);
                });
            }
            catch (InvalidOperationException error) when (error.Message == AtomicsHost413Names.Rollback)
            {
                rollback = true;
            }
            AtomicsHost413Access.Require(rollback && !runtime.Modules.Exports.Any(export => export.Module == AtomicsHost413Names.Internal
                && export.Function == AtomicsHost413Names.Constructor && export.Arity == 2));
            RuntimeBootstrapAtomics.Register(runtime, runtime.Modules);
            RuntimeBootstrapAtomics.Register(runtime, runtime.Modules);
            await using var other = new ProcessRuntime();
            bool foreign = false;
            try
            {
                RuntimeBootstrapAtomics.Register(runtime, other.Modules);
            }
            catch (ArgumentException)
            {
                foreign = true;
            }
            AtomicsHost413Access.Require(foreign && runtime.AtomicsStorage413.Created == 0);
        }
        await using (var conflict = new ProcessRuntime())
        {
            conflict.Modules.Register(
                AtomicsHost413Names.Internal,
                AtomicsHost413Names.Constructor,
                2,
                (_, _) => ValueTask.FromResult<Term>(Nil.Value)
            );
            bool rejected = false;
            try
            {
                RuntimeBootstrapAtomics.Register(conflict, conflict.Modules);
            }
            catch (InvalidOperationException)
            {
                rejected = true;
            }
            AtomicsHost413Access.Require(rejected && conflict.AtomicsStorage413.Created == 0);
        }
        await using (var runtime = await AtomicsHost413Access.Create())
        {
            typeof(RuntimeReferenceResources).GetMethod(AtomicsHost413Names.Close, AtomicsHost413Names.Members)!.Invoke(runtime.ReferenceResources, null);
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);
            bool rejected = false;
            try
            {
                _ = await AtomicsHost413Access.Call(
                    actor,
                    runtime,
                    AtomicsHost413Names.New,
                    Term.I(1),
                    Nil.Value
                );
            }
            catch (ObjectDisposedException)
            {
                rejected = true;
            }
            var report = runtime.AtomicsStorage413;
            AtomicsHost413Access.Require(rejected && report.Created == 1 && report.Freed == 1 && report.LiveArrays == 0 && report.OwnedNativeBytes == 0);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<WeakReference> Dropped(ProcessRuntime runtime, RuntimeNodeFoundationHostActor actor)
    {
        Term value = await AtomicsHost413Access.Call(
            actor,
            runtime,
            AtomicsHost413Names.New,
            Term.I(1),
            Nil.Value
        );

        return new WeakReference(value);
    }

    internal static async Task GcNoGuestRoots()
    {
        await using var runtime = await AtomicsHost413Access.Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        WeakReference weak = await Dropped(runtime, actor);
        _ = await actor.Invoke(_ => true); // Replace actor's last native command closure before actual collection.
        using var deadline = new CancellationTokenSource(AtomicsHost413Names.Deadline);
        while (runtime.AtomicsStorage413.LiveArrays != 0)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            await Task.Delay(1, deadline.Token);
        }
        AtomicsHost413Access.Require(!weak.IsAlive && runtime.AtomicsStorage413.Created == runtime.AtomicsStorage413.Freed
            && runtime.AtomicsStorage413.OwnedNativeBytes == 0);
    }
}
