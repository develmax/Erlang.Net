using Erlang;
using System.Reflection;

namespace Erlang.Tests;

internal static class IdentifierObservationShutdownHostChecks
{
    internal static Task DisposeWithMonitorAndLink() => CheckShutdown(initializerDeath: false, halt: false);

    internal static Task InitializerDeathWithMonitorAndLink() => CheckShutdown(initializerDeath: true, halt: false);

    internal static Task HaltWithLiveCallbacks() => CheckShutdown(initializerDeath: false, halt: true);

    private static async Task CheckShutdown(bool initializerDeath, bool halt)
    {
        var runtime = new ProcessRuntime();
        // Target is inserted first: actual disposal visits its live monitoring observer.
        await using var target = new RuntimeNodeFoundationHostActor(runtime);
        await using var observer = new RuntimeNodeFoundationHostActor(runtime);
        await target.Ready;
        ProcessContext context = await observer.Ready;
        int closedDownCallbacks = 0;
        WrapIncoming(
            runtime,
            context.Mailbox,
            term =>
        {
            if (IsClosed(runtime) && term is TupleTerm tuple && tuple.Items.Count != 0 &&
                tuple.Items[0].Equals(Term.A(IdentifierObservationShutdownHostNames.Down)))
                Interlocked.Increment(ref closedDownCallbacks);
        }
        );
        await observer.Invoke(owner =>
        {
            owner.TrapExits = true;
            runtime.Link(owner, target.Process.Pid);

            return runtime.Monitor(owner, target.Process.Pid);
        });
        try
        {
            if (initializerDeath)
            {
                var killer = runtime.Spawn(owner =>
                {
                    runtime.Exit(owner, runtime.InitializationPid, Term.A(IdentifierObservationShutdownHostNames.Kill));

                    return ValueTask.FromResult<Term>(Nil.Value);
                });
                await killer.Completion.WaitAsync(IdentifierObservationShutdownHostNames.Deadline);
            }
            else if (halt)
            {
                await runtime.RequestHalt(IdentifierObservationShutdownHostNames.HaltStatus,
                    new RuntimeHaltOptions(false)).WaitAsync(IdentifierObservationShutdownHostNames.Deadline);
                await runtime.HaltQuiescence.WaitAsync(IdentifierObservationShutdownHostNames.Deadline);
            }
            else
                await runtime.DisposeAsync().AsTask().WaitAsync(IdentifierObservationShutdownHostNames.Deadline);
            await Task.WhenAll(target.Process.Completion, observer.Process.Completion)
                .WaitAsync(IdentifierObservationShutdownHostNames.Deadline);
            Check(IsClosed(runtime));
            if (!halt)
                Check(Volatile.Read(ref closedDownCallbacks) > 0);
            Check(!runtime.IsAlive(target.Process.Pid) && !runtime.IsAlive(observer.Process.Pid));
        }
        finally
        {
            await runtime.DisposeAsync().AsTask().WaitAsync(IdentifierObservationShutdownHostNames.Deadline);
        }
    }

    private static bool IsClosed(ProcessRuntime runtime)
    {
        object known = typeof(ProcessRuntime).GetProperty(IdentifierObservationShutdownHostNames.KnownNodesProperty,
            BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(runtime)!;

        return (bool)known.GetType().GetField(IdentifierObservationShutdownHostNames.ClosedField,
            BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(known)!;
    }

    private static void WrapIncoming(ProcessRuntime runtime, Mailbox mailbox, Action<Term> record)
    {
        PropertyInfo property = typeof(Mailbox).GetProperty(IdentifierObservationShutdownHostNames.IncomingTermProperty,
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        var original = (Action<Term>)property.GetValue(mailbox)!;
        property.SetValue(mailbox, (Action<Term>)(term =>
        {
            record(term);
            original(term);
        }));
        GC.KeepAlive(runtime);
    }

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(IdentifierObservationShutdownHostNames.Failure);
    }
}
