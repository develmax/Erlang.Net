using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.SequenceLiteral814Names;

namespace Erlang.Tests;

internal static class SequenceLiteral814Checks
{
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    internal static async Task ActualTokenLiteralCustody()
    {
        await ReceivedPreparationRollback456();
        await SpeculativeHostRollback429();
        foreach (bool owned in new[] { false, true })
        {
            await using ProcessRuntime runtime = OwnedGuest506RuntimeFactory.Create(owned);
            Term portable = Term.Tuple(Term.A(LiteralAtom), Cons.From([Term.I(17), Term.I(23)]));
            await using PreparedBeamImage prepared = runtime.Modules.PrepareBeam(
                BeamLiteralExportHostBytes.Image(ExternalTermFormat.Encode(portable)));
            object code = Get(prepared, Code);
            TupleTerm literal = ((IReadOnlyList<Term>)Get(Get(code, Structure), Literals))
                .OfType<TupleTerm>().Single(value => value.Items.Count == 2);
            ModuleCodeImage image = (ModuleCodeImage)Get(Get(prepared, Execution), Image);
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);
            await actor.InvokeAsync(context =>
            {
                object heap = Get(context, Heap);
                object tracing = Get(runtime, Tracing);
                CollectNow(heap);
                long baseline = ReadWords(heap);
                bool rejected = false;
                try
                {
                    Call(
                        tracing,
                        Set,
                        context,
                        Term.A(SequenceTraceNames.Label),
                        literal
                    );
                }
                catch (InvalidOperationException)
                {
                    rejected = true;
                }
                Require(rejected && ReadWords(heap) == baseline);
                using SourceCodeLease admitted = runtime.Modules.ReserveSourceCode(context, image);
                using IDisposable attached = admitted.Attach(prepared);
                IDisposable? transport = null, copied = null;
                try
                {
                    Call(
                        tracing,
                        Set,
                        context,
                        Term.A(SequenceTraceNames.Label),
                        literal
                    );
                    object state = Get(context, State);
                    object token = Get(state, Token);
                    TupleTerm actual = (TupleTerm)Get(token, SequenceLiteral814Names.Tuple);
                    object storage = Get(actual, Storage);
                    Require((bool)Get(storage, Mutable) && !(bool)Get(storage, Immutable)
                        && ReferenceEquals(Get(token, Label), literal)
                        && ReadWords(heap) == baseline + TokenWords);
                    Call(
                        tracing,
                        Set,
                        context,
                        Term.A(SequenceTraceNames.Serial),
                        Term.Tuple(Term.I(3), Term.I(9))
                    );
                    token = Get(state, Token);
                    Require(ReferenceEquals(Get(token, SequenceLiteral814Names.Tuple), actual)
                        && ReferenceEquals(Get(token, Label), literal)
                        && ReadWords(heap) == baseline + TokenWords);
                    Type transportType = typeof(ProcessRuntime).Assembly.GetType(TransportType)!;
                    transport = (IDisposable)CallStatic(
                        transportType,
                        Capture,
                        runtime,
                        token
                    )!;
                    object copiedToken = Get(transport, Token);
                    object? literalRoots = GetNullable(transport, LiteralRoots);
                    Require(literalRoots is not null);
                    Type storageType = typeof(ProcessRuntime).Assembly.GetType(StorageType)!;
                    copied = (IDisposable)CallStatic(
                        storageType,
                        Prepare,
                        context,
                        copiedToken,
                        literalRoots
                    )!;
                    object copy = Get(copied, Token);
                    TupleTerm copyTuple = (TupleTerm)Get(copy, SequenceLiteral814Names.Tuple);
                    object copyStorage = Get(copyTuple, Storage);
                    Require(!ReferenceEquals(actual, copyTuple) && (bool)Get(copyStorage, Mutable)
                        && !(bool)Get(copyStorage, Immutable) && ReferenceEquals(Get(copy, Label), literal)
                        && ReadWords(heap) == baseline + TokenWords + TokenWords);
                    Term cleared = (Term)Call(
                        tracing,
                        Set,
                        context,
                        Term.A(SequenceTraceNames.Token),
                        Nil.Value
                    )!;
                    Require(cleared is TupleTerm result && result.Items[LabelIndex].Equals(portable)
                        && !ReferenceEquals(result.Items[LabelIndex], literal));
                    Require(CollectNow(heap) == TokenWords && ReadWords(heap) == baseline + TokenWords
                        && ReferenceEquals(Get(copy, Label), literal));
                    copied.Dispose();
                    copied = null;
                    Require(CollectNow(heap) == TokenWords && ReadWords(heap) == baseline);

                    return Task.FromResult(true);
                }
                finally
                {
                    Call(
                        tracing,
                        Set,
                        context,
                        Term.A(SequenceTraceNames.Token),
                        Nil.Value
                    );
                    copied?.Dispose();
                    CollectNow(heap);
                    transport?.Dispose();
                }
            });
            await prepared.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(DeadlineSeconds));
            bool stale = false;
            try
            {
                _ = literal.Items[0];
            }
            catch (ObjectDisposedException)
            {
                stale = true;
            }
            Require(stale);
        }
    }

    private static async Task ReceivedPreparationRollback456()
    {
        foreach (bool profile in new[] { false, true })
        {
            await using var runtime = OwnedGuest506RuntimeFactory.Create(profile);
            await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
            await using var tracer = new RuntimeNodeFoundationHostActor(runtime);
            ProcessContext actualTracer = await tracer.Ready.WaitAsync(TimeSpan.FromSeconds(DeadlineSeconds));
            object tracing = Get(runtime, Tracing);
            _ = Call(tracing, SetTracer456, actualTracer.Self);
            await receiver.Invoke(context =>
            {
                object heap = Get(context, Heap);
                var sourceFrame = (IDisposable)Call(heap, Enter456)!;
                Term stale;
                try
                {
                    int slot = (int)Call(sourceFrame, Reserve456)!;
                    stale = (Term)Call(
                        sourceFrame,
                        Adopt456,
                        slot,
                        Term.Tuple(Term.I(456))
                    )!;
                    Require(GetNullable(stale, Storage) is not null);
                }
                finally
                {
                    sourceFrame.Dispose();
                }
                _ = CollectNow(heap);
                _ = Call(
                    tracing,
                    Set,
                    context,
                    Term.A(SequenceTraceNames.Label),
                    Term.I(456)
                );
                _ = Call(
                    tracing,
                    Set,
                    context,
                    Term.A(SequenceTraceNames.Receive),
                    Term.A(ErlangBooleanAtoms.True)
                );
                object state = Get(context, State);
                object token = Get(state, Token);
                Type outputStateMachine = typeof(ProcessRuntime).Assembly.GetType(OutputOwner456, true)!
                    .GetMethod(OutputPreparation456, Members)!
                    .GetCustomAttribute<System.Runtime.CompilerServices.AsyncStateMachineAttribute>()!.StateMachineType;
                _ = CollectNow(heap);
                long baselineWords = ReadWords(heap);
                int baselineFrames = (int)Get(Get(heap, Snapshot456), RootFrames456);
                int[] collections = [GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2)];
                bool refused = false;
                try
                {
                    // A valid token is physically prepared first. Only the
                    // later real output producer encounters the stale message.
                    _ = Call(
                        tracing,
                        Received456,
                        context,
                        stale,
                        token,
                        false
                    );
                }
                catch (ObjectDisposedException error) when (new System.Diagnostics.StackTrace(error).GetFrames().Any(frame =>
                    frame.GetMethod()?.DeclaringType == outputStateMachine
                    || frame.GetMethod()?.DeclaringType?.FullName == OutputOwner456
                        && frame.GetMethod()?.Name == OutputPreparation456))
                {
                    refused = true;
                }
                Require(refused && ReferenceEquals(token, Get(state, Token)));
                Require((int)Get(Get(heap, Snapshot456), RootFrames456) == baselineFrames);
                Require(collections.SequenceEqual(new[] { GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2) }));
                // Frame disposal is immediate; physical cells are reclaimed by
                // the NEXT real guest GC, not by an invented disposal credit.
                Require(ReadWords(heap) == baselineWords + TokenWords);
                Require(CollectNow(heap) == TokenWords && ReadWords(heap) == baselineWords);
                _ = Call(
                    tracing,
                    Set,
                    context,
                    Term.A(SequenceTraceNames.Token),
                    Nil.Value
                );
                _ = CollectNow(heap);

                return true;
            });
            _ = Call(tracing, SetTracer456, Term.A(ErlangBooleanAtoms.False));
        }
    }

    private static async Task SpeculativeHostRollback429()
    {
        foreach (bool profile in new[] { false, true })
            foreach (bool counters in new[] { false, true })
            {
                await using var runtime = OwnedGuest506RuntimeFactory.Create(profile);
                await using var actor = new RuntimeNodeFoundationHostActor(runtime);
                ProcessContext context = await actor.Ready.WaitAsync(TimeSpan.FromSeconds(DeadlineSeconds));
                object tracing = Get(runtime, Tracing);
                Term? original = null;
                string backingName = counters ? CountersBacking429 : AtomicsBacking429;
                string hostName = counters ? CountersHost429 : AtomicsHost429;
                string retiredName = counters ? CountersRetired429 : Retired429;
                string rootsName = counters ? CountersHostRoots429 : HostRoots429;
                bool completed = false;
                object? array = null;
                Task? blocked = null;
                Task<Term>? operation = null;
                var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                try
                {
                    // Successful clear returns a real independently owned Host
                    // carrier. Source state/producer can die before that result.
                    await actor.InvokeAsync(async current =>
                    {
                        original = await NewReference429(runtime, current, counters);
                        array = Get(Get(original, backingName), Array429);
                        Require(HostCount429(array!, rootsName) == 1);
                        _ = Call(
                            tracing,
                            Set,
                            current,
                            Term.A(SequenceTraceNames.Label),
                            original
                        );
                        var returned = (TupleTerm)Call(
                            tracing,
                            Set,
                            current,
                            Term.A(SequenceTraceNames.Token),
                            Nil.Value
                        )!;
                        Term returnedReference = returned.Items[LabelIndex];
                        Require(HostCount429(array!, rootsName) == 2 && returnedReference.Equals(original));
                        _ = CollectNow(Get(current, Heap));
                        ((IDisposable)Get(original, hostName)).Dispose();
                        original = null;
                        Require(HostCount429(array!, rootsName) == 1 && !((Task)Get(array!, retiredName)).IsCompleted);
                        using (var pin = (IDisposable)Call(array!, Acquire429)!)
                            Require((ulong)Get(Get(pin, Value429), Length429) == 1UL);
                        ((IDisposable)Get(returnedReference, hostName)).Dispose();
                        await ((Task)Get(array!, retiredName)).WaitAsync(TimeSpan.FromSeconds(DeadlineSeconds));
                        Require(HostCount429(array!, rootsName) == 0 && (bool)Field429(array!, Freed429));
                        // A separate actual allocation drives the losing attempt.
                        original = await NewReference429(runtime, current, counters);
                        array = Get(Get(original, backingName), Array429);
                        _ = Call(
                            tracing,
                            Set,
                            current,
                            Term.A(SequenceTraceNames.Label),
                            original
                        );
                        Require(HostCount429(array!, rootsName) == 1);

                        return true;
                    });
                    var next = (BitString)CallStatic(typeof(BitString), InitializeWritable429, 512)!;
                    object backingGate = Field429(Field429(next, BinaryBacking429), Gate429);
                    var held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    blocked = Task.Factory.StartNew(
                        () =>
                    {
                        lock (backingGate)
                        {
                            held.TrySetResult();
                            release.Task.GetAwaiter().GetResult();
                        }
                    },
                        CancellationToken.None,
                        TaskCreationOptions.LongRunning,
                        TaskScheduler.Default
                    );
                    await held.Task.WaitAsync(TimeSpan.FromSeconds(DeadlineSeconds));
                    int[] collections = [GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2)];
                    operation = actor.InvokeAsync(current =>
                    {
                        Require(ReferenceEquals(current, context) && GetNullable(current, CurrentOwned429) is null);
                        Term result = (Term)Call(
                            tracing,
                            Set,
                            current,
                            Term.A(SequenceTraceNames.Label),
                            next
                        )!;

                        return Task.FromResult(result);
                    });
                    // The real managed-backing monitor stops only the NEW label's
                    // later capture, after previous-result Host custody was created.
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    while (HostCount429(array!, rootsName) != 2)
                    {
                        Require(!operation.IsCompleted && watch.Elapsed < TimeSpan.FromSeconds(DeadlineSeconds));
                        await Task.Yield();
                    }
                    Require(!operation.IsCompleted);
                    object preparationHeap = Get(context, Heap);
                    object mailboxOwner = Field429(runtime, MailboxOwnerField450);
                    Require(ReferenceEquals(context.Runtime, runtime));
                    object payloadOwner = Get(runtime, PayloadOwner429);
                    Require(ReferenceEquals(Get(payloadOwner, PayloadRuntime429), runtime));
                    Task halted = runtime.RequestImmediateHalt(0);
                    release.TrySetResult();
                    await blocked.WaitAsync(TimeSpan.FromSeconds(DeadlineSeconds));
                    bool refused = false;
                    try
                    {
                        await operation.WaitAsync(TimeSpan.FromSeconds(DeadlineSeconds));
                    }
                    catch (OperationCanceledException)
                    {
                        refused = true;
                    }
                    catch (Exception error) when (error.GetType().Name == HaltTransfer429)
                    {
                        refused = true;
                    }
                    catch (ObjectDisposedException error) when (IsPreparationHaltRefusal450(
                        error,
                        runtime,
                        context,
                        preparationHeap,
                        payloadOwner,
                        mailboxOwner
                    ))
                    {
                        refused = true;
                    }
                    Require(refused);
                    // No GC/finalization fallback may explain the lost-copy release.
                    Require(collections.SequenceEqual(new[] { GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2) }));
                    Require(HostCount429(array!, rootsName) == 1);
                    // Only now dispose the original user-owned Host carrier. The
                    // actual native array destructor/free must settle before VM join.
                    ((IDisposable)Get(original!, hostName)).Dispose();
                    original = null;
                    await ((Task)Get(array!, retiredName)).WaitAsync(TimeSpan.FromSeconds(DeadlineSeconds));
                    Require(HostCount429(array!, rootsName) == 0 && (bool)Field429(array!, Freed429));
                    await halted.WaitAsync(TimeSpan.FromSeconds(DeadlineSeconds));
                    await runtime.HaltQuiescence.WaitAsync(TimeSpan.FromSeconds(DeadlineSeconds));
                    completed = true;
                }
                finally
                {
                    release.TrySetResult();
                    if (blocked is not null) await blocked.WaitAsync(TimeSpan.FromSeconds(DeadlineSeconds));
                    if (operation is not null)
                    {
                        try
                        {
                            Term result = await operation.WaitAsync(TimeSpan.FromSeconds(DeadlineSeconds));
                            if (result is TupleTerm tuple)
                                (GetNullable(tuple.Items[LabelIndex], hostName) as IDisposable)?.Dispose();
                            else
                                (GetNullable(result, hostName) as IDisposable)?.Dispose();
                        }
                        catch (Exception) when (operation.IsCompleted) { }
                    }
                    if (original is not null) ((IDisposable)Get(original, hostName)).Dispose();
                    if (!completed)
                    {
                        // Failure cleanup only: an old losing copy may be hidden
                        // behind its finalizer. This is NEVER acceptance evidence.
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                }
            }
    }

    private static bool IsPreparationHaltRefusal450(
        ObjectDisposedException error,
        ProcessRuntime runtime,
        ProcessContext context,
        object heap,
        object payloadOwner,
        object mailboxOwner
    )
    {
        // Only the exact real preparation command above calls this AFTER its
        // explicit halt. Check its own context/heap and exact failing methods;
        // unrelated stale resources/descriptors/frames are never acceptable.
        if (!ReferenceEquals(context.Runtime, runtime)
            || !ReferenceEquals(Field429(context, HeapField450), heap)
            || !ReferenceEquals(Get(heap, HeapRuntime450), runtime))
            return false;
        System.Diagnostics.StackFrame[] frames = new System.Diagnostics.StackTrace(error).GetFrames();
        if (!frames.Any(frame => frame.GetMethod() is { } method
            && method.DeclaringType == Get(runtime, Tracing).GetType()
            && method.Name == LabelPreparation450))
            return false;
        if (IsPayloadHaltRefusal429(error, runtime, payloadOwner))
            return true;
        if (ReferenceEquals(Field429(runtime, MailboxOwnerField450), mailboxOwner)
            && error.ObjectName == mailboxOwner.GetType().FullName
            && frames.Any(frame => frame.GetMethod() is { } method
                && method.DeclaringType == mailboxOwner.GetType() && method.Name == MailboxCapture450))
        {
            lock (Field429(mailboxOwner, Gate450))
                if ((bool)Field429(mailboxOwner, MailboxClosed450)) return true;
        }
        bool closing;
        lock (Get(heap, Gate450))
            closing = (bool)Field429(heap, HeapClosing450);

        // RetireAsync closes further Enter admissions, including the nested
        // PrepareHeapImport staging Enter, but DOES NOT dispose active frames.
        // Therefore stale-frame/block/backing ODEs remain failures here.
        return closing && error.ObjectName == heap.GetType().FullName
            && frames.Any(frame => frame.GetMethod() is { } method
                && method.DeclaringType == heap.GetType() && method.Name == HeapEnter450);
    }

    private static bool IsPayloadHaltRefusal429(
        ObjectDisposedException error,
        ProcessRuntime runtime,
        object owner
    )
    {
        // The exact owner was captured BEFORE our real halt request. Do not
        // accept arbitrary disposed backing or any unrelated preparation error.
        if (!ReferenceEquals(Field429(runtime, PayloadOwnerField429), owner)
            || !ReferenceEquals(Get(owner, PayloadRuntime429), runtime)
            || error.ObjectName != typeof(string).FullName)
            return false;

        bool closed;
        lock (Field429(owner, Gate429))
            closed = (bool)Field429(owner, PayloadStopped429) || (bool)Field429(owner, PayloadDrained429);
        if (!closed)
            return false;

        return new System.Diagnostics.StackTrace(error).GetFrames().Any(frame =>
            frame.GetMethod() is { } method
            && method.DeclaringType == owner.GetType()
            && method.Name == PayloadReserve429);
    }

    private static async ValueTask<Term> NewReference429(ProcessRuntime runtime, ProcessContext context, bool counters)
    {
        if (counters)
            return (Term)CallStatic(
                typeof(ProcessRuntime).Assembly.GetType(CountersProducer429, true)!,
                CountersCreate429,
                runtime,
                Get(runtime, CountersOwner429),
                context,
                1UL
            )!;

        object binding = Activator.CreateInstance(
            typeof(ProcessRuntime).Assembly.GetType(AtomicsProducer429, true)!,
            Members,
            null,
            [runtime, runtime.Modules],
            null
        )!;

        return await ((ErlangFunction)Get(binding, ProducerBody429))(context, [Term.I(1), Term.I(0)]);
    }

    private static long HostCount429(object array, string rootsName)
    {
        lock (Field429(array, Gate429)) return (long)Field429(array, rootsName);
    }

    private static object Field429(object value, string name)
        => value.GetType().GetField(name, Members)!.GetValue(value)!;

    private static long CollectNow(object heap)
        => (long)Get(Call(
            heap,
            Collect,
            true,
            (ushort)0
        )!, Reclaimed);

    private static long ReadWords(object heap) => (long)Get(Get(heap, Words), LiveWords);

    private static object Get(object value, string name) => GetNullable(value, name)!;

    private static object? GetNullable(object value, string name)
        => value.GetType().GetProperty(name, Members)!.GetValue(value);

    private static object? Call(object value, string name, params object?[] arguments)
        => Invoke(value.GetType().GetMethod(name, Members)!, value, arguments);

    private static object? CallStatic(Type type, string name, params object?[] arguments)
        => Invoke(type.GetMethod(name, Members)!, null, arguments);

    private static object? Invoke(MethodInfo method, object? instance, object?[] arguments)
    {
        try
        {
            return method.Invoke(instance, arguments);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }
}
