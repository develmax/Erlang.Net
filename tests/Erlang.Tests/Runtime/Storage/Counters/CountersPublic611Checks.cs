using Erlang.Differential;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.CountersPublic611Names;

namespace Erlang.Tests;

internal static class CountersPublic611Checks
{
    internal static Task CallbackHost() => Run(Callback);

    internal static Task ReturnedHost() => Run(Returned);

    internal static Task SnapshotHost() => Run(MailboxSnapshot);

    internal static Task InfoSingleHost() => Run(InfoSingle);

    internal static Task InfoFullHost() => Run(InfoFull);

    internal static Task InfoListHost() => Run(InfoList);

    internal static Task InfoKeyedHost() => Run(InfoKeyed);

    internal static async Task ComponentHost()
    {
        var failures = new List<Exception>();
        foreach (int route in new[] { ComponentSingle, ComponentList })
            try
            {
                await Run(route);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        if (failures.Count != 0) throw new AggregateException(Failure, failures);
    }

    private static Task Run(int route) => CountersCustody596Checks.Run(async fixture =>
    {
        RuntimeNodeFoundationHostActor? receiver = null, requester = null;
        Task<Term>? pumping = null;
        ResourceReferenceTerm? callbackReference = null, escaped = null;
        object? backing = null, array = null;
        var carriers = new HashSet<IDisposable>(ReferenceEqualityComparer.Instance);
        var failures = new List<Exception>();
        try
        {
            Require((bool)Property(fixture.Runtime, GateReady));
            await fixture.Runtime.InitializeModulesAsync(registry =>
            {
                PreparedBeamImage bridge = CountersOriginal421Preparation.Prepare(fixture.Runtime, registry);
                Require(registry.PublishPreparedBeam(bridge).Status == BeamPublicationStatus.Published);
                CountersOriginal421Preparation.AssertPublished(registry, bridge);
            });
            using var compiled = GeneratedModuleCompiler.Compile(Source, FileName);
            compiled.Definition.Register(fixture.Runtime.Modules);
            receiver = new(fixture.Runtime);
            ProcessContext receiving = await receiver.Ready.WaitAsync(Deadline);
            ProcessHandle sender = fixture.Runtime.Spawn(context => fixture.Runtime.Modules.Call(
                context,
                Module,
                Send,
                receiver.Process.Pid
            ));
            Require((await sender.Completion.WaitAsync(Deadline)).Equals(Term.A(Normal)));
            Require(receiving.Mailbox.Count == 1 && fixture.Runtime.CountersStorage421.LiveArrays == 1);

            ResourceReferenceTerm Capture(Term message)
            {
                Require(message is TupleTerm { Items.Count: 2 } tuple && tuple.Items[0].Equals(Term.A(Tag)));

                return CaptureReference(((TupleTerm)message).Items[1]);
            }

            ResourceReferenceTerm CaptureReference(Term value)
            {
                Require(value is ResourceReferenceTerm);
                var reference = (ResourceReferenceTerm)value;
                object actualBacking = Property(reference, Backing);
                backing ??= actualBacking;
                array ??= Property(actualBacking, ArrayProperty);
                if (reference.GetType().GetProperty(Host, Members)!.GetValue(reference) is IDisposable carrier)
                    carriers.Add(carrier);

                return reference;
            }

            if (route is Callback or Returned)
            {
                Term? actual = await receiver.InvokeAsync(async context => await context.ReceiveAsync<Term>(message =>
                {
                    callbackReference = Capture(message);

                    return route == Callback ? Nil.Value : message;
                }, Deadline));
                escaped = route == Callback ? callbackReference : Capture(actual!);
                Require(callbackReference is not null);
            }
            else if (route == MailboxSnapshot)
            {
                var messages = (IReadOnlyList<Term>)Call(receiving.Mailbox, Snapshot)!;
                Require(messages.Count == 1);
                escaped = Capture(messages[0]);
            }
            else if (route is ComponentSingle or ComponentList)
            {
                // Actual embedding projection over the real queue. Only its
                // existing supported key parser is reflected; no snapshot,
                // value, slot or canonical invocation is fabricated.
                Type item = typeof(ProcessRuntime).Assembly.GetType(ItemType, true)!;
                object parsed = item.GetMethod(Parse, Members)!.Invoke(null, [Term.A(Messages)])!;
                global::System.Array keys = global::System.Array.CreateInstance(item, 1);
                keys.SetValue(parsed, 0);
                Term info = (Term)Call(
                    fixture.Runtime,
                    ProjectionMethod,
                    receiver.Process.Pid,
                    keys,
                    route == ComponentSingle
                )!;
                var pair = route == ComponentSingle ? (TupleTerm)info : Cons.Items(info).Cast<TupleTerm>().Single();
                Require(pair.Items[0].Equals(Term.A(Messages)));
                Term[] messages = Cons.Items(pair.Items[1]).ToArray();
                Require(messages.Length == 1);
                escaped = Capture(messages[0]);
            }
            else
            {
                // A real compiled receive pumps actual remote process-info
                // requests without an opaque false selector exporting copies.
                requester = new(fixture.Runtime);
                pumping = receiver.InvokeAsync(context => route is InfoFull or InfoKeyed
                    ? fixture.Runtime.Modules.Call(
                        context,
                        Module,
                        PumpDictionary,
                        requester.Process.Pid
                    ).AsTask()
                    : fixture.Runtime.Modules.Call(context, Module, Pump).AsTask());
                if (route is InfoFull or InfoKeyed)
                    await requester.InvokeAsync(async context => await context.ReceiveAsync<Term>(
                        value => value is Atom { Name: Ready } ? value : null, Deadline));
                Term info = await requester.InvokeAsync(async context => route == InfoFull
                    ? await fixture.Runtime.Modules.Call(
                        context,
                        ErlangModule,
                        ProcessInfo,
                        receiver.Process.Pid
                    )
                    : await fixture.Runtime.Modules.Call(
                        context,
                        ErlangModule,
                        ProcessInfo,
                        receiver.Process.Pid,
                        route == InfoKeyed ? Term.Tuple(Term.A(DictionaryKey), Term.A(Tag))
                            : route == InfoSingle ? Term.A(Messages) : Term.List(Term.A(Messages))
                    ));
                if (route == InfoKeyed)
                {
                    Require(info is TupleTerm { Items.Count: 2 } keyed
                        && keyed.Items[0].Equals(Term.Tuple(Term.A(DictionaryKey), Term.A(Tag))));
                    // Use the actual returned pair value, never a synthetic
                    // tagged message that could hide a missing Host carrier.
                    escaped = CaptureReference(((TupleTerm)info).Items[1]);
                }
                else
                {
                    Term messages;
                    if (route == InfoSingle)
                    {
                        Require(info is TupleTerm { Items.Count: 2 } single && single.Items[0].Equals(Term.A(Messages)));
                        messages = ((TupleTerm)info).Items[1];
                    }
                    else
                    {
                        string key = route == InfoFull ? DictionaryKey : Messages;
                        TupleTerm[] entries = Cons.Items(info).Cast<TupleTerm>()
                            .Where(entry => entry.Items.Count == 2 && entry.Items[0].Equals(Term.A(key))).ToArray();
                        Require(entries.Length == 1);
                        messages = entries[0].Items[1];
                    }
                    Term[] values = Cons.Items(messages).ToArray();
                    if (route == InfoFull)
                        values = values.OfType<TupleTerm>().Where(pair => pair.Items.Count == 2
                            && pair.Items[0].Equals(Term.A(Tag))).Cast<Term>().ToArray();
                    Require(values.Length == 1);
                    escaped = Capture(values[0]);
                }
                fixture.Runtime.Send(receiver.Process.Pid, Term.A(Stop));
                await pumping.WaitAsync(Deadline);
                pumping = null;
            }
            if (receiving.Mailbox.Count != 0)
                await receiver.InvokeAsync(context => fixture.Runtime.Modules.Call(context, Module, Take).AsTask());
            await ((Task)Call(receiving.Mailbox, Drain)!).WaitAsync(Deadline);
            // Process completion retires its actual guest heap. No manually
            // fabricated scope, guest lease release or CLR collection is used.
            await receiver.DisposeAsync().AsTask().WaitAsync(Deadline);
            receiver = null;
            // Native process-info export can own temporary result Guest
            // headers. They must retire before the last Host-drop assertion.
            if (requester is not null)
            {
                await requester.DisposeAsync().AsTask().WaitAsync(Deadline);
                requester = null;
            }
            Require(escaped is not null && backing is not null && array is not null);
            Require(Property(escaped!, Storage) is null);
            Observe(route, array!, BeforeHostProof);
            object? host = escaped!.GetType().GetProperty(Host, Members)!.GetValue(escaped);
            Require(host is IDisposable && (bool)Call(host!, Owns, backing)!);
            Require((bool)Property(array!, Allocated) && !((Task)Property(array!, Retired)).IsCompleted);
            Term? read = null;
            ProcessHandle reader = fixture.Runtime.Spawn(async context =>
            {
                read = await fixture.Runtime.Modules.Call(
                    context,
                    ErtsInternal,
                    Get,
                    escaped!,
                    Term.I(1)
                );

                return Nil.Value;
            });
            Require((await reader.Completion.WaitAsync(Deadline)).Equals(Term.A(Normal)) && read!.Equals(Term.I(Sentinel)));
            Observe(route, array!, AfterReaderDone);
            // The native read may create real temporary guest headers. Its Done
            // must precede disposing the last returned Host carrier.
            foreach (IDisposable carrier in carriers) carrier.Dispose();
            carriers.Clear();
            Observe(route, array!, AfterHostDrop);
            await ((Task)Property(array!, Retired)).WaitAsync(Deadline);
            Require(!(bool)Property(array!, Allocated));
            GC.KeepAlive(escaped);
            GC.KeepAlive(callbackReference);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            if (pumping is not null && receiver is not null)
                try
                {
                    fixture.Runtime.Send(receiver.Process.Pid, Term.A(Stop));
                    await pumping.WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            foreach (IDisposable carrier in carriers)
                try
                {
                    carrier.Dispose();
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            if (receiver is not null)
                try
                {
                    await receiver.DisposeAsync().AsTask().WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            if (requester is not null)
                try
                {
                    await requester.DisposeAsync().AsTask().WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
        }
        if (failures.Count != 0) throw new AggregateException(Failure, failures);
    });

    private static void Observe(int route, object array, string stage)
    {
        object gate = array.GetType().GetField(ArrayGate, Members)!.GetValue(array)!;
        long[] counts = new long[4];
        string[] fields = [HostCount, GuestCount, ProducerCount, PinCount];
        lock (gate)
            for (int index = 0; index < fields.Length; index++)
                counts[index] = Convert.ToInt64(array.GetType().GetField(fields[index], Members)!.GetValue(array));
        Console.WriteLine(
            Observation,
            route,
            stage,
            counts[0],
            counts[1],
            counts[2],
            counts[3]
        );
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }
}
