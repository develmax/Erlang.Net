extern alias NifNativeBackend;

using System.Reflection;
namespace Erlang.Tests;

internal static class SystemTaskOwner428Checks
{
    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(SystemTaskOwner428Names.Failure);
    }

    private static ValueTask<Term> Request(
        ProcessContext c,
        Pid target,
        string priority,
        Term operation
    )
     => c.Runtime.Modules.Call(
         c,
         SystemTaskOwner428Names.Module,
         SystemTaskOwner428Names.Request,
         target,
         Term.A(priority),
         operation
     );

    private static ValueTask<Term> Bind(ProcessContext c, int lane) => c.Runtime.Modules.Call(
        c,
        SystemTaskOwner428Names.Runtime,
        SystemTaskOwner428Names.Flag,
        Term.A(SystemTaskOwner428Names.Scheduler),
        Term.I(lane)
    );

    private static Term Operation(string kind, int id) => kind == SystemTaskOwner428Names.Gc
     ? Term.Tuple(Term.A(kind), Term.I(id), Term.A(SystemTaskOwner428Names.Minor))
     : Term.Tuple(Term.A(kind), Term.I(id));

    private static bool IsReply(Term value, string kind, int id) => value is TupleTerm { Items.Count: 3 } t
     && t.Items[0].Equals(Term.A(kind)) && t.Items[1].Equals(Term.I(id));

    private static ushort MinorCount(ProcessContext c)
    {
        object policy = typeof(ProcessContext).GetProperty(SystemTaskOwner428Names.Policy, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(c)!;

        return (ushort)policy.GetType().GetField(SystemTaskOwner428Names.MinorField, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(policy)!;
    }

    internal static async Task Binding()
    {
        await using var runtime = new ProcessRuntime();
        Require(!runtime.Modules.HasExport(SystemTaskOwner428Names.Module, SystemTaskOwner428Names.Request, 3));
        await runtime.InitializeModulesAsync(registry =>
      {
          RuntimeBootstrapInternalSystemTasks428.Register(runtime, registry);
          RuntimeBootstrapInternalSystemTasks428.Register(runtime, registry);
      });
        await using var foreign = new ProcessRuntime();
        await foreign.InitializeModulesAsync(_ => { });
        bool refused = false;
        try
        {
            RuntimeBootstrapInternalSystemTasks428.Register(runtime, foreign.Modules);
        }
        catch (ArgumentException)
        {
            refused = true;
        }
        Require(refused);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async c =>
        {
            foreach (string f in new[] { SystemTaskOwner428Names.Check, SystemTaskOwner428Names.Handle })
            {
                bool notsup = false;
                try
                {
                    if (f == SystemTaskOwner428Names.Check) await runtime.Modules.Call(
                        c,
                        SystemTaskOwner428Names.Module,
                        f,
                        Term.I(0),
                        Term.I(0)
                    );
                    else await runtime.Modules.Call(
                        c,
                        SystemTaskOwner428Names.Module,
                        f,
                        Term.Tuple()
                    );
                }
                catch (ErlangException error)
                {
                    notsup = error.Reason.Equals(Term.A(SystemTaskOwner428Names.NotSupported));
                }
                Require(notsup);
            }
            Require((await Request(
                c,
                c.Self,
                SystemTaskOwner428Names.Normal,
                Operation(SystemTaskOwner428Names.Test, 0)
            )).Equals(Term.A(SystemTaskOwner428Names.Ok)));
            Term? reply = await c.Mailbox.ReceiveAsync<Term>(v => IsReply(v, SystemTaskOwner428Names.Test, 0) ? v : null, SystemTaskOwner428Names.Deadline, c.Cancellation);
            Require(reply!.Equals(Term.Tuple(Term.A(SystemTaskOwner428Names.Test), Term.I(0), Term.A(SystemTaskOwner428Names.True))));

            return true;
        });
    }

    internal static async Task Priority()
    {
        await using var runtime = new ProcessRuntime(2);
        await runtime.InitializeModulesAsync(registry => RuntimeBootstrapInternalSystemTasks428.Register(runtime, registry));
        await using var requester = new RuntimeNodeFoundationHostActor(runtime);
        await using var target = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext t = await target.Ready;
        await requester.InvokeAsync(async c => await Bind(c, 1));
        await target.InvokeAsync(async c => await Bind(c, 2));
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        Task<bool>? held = null;
        try
        {
            held = target.Invoke(c =>
         {
             entered.Set();
             Require(release.Wait(SystemTaskOwner428Names.Deadline));

             return true;
         });
            Require(entered.Wait(SystemTaskOwner428Names.Deadline));
            await requester.InvokeAsync(async c =>
            {
                string[] order = [SystemTaskOwner428Names.Low, SystemTaskOwner428Names.Normal, SystemTaskOwner428Names.High, SystemTaskOwner428Names.Maximum];
                for (int i = 0; i < order.Length; i++) Require((await Request(
                    c,
                    t.Self,
                    order[i],
                    Operation(SystemTaskOwner428Names.Test, i)
                )).Equals(Term.A(SystemTaskOwner428Names.Ok)));
                Require(await c.Mailbox.ReceiveAsync<Term>(v => v is TupleTerm x && x.Items[0].Equals(Term.A(SystemTaskOwner428Names.Test)) ? v : null, TimeSpan.Zero, c.Cancellation) is null);

                return true;
            });
            release.Set();
            Require(await held);
            await requester.InvokeAsync(async c =>
            {
                for (int i = 3; i >= 0; i--)
                {
                    Term? reply = await c.Mailbox.ReceiveAsync<Term>(
                        v => v is TupleTerm x && x.Items[0].Equals(Term.A(SystemTaskOwner428Names.Test)) ? v : null,
                        SystemTaskOwner428Names.Deadline,
                        c.Cancellation
                    );
                    Require(reply!.Equals(Term.Tuple(Term.A(SystemTaskOwner428Names.Test), Term.I(i), Term.A(SystemTaskOwner428Names.True))));
                }

                return true;
            });
            await target.InvokeAsync(async c => RequireNormal(await runtime.Modules.Call(
                c,
                SystemTaskOwner428Names.Runtime,
                SystemTaskOwner428Names.Info,
                c.Self,
                Term.A(SystemTaskOwner428Names.Priority)
            )));
        }
        finally
        {
            release.Set();
            if (held is not null) await held;
        }
    }

    private static bool RequireNormal(Term value)
    {
        Require(value.Equals(Term.Tuple(Term.A(SystemTaskOwner428Names.Priority), Term.A(SystemTaskOwner428Names.Normal))));

        return true;
    }

    internal static async Task Death()
    {
        await using var runtime = new ProcessRuntime(2);
        await runtime.InitializeModulesAsync(registry => RuntimeBootstrapInternalSystemTasks428.Register(runtime, registry));
        await using var requester = new RuntimeNodeFoundationHostActor(runtime);
        await using var target = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext t = await target.Ready;
        await requester.InvokeAsync(async c => await Bind(c, 1));
        await target.InvokeAsync(async c => await Bind(c, 2));
        ushort before = MinorCount(t);
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        Task<bool>? held = null;
        try
        {
            held = target.Invoke(c =>
         {
             entered.Set();
             Require(release.Wait(SystemTaskOwner428Names.Deadline));

             return true;
         });
            Require(entered.Wait(SystemTaskOwner428Names.Deadline));
            await requester.InvokeAsync(async c =>
            {
                ReferenceTerm monitor = runtime.Monitor(c, t.Self);
                Require((await Request(
                    c,
                    t.Self,
                    SystemTaskOwner428Names.Normal,
                    Operation(SystemTaskOwner428Names.Gc, 1)
                )).Equals(Term.A(SystemTaskOwner428Names.Ok)));
                runtime.Exit(c, t.Self, Term.A(SystemTaskOwner428Names.Kill));
                Term? first = await c.Mailbox.ReceiveAsync<Term>(_ => _, SystemTaskOwner428Names.Deadline, c.Cancellation);
                Require(first!.Equals(Term.Tuple(Term.A(SystemTaskOwner428Names.Gc), Term.I(1), Term.A(SystemTaskOwner428Names.False))));
                Term? down = await c.Mailbox.ReceiveAsync<Term>(
                    v => v is TupleTerm { Items.Count: 5 } x && x.Items[0].Equals(Term.A(SystemTaskOwner428Names.Down)) && x.Items[1].Equals(monitor) ? v : null,
                    SystemTaskOwner428Names.Deadline,
                    c.Cancellation
                );
                Require(down is not null && MinorCount(t) == before);

                return true;
            });
            release.Set();
            Require(await held);
            Require((await target.Process.Completion.WaitAsync(SystemTaskOwner428Names.Deadline)).Equals(Term.A(SystemTaskOwner428Names.Killed)));
        }
        finally
        {
            release.Set();
            if (held is not null) await held;
        }
    }


    private static async Task<ProcessContext[]> TraceStartup514(ProcessRuntime runtime)
    {
        var records = new TaskCompletionSource<Term[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        var tracerReady = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var stop = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle tracer = runtime.Spawn(async context =>
        {
            tracerReady.TrySetResult(context);
            try
            {
                var actual = new Term[SystemTaskOwner428Names.TraceEvents514];
                for (int index = 0; index < actual.Length; index++)
                    actual[index] = (await context.ReceiveAsync<Term>(value => value, SystemTaskOwner428Names.Deadline))!;
                records.TrySetResult(actual);
                await stop.Task.WaitAsync(SystemTaskOwner428Names.Deadline);
            }
            catch (Exception failure)
            {
                records.TrySetException(failure);
                throw;
            }

            return Nil.Value;
        });
        ProcessContext tracerContext = await tracerReady.Task.WaitAsync(SystemTaskOwner428Names.Deadline);
        object gate = ManagedCollectors413Access.Field(runtime, ManagedCollectorPrestart415Names.Gate)!;
        ProcessContext parent;
        lock (gate)
            parent = (ProcessContext)((System.Collections.IDictionary)ManagedCollectors413Access.Field(runtime, ManagedCollectorPrestart415Names.Processes)!)[runtime.InitializationPid]!;
        object tracing = ManagedCollectors413Access.Property(runtime, SystemTaskOwner428Names.Tracing514)!;

        object? TraceCall(string methodName, params object?[] arguments)
            => ManagedCollectors413Access.Invoke(
                tracing,
                tracing.GetType(),
                methodName,
                arguments
            );
        object[] slots = ((Array)ManagedCollectors413Access.Field(runtime, SystemTaskOwner428Names.HandlerSlots514)!).Cast<object>().ToArray();
        Require(slots.Length == SystemTaskOwner428Names.HandlerCount514);
        PreparedBeamImage image = (PreparedBeamImage)runtime.Modules.SourceOwner(SystemTaskOwner428Names.HandlerModule514, ManagedCollectors413Names.Start, 0)!;
        ProcessContext[] actors = new ProcessContext[slots.Length];
        try
        {
            // Same actual bytes/hash, independently prepared but never published:
            // source identity must be refused before any privileged role starts.
            using (Stream source = typeof(SystemTaskOwner428Checks).Assembly.GetManifestResourceStream(SystemTaskOwner428Names.HandlerPrefix + SystemTaskOwner428Names.HandlerBeam)!)
            using (var memory = new MemoryStream())
            {
                source.CopyTo(memory);
                byte[] bytes = memory.ToArray();
                Require(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)) == SystemTaskOwner428Names.HandlerHash);
                await using PreparedBeamImage unpublished = runtime.Modules.PrepareBeam(bytes,
                    new BeamDependencySelection([SystemTaskOwner428Names.Runtime, SystemTaskOwner428Names.Module]));
                bool refused = false;
                try
                {
                    ManagedCollectors413Access.Invoke(
                        runtime,
                        typeof(ProcessRuntime),
                        SystemTaskOwner428Names.StartHandlers514,
                        unpublished
                    );
                }
                catch (InvalidOperationException)
                {
                    refused = true;
                }
                Require(refused && slots.All(slot => ManagedCollectors413Access.Property(slot, ManagedCollectors413Names.Actor) is null
                    && ManagedCollectors413Access.Property(slot, ManagedCollectors413Names.Source) is null
                    && ManagedCollectors413Access.Property(slot, ManagedCollectorPrestart415Names.Reservation) is null));
            }
            TraceCall(SystemTaskOwner428Names.SetTracer514, tracer.Pid);
            TraceCall(
                SystemTaskOwner428Names.TraceSet514,
                parent,
                Term.A(SequenceTraceNames.Label),
                Term.I(SystemTaskOwner428Names.TraceLabel514)
            );
            TraceCall(
                SystemTaskOwner428Names.TraceSet514,
                parent,
                Term.A(SequenceTraceNames.Send),
                Term.A(SystemTaskOwner428Names.True)
            );
            TraceCall(
                SystemTaskOwner428Names.TraceSet514,
                parent,
                Term.A(SequenceTraceNames.Receive),
                Term.A(SystemTaskOwner428Names.True)
            );
            TraceCall(
                SystemTaskOwner428Names.TraceSet514,
                parent,
                Term.A(SequenceTraceNames.Serial),
                Term.Tuple(Term.I(0), Term.I(0))
            );
            Term groupLeader = runtime.GetGroupLeader(parent);
            RuntimeBootstrapInternalSystemTasks428.StartOriginalHandlers(runtime);
            lock (gate)
                for (int index = 0; index < slots.Length; index++)
                {
                    object slot = slots[index];
                    actors[index] = (ProcessContext)ManagedCollectors413Access.Property(slot, ManagedCollectors413Names.Actor)!;
                    Require(actors[index] is not null && ReferenceEquals(ManagedCollectors413Access.Property(slot, ManagedCollectors413Names.Source), image)
                        && ManagedCollectors413Access.Property(slot, ManagedCollectorPrestart415Names.Reservation) is not null);
                    object actualPriority = ManagedCollectors413Access.Property(actors[index], SystemTaskOwner428Names.SchedulingPriority514)!;
                    Require(actualPriority.ToString() == SystemTaskOwner428Names.StartupPriorities514[index]
                        && Equals(actualPriority, ManagedCollectors413Access.Property(slot, SystemTaskOwner428Names.SlotPriority514)));
                }
            Require(actors.Select(actor => actor.Self).Distinct().Count() == SystemTaskOwner428Names.HandlerCount514);
            RuntimeBootstrapInternalSystemTasks428.StartOriginalHandlers(runtime);
            lock (gate)
                for (int index = 0; index < slots.Length; index++)
                    Require(ReferenceEquals(ManagedCollectors413Access.Property(slots[index], ManagedCollectors413Names.Actor), actors[index]));
            Term[] actual = await records.Task.WaitAsync(SystemTaskOwner428Names.Deadline);
            var references = new HashSet<Term>();

            Term Notice(
                string kind,
                long previous,
                long serial,
                Term sender,
                Term destination,
                Term message
            )
                => Term.Tuple(
                    Term.A(SequenceTraceNames.Message),
                    Term.I(SystemTaskOwner428Names.TraceLabel514),
                    Term.Tuple(
                        Term.A(kind),
                        Term.Tuple(Term.I(previous), Term.I(serial)),
                        sender,
                        destination,
                        message
                    )
                );
            for (int index = 0; index < actors.Length; index++)
            {
                int offset = index * SystemTaskOwner428Names.TraceEventsPerHandler514;
                Require(actual[offset] is TupleTerm { Items.Count: 3 } notification && notification.Items[2] is TupleTerm { Items.Count: 5 });
                var details = (TupleTerm)((TupleTerm)actual[offset]).Items[2];
                Require(details.Items[4] is TupleTerm { Items.Count: 8 });
                var requestActual = (TupleTerm)details.Items[4];
                Term reference = requestActual.Items[1];
                Require(reference is ReferenceTerm && references.Add(reference));
                Term request = Term.Tuple(
                    Term.A(SequenceTraceNames.SpawnRequest),
                    reference,
                    parent.Self,
                    groupLeader,
                    Term.Tuple(Term.A(SystemTaskOwner428Names.HandlerModule514), Term.A(ManagedCollectors413Names.Start), Term.I(0)),
                    Nil.Value,
                    Term.A(SequenceTraceNames.SpawnReply),
                    Nil.Value
                );
                Term reply = Term.Tuple(
                    Term.A(SequenceTraceNames.SpawnReply),
                    reference,
                    Term.A(SystemTaskOwner428Names.Ok),
                    actors[index].Self
                );
                var serials = SystemTaskOwner428Names.StartupSerials514[index];
                Term[] expected = [
                    Notice(
                        SequenceTraceNames.Send,
                        serials.PreviousRequest,
                        serials.Request,
                        parent.Self,
                        actors[index].Self,
                        request
                    ),
                    Notice(
                        SequenceTraceNames.Receive,
                        serials.PreviousRequest,
                        serials.Request,
                        parent.Self,
                        actors[index].Self,
                        request
                    ),
                    Notice(
                        SequenceTraceNames.Send,
                        serials.PreviousReply,
                        serials.Reply,
                        actors[index].Self,
                        parent.Self,
                        reply
                    ),
                    Notice(
                        SequenceTraceNames.Receive,
                        serials.PreviousReply,
                        serials.Reply,
                        actors[index].Self,
                        parent.Self,
                        reply
                    )
                ];
                for (int field = 0; field < expected.Length; field++) Require(actual[offset + field].Equals(expected[field]));
            }
            Require(ManagedCollectors413Access.Snapshot(tracerContext).Count == 0);

            return actors;
        }
        finally
        {
            try
            {
                TraceCall(
                    SystemTaskOwner428Names.TraceSet514,
                    parent,
                    Term.A(SequenceTraceNames.Token),
                    Nil.Value
                );
                TraceCall(SystemTaskOwner428Names.SetTracer514, Term.A(SystemTaskOwner428Names.False));
            }
            finally
            {
                stop.TrySetResult();
                await tracer.Completion.WaitAsync(SystemTaskOwner428Names.Deadline);
            }
        }
    }

    internal static async Task DirtySignals()
    {
        await using var runtime = new ProcessRuntime(2);
        await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
            runtime,
            async (registry, scope) =>
        {
            await runtime.InitializeModulesAsync(_ =>
            {
                {
                    RuntimeBootstrapInternalSystemTasks428.Register(runtime, registry);
                }
                {
                    RuntimeBootstrapProcessAlive425.Register(runtime, registry);
                }
            });
            using var stream = typeof(SystemTaskOwner428Checks).Assembly.GetManifestResourceStream(SystemTaskOwner428Names.HandlerPrefix + SystemTaskOwner428Names.HandlerBeam)!;
            using var bytes = new MemoryStream();
            stream.CopyTo(bytes);
            byte[] image = bytes.ToArray();
            Require(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(image)) == SystemTaskOwner428Names.HandlerHash);
            PreparedBeamImage handler = scope.Prepare(() => registry.PrepareBeam(image, new BeamDependencySelection([SystemTaskOwner428Names.Runtime, SystemTaskOwner428Names.Module])));

            return _ =>
            {
                Require(handler.NativePrimitiveExports.Count == 0 && registry.PublishPreparedBeam([handler]).Status == BeamPublicationStatus.Published);
            };
        }
        );
        ProcessContext[] startupActors514 = await TraceStartup514(runtime);
        await using var requester = new RuntimeNodeFoundationHostActor(runtime);
        await using var target = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext t = await target.Ready;
        await requester.InvokeAsync(async c => await Bind(c, 1));
        await target.InvokeAsync(async c => await Bind(c, 2));
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        Task<bool>? dirty = null;
        try
        {
            dirty = target.InvokeAsync(async c =>
            {
                await DirtyNative419Access.Schedule(
                    c,
                    SystemTaskOwner428Names.Cpu,
                    [],
                    (actual, _) =>
       {
           Require(ReferenceEquals(actual, t) && DirtyNative419Access.Kind(actual) == SystemTaskOwner428Names.Cpu && !DirtyNative419Access.NormalTurn(actual));
           entered.Set();
           Require(release.Wait(SystemTaskOwner428Names.Deadline));

           return Nil.Value;
       }
                );

                return true;
            });
            Require(entered.Wait(SystemTaskOwner428Names.Deadline));
            Action verifyDeferredBacking = NifNativeBackend::Erlang.NifNativeBackend.DeferredMailboxBackingChecks.PrepareDirtyTarget(t);
            await requester.InvokeAsync(async c =>
            {
                ReferenceTerm reference = runtime.MakeReference();
                Require((await runtime.Modules.Call(
                    c,
                    SystemTaskOwner428Names.Module,
                    SystemTaskOwner428Names.Alive,
                    t.Self,
                    reference
                )).Equals(Term.A(SystemTaskOwner428Names.Ok)));
                Term? reply = await c.Mailbox.ReceiveAsync<Term>(v => v is TupleTerm { Items.Count: 2 } x && x.Items[0].Equals(reference) ? v : null, SystemTaskOwner428Names.Deadline, c.Cancellation);
                Require(reply!.Equals(Term.Tuple(reference, Term.A(SystemTaskOwner428Names.True))));
                // The actual physical worker is still held: only the genuine selected source
                // handler can admit this target's pending ordered liveness control here.
                Require(!release.IsSet && dirty is { IsCompleted: false });
                verifyDeferredBacking();

                return true;
            });
            release.Set();
            Require(await dirty);
        }
        finally
        {
            release.Set();
            if (dirty is not null) await dirty;
        }
        await target.DisposeAsync();
        await requester.DisposeAsync();
        await runtime.DisposeAsync();
        foreach (object slot in (Array)ManagedCollectors413Access.Field(runtime, SystemTaskOwner428Names.HandlerSlots514)!)
            foreach (string member in new[] { ManagedCollectors413Names.Actor, ManagedCollectors413Names.Source, ManagedCollectorPrestart415Names.Reservation })
                Require(ManagedCollectors413Access.Property(slot, member) is null);
        foreach (ProcessContext actor in startupActors514)
        {
            Require((bool)ManagedCollectors413Access.Property(actor, SystemTaskOwner428Names.Terminated514)!);
            var unwound = (TaskCompletionSource)ManagedCollectors413Access.Property(actor, ManagedCollectorPrestart415Names.Unwound)!;
            Require(unwound.Task.IsCompletedSuccessfully);
        }
    }
}
