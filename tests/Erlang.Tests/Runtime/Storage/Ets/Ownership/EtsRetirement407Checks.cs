using System.Reflection;

namespace Erlang.Tests;

internal static class EtsRetirement407Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private static long Populate(object storage, ProcessContext context, Term table)
    {
        long last = 0;
        for (int index = 0; index < EtsRetirement407Names.Rows; index++)
            last = EtsFoundation405Access.Store(
                storage,
                context,
                table,
                Term.Tuple(Term.I(index))
            );

        return last;
    }

    internal static async Task OwnerDown()
    {
        await using var runtime = new ProcessRuntime(1);
        object storage = EtsFoundation405Access.Get(runtime);
        await using var observer = new RuntimeNodeFoundationHostActor(runtime);
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        Term table = await owner.Invoke(context =>
        {
            Term id = EtsFoundation405Access.Create(storage, context, true);
            Populate(storage, context, id);

            return id;
        });
        ReferenceTerm monitor = await observer.Invoke(context => runtime.Monitor(context, owner.Process.Pid));
        Task completed = owner.DisposeAsync().AsTask();
        await observer.InvokeAsync(async context =>
        {
            Term? down = await context.Mailbox.ReceiveAsync<Term>(
                term => term is TupleTerm { Items.Count: 5 } tuple && tuple.Items[0].Equals(Term.A(EtsRetirement407Names.Down))
                    && tuple.Items[1].Equals(monitor) ? term : null,
                EtsRetirement407Names.Deadline,
                context.Cancellation
            );
            EtsFoundation405Access.Require(down is not null);
            EtsFoundation405Access.Empty(storage);
            EtsFoundation405Access.Require((long)storage.GetType().GetProperty(EtsRetirement407Names.Yields, Members)!.GetValue(storage)!
                >= EtsRetirement407Names.MinimumYields);
            bool stale = false;
            try
            {
                EtsFoundation405Access.Read(
                    storage,
                    context,
                    table,
                    1
                );
            }
            catch (InvalidOperationException)
            {
                stale = true;
            }
            EtsFoundation405Access.Require(stale);

            return true;
        });
        await completed.WaitAsync(EtsRetirement407Names.Deadline);
    }

    internal static async Task SparseAndReader()
    {
        await using var runtime = new ProcessRuntime(1);
        object storage = EtsFoundation405Access.Get(runtime);
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        var held = await owner.Invoke(context =>
        {
            Term table = EtsFoundation405Access.Create(storage, context, true);
            long last = Populate(storage, context, table);
            for (long index = 1; index < last; index++)
                EtsFoundation405Access.Require((bool)EtsFoundation405Access.Invoke(
                    storage,
                    EtsRetirement407Names.Remove,
                    context,
                    table,
                    index
                )!);

            return EtsFoundation405Access.Invoke(
                storage,
                EtsFoundation405Names.AcquireRead,
                context,
                table,
                last
            )!;
        });
        await owner.DisposeAsync();
        var snapshot = EtsFoundation405Access.Snapshot(storage);
        EtsFoundation405Access.Require((int)snapshot[0]! == 0 && (long)snapshot[3]! == 1);
        Term copy = (Term)EtsFoundation405Access.Invoke(held, EtsFoundation405Names.Materialize, CancellationToken.None)!;
        EtsFoundation405Access.Require(copy.Equals(Term.Tuple(Term.I(EtsRetirement407Names.Rows - 1))));
        ((IDisposable)held).Dispose();
        EtsFoundation405Access.Empty(storage);
    }

    private static ProcessContext Initializer(ProcessRuntime runtime)
    {
        object gate = typeof(ProcessRuntime).GetField(EtsRetirement407Names.Gate, Members)!.GetValue(runtime)!;
        lock (gate)
        {
            var processes = (Dictionary<Pid, ProcessContext>)typeof(ProcessRuntime).GetField(EtsRetirement407Names.ProcessMap, Members)!.GetValue(runtime)!;

            return processes[runtime.InitializationPid];
        }
    }

    internal static async Task ExplicitDelete()
    {
        await using var runtime = new ProcessRuntime(1);
        object storage = EtsFoundation405Access.Get(runtime);
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        await using var writer = new RuntimeNodeFoundationHostActor(runtime);
        Term old = await owner.Invoke(context =>
        {
            Term table = EtsFoundation405Access.Create(
                storage,
                context,
                true,
                EtsFoundation405Names.Public
            );
            Populate(storage, context, table);

            return table;
        });
        var deletion = await writer.Invoke(context =>
        {
            object gate = typeof(ProcessRuntime).GetField(EtsRetirement407Names.Gate, Members)!.GetValue(runtime)!;
            lock (gate)
            {
                Task retired = (Task)EtsFoundation405Access.Invoke(
                    storage,
                    EtsRetirement407Names.DeleteAsync,
                    context,
                    old
                )!;
                // Pin the real generation race: the retirement cursor cannot acquire this gate yet.
                Term replacement = EtsFoundation405Access.Create(storage, context, true);
                long slot = EtsFoundation405Access.Store(
                    storage,
                    context,
                    replacement,
                    Term.Tuple(Term.I(1))
                );

                return (Retired: retired, Slot: slot);
            }
        });
        await deletion.Retired.WaitAsync(EtsRetirement407Names.Deadline);
        await writer.Invoke(context =>
        {
            Term copy = EtsFoundation405Access.Read(
                storage,
                context,
                Term.A(EtsFoundation405Names.Name),
                deletion.Slot
            );
            EtsFoundation405Access.Require(copy.Equals(Term.Tuple(Term.I(1))));

            return true;
        });
    }

    internal static async Task GlobalShutdown()
    {
        foreach (bool halt in new[] { false, true })
        {
            await using var runtime = new ProcessRuntime(1);
            object storage = EtsFoundation405Access.Get(runtime);
            await using var owner = new RuntimeNodeFoundationHostActor(runtime);
            await owner.Invoke(context => Populate(storage, context, EtsFoundation405Access.Create(storage, context, true)));
            if (halt)
            {
                await runtime.RequestHalt(0, new RuntimeHaltOptions(false));
                await runtime.HaltQuiescence.WaitAsync(EtsRetirement407Names.Deadline);
            }
            else
                await runtime.DisposeAsync();
            EtsFoundation405Access.Empty(storage, true);
        }
        await using var initialized = new ProcessRuntime(1);
        object initStorage = EtsFoundation405Access.Get(initialized);
        ProcessContext initializer = Initializer(initialized);
        await initialized.InitializeModulesAsync(_ => Populate(initStorage, initializer, EtsFoundation405Access.Create(initStorage, initializer, true)));
        await using var killer = new RuntimeNodeFoundationHostActor(initialized);
        await killer.Invoke(context =>
        {
            initialized.Exit(context, initialized.InitializationPid, Term.A(EtsRetirement407Names.Kill));

            return true;
        });
        await initialized.DisposeAsync();
        EtsFoundation405Access.Empty(initStorage, true);
    }
}
