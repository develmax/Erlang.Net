using Erlang;
using System.Collections;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Erlang.Tests;

internal static class ClrMemoryPublicHostChecks
{
    internal static bool Owns(string module, string function, int arity)
        => module == ErlangModuleNames.Module && function == ClrMemoryPublicHostNames.Memory && arity is 0 or 1;

    internal static async Task RunMfa(int arity)
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        if (arity == 0)
            Coherent(await Call(actor));
        else
            Require(await Call(actor, Term.A(ClrMemoryPublicHostNames.Total)) is Integer { Value.Sign: > 0 });
    }

    internal static async Task Run()
    {
        var registry = new ModuleRegistry();
        CoreModules.Register(registry);
        Require(registry.Exports.Count(export => Owns(export.Module, export.Function, export.Arity)) == ClrMemoryPublicHostParameters.PublicFunctions);
        StorageIdentity();
        CodeBacking();
        WeakStorageLifetime();
        WeakCodeLifetime();
        await PublicQueries();
        await SlotsAndMailbox();
        await NativeRuntimeSources();
        await ConcurrentQueries();
        await CancelWhileNativeHeld();
    }

    private static Task<Term> Call(RuntimeNodeFoundationHostActor actor, params Term[] input)
        => actor.InvokeAsync(context => context.Runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            ClrMemoryPublicHostNames.Memory,
            input
        ).AsTask());

    private static Dictionary<string, BigInteger> Coherent(Term result)
    {
        var pairs = Cons.Items(result).Cast<TupleTerm>().ToArray();
        Require(pairs.Select(pair => ((Atom)pair.Items[0]).Name).SequenceEqual(ClrMemoryPublicHostNames.Order));
        var values = pairs.ToDictionary(pair => ((Atom)pair.Items[0]).Name, pair => ((Integer)pair.Items[1]).Value);
        Require(values.Values.All(value => value >= 0));
        Require(values[ClrMemoryPublicHostNames.Total] == values[ClrMemoryPublicHostNames.Processes] + values[ClrMemoryPublicHostNames.System]);
        Require(values[ClrMemoryPublicHostNames.Processes] >= values[ClrMemoryPublicHostNames.ProcessesUsed]);
        Require(values[ClrMemoryPublicHostNames.Atom] == values[ClrMemoryPublicHostNames.AtomUsed]);
        Require(values[ClrMemoryPublicHostNames.System] >= values[ClrMemoryPublicHostNames.Atom] + values[ClrMemoryPublicHostNames.Binary] + values[ClrMemoryPublicHostNames.Code]);
        Require(values[ClrMemoryPublicHostNames.Ets] == 0);

        return values;
    }

    private static async Task PublicQueries()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Require(Coherent(await Call(actor))[ClrMemoryPublicHostNames.Total] > 0);
        Require((await Call(actor, Nil.Value)).Equals(Nil.Value));
        var duplicate = Cons.Items(await Call(actor, Term.List(Term.A(ClrMemoryPublicHostNames.Total), Term.A(ClrMemoryPublicHostNames.Total)))).Cast<TupleTerm>().ToArray();
        Require(duplicate[0].Equals(duplicate[1]));
        var reversed = Cons.Items(await Call(actor, Term.List(Term.A(ClrMemoryPublicHostNames.Binary), Term.A(ClrMemoryPublicHostNames.Atom)))).Cast<TupleTerm>().ToArray();
        Require(reversed.Select(pair => ((Atom)pair.Items[0]).Name).SequenceEqual(new[] { ClrMemoryPublicHostNames.Binary, ClrMemoryPublicHostNames.Atom }));
        await Error(actor, Term.I(0), ErlangErrorReasons.BadArgument);
        await Error(actor, Term.A(ClrMemoryPublicHostNames.Unknown), ErlangErrorReasons.BadArgument);
        await Error(actor, Term.A(ClrMemoryPublicHostNames.Maximum), ErlangErrorReasons.BadArgument);
        await Error(actor, Term.List(Term.I(0)), ErlangErrorReasons.BadArgument);
        await Error(actor, new Cons(Term.A(ClrMemoryPublicHostNames.Total), Term.I(0)), ErlangErrorReasons.FunctionClause);
        await Error(actor, new Cons(Term.A(ClrMemoryPublicHostNames.Unknown), Term.I(0)), ErlangErrorReasons.BadArgument);
        Require(typeof(ProcessRuntime).GetField(ClrMemoryPublicHostNames.NativeDomain, ClrMemoryPublicHostNames.Members)!.GetValue(runtime) is null);
    }

    private static void StorageIdentity()
    {
        Require(Entry(ErlangBooleanAtoms.False) is not null);
        var binary = new BitString(new byte[ClrMemoryPublicHostParameters.Payload]);
        byte[] backing = (byte[])typeof(BitString).GetProperty(ClrMemoryPublicHostNames.Backing, ClrMemoryPublicHostNames.Members)!.GetValue(binary)!;
        object entry = Entry(backing)!;
        Require(Number(entry, ClrMemoryPublicHostNames.Bytes) == backing.Length);
        Require(Property(entry, ClrMemoryPublicHostNames.Kind).ToString() == ClrMemoryPublicHostNames.ObservedBinary);
        var equalContent = new BitString(new byte[ClrMemoryPublicHostParameters.Payload]);
        object equalBacking = typeof(BitString).GetProperty(ClrMemoryPublicHostNames.Backing, ClrMemoryPublicHostNames.Members)!.GetValue(equalContent)!;
        Require(!ReferenceEquals(backing, equalBacking) && !ReferenceEquals(entry, Entry(equalBacking)));
        var shared = new byte[ClrMemoryPublicHostParameters.Payload];
        MethodInfo adopt = typeof(BitString).GetMethod(ClrMemoryPublicHostNames.FromOwned, ClrMemoryPublicHostNames.Members)!;
        var adopted = (BitString)adopt.Invoke(null, [shared])!;
        var secondAdoption = (BitString)adopt.Invoke(null, [shared])!;
        Require(ReferenceEquals(
            typeof(BitString).GetProperty(ClrMemoryPublicHostNames.Backing, ClrMemoryPublicHostNames.Members)!.GetValue(adopted),
            shared
        ));
        object sharedEntry = Entry(shared)!;
        Require(Number(sharedEntry, ClrMemoryPublicHostNames.Bytes) == shared.LongLength);
        byte[] exported = binary.ToArray();
        Require(Entry(exported) is null);
        var partial = new BitString(new byte[1], ClrMemoryPublicHostParameters.PartialBits);
        var alias = (BitString)typeof(BitString).GetMethod(ClrMemoryPublicHostNames.ByteView, ClrMemoryPublicHostNames.Members)!.Invoke(partial, null)!;
        object firstBacking = typeof(BitString).GetProperty(ClrMemoryPublicHostNames.Backing, ClrMemoryPublicHostNames.Members)!.GetValue(partial)!;
        object secondBacking = typeof(BitString).GetProperty(ClrMemoryPublicHostNames.Backing, ClrMemoryPublicHostNames.Members)!.GetValue(alias)!;
        Require(!ReferenceEquals(firstBacking, secondBacking)
            && Entry(firstBacking) is not null && Entry(secondBacking) is not null
            && partial.ToArray().SequenceEqual(alias.ToArray()));
        var parent = new BitString(new byte[ClrMemoryPublicHostParameters.RetainedPayload]);
        BitString retained = parent.SliceBits(ClrMemoryPublicHostParameters.RetainedOffsetBits, ClrMemoryPublicHostParameters.RetainedSliceBits);
        object parentBacking = typeof(BitString).GetProperty(ClrMemoryPublicHostNames.Backing, ClrMemoryPublicHostNames.Members)!.GetValue(parent)!;
        object retainedBacking = typeof(BitString).GetProperty(ClrMemoryPublicHostNames.Backing, ClrMemoryPublicHostNames.Members)!.GetValue(retained)!;
        Require(ReferenceEquals(parentBacking, retainedBacking)
            && Entry(parentBacking) is not null
            && ReferenceEquals(Entry(parentBacking), Entry(retainedBacking)));
        string first = new(ClrMemoryPublicHostNames.DuplicateAtom.ToCharArray());
        string second = new(ClrMemoryPublicHostNames.DuplicateAtom.ToCharArray());
        var atom = Term.A(first);
        var duplicate = Term.A(second);
        Require(!ReferenceEquals(first, second) && Entry(first) is not null && Entry(second) is not null);
        Require(Number(Entry(second)!, ClrMemoryPublicHostNames.Bytes) == (BigInteger)second.Length * sizeof(char));
        var descriptor = new ModuleCodeDescriptor(
            [],
            [],
            new BitString(new byte[System.Security.Cryptography.MD5.HashSizeInBytes]),
            Nil.Value
        );
        GC.KeepAlive(descriptor);
        GC.KeepAlive(equalContent);
        GC.KeepAlive(adopted);
        GC.KeepAlive(secondAdoption);
        GC.KeepAlive(binary);
        GC.KeepAlive(parent);
        GC.KeepAlive(retained);
        GC.KeepAlive(partial);
        GC.KeepAlive(alias);
        GC.KeepAlive(atom);
        GC.KeepAlive(duplicate);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference EphemeralBacking()
    {
        var value = new BitString(new byte[ClrMemoryPublicHostParameters.Payload]);
        object backing = typeof(BitString).GetProperty(ClrMemoryPublicHostNames.Backing, ClrMemoryPublicHostNames.Members)!.GetValue(value)!;
        Require(Entry(backing) is not null);

        return new WeakReference(backing);
    }

    private static void WeakStorageLifetime()
    {
        WeakReference backing = EphemeralBacking();
        for (int attempt = 0; backing.IsAlive && attempt < ClrMemoryPublicHostParameters.CollectionAttempts; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        Require(!backing.IsAlive);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference EphemeralCode()
    {
        Assembly assembly = typeof(ClrMemoryPublicHostChecks).Assembly;
        string resource = assembly.GetManifestResourceNames().Single(name => name.EndsWith(ClrMemoryPublicHostNames.BeamResource, StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(resource)!;
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        PreparedBeamImage image = new ModuleRegistry().PrepareBeam(bytes.ToArray());

        return new WeakReference(image);
    }

    private static void WeakCodeLifetime()
    {
        WeakReference image = EphemeralCode();
        for (int attempt = 0; image.IsAlive && attempt < ClrMemoryPublicHostParameters.CollectionAttempts; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        Require(!image.IsAlive);
    }

    private static async Task SlotsAndMailbox()
    {
        await using var runtime = new ProcessRuntime(maximumProcesses: ClrMemoryPublicHostParameters.SmallProcessLimit);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext context = await actor.Ready.WaitAsync(ClrMemoryPublicHostParameters.Deadline);
        BigInteger before = await RuntimeField(runtime, ClrMemoryPublicHostNames.CapturedProcesses);
        runtime.Send(context.Self, Term.I(ClrMemoryPublicHostParameters.Payload));
        BigInteger queued = await RuntimeField(runtime, ClrMemoryPublicHostNames.CapturedProcesses);
        Require(queued > before);
        Require(context.Mailbox.RemoveFirst(message => message.Equals(Term.I(ClrMemoryPublicHostParameters.Payload))));
        Require(await RuntimeField(runtime, ClrMemoryPublicHostNames.CapturedProcesses) == before);
        object slots = typeof(ProcessRuntime).GetField(ClrMemoryPublicHostNames.Slots, ClrMemoryPublicHostNames.Members)!.GetValue(runtime)!;
        Require(Property(slots, ClrMemoryPublicHostNames.SlotBacking) is IList);
        for (int cycle = 0; cycle < ClrMemoryPublicHostParameters.SlotCycles; cycle++)
        {
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var process = runtime.Spawn(async context =>
            {
                entered.SetResult();
                await release.Task;

                return Nil.Value;
            });
            try
            {
                await entered.Task.WaitAsync(ClrMemoryPublicHostParameters.Deadline);
                Require(Number(slots, ClrMemoryPublicHostNames.Used) == ClrMemoryPublicHostParameters.SmallProcessLimit * IntPtr.Size);
                try
                {
                    runtime.Spawn(_ => new ValueTask<Term>(Nil.Value));
                    throw new InvalidOperationException(ClrMemoryPublicHostNames.Failure);
                }
                catch (ErlangException failure) when (failure.Reason.Equals(Term.A(ErlangErrorReasons.SystemLimit))) { }
                runtime.Send(process.Pid, Term.I(cycle));
                Coherent(await Call(actor));
            }
            finally
            {
                release.TrySetResult();
                await process.Completion.WaitAsync(ClrMemoryPublicHostParameters.Deadline);
            }
        }
        Require(Number(slots, ClrMemoryPublicHostNames.Allocated) < runtime.ProcessLimit * ClrMemoryPublicHostParameters.SlotCycles * IntPtr.Size);
    }

    private static async Task NativeRuntimeSources()
    {
        await using var first = new ProcessRuntime();
        await using var second = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(first);
        using var one = new NativePayloadProviderHostOwner(first);
        using var two = new NativePayloadProviderHostOwner(second);
        nint payload = one.Payload.Allocate(ClrMemoryPublicHostParameters.FirstNative);
        nint zero = one.Payload.Allocate(0);
        nint other = two.Payload.Allocate(ClrMemoryPublicHostParameters.SecondNative);
        Require(payload != 0 && zero != 0 && other != 0);
        Require(await RuntimeNative(first) == ClrMemoryPublicHostParameters.FirstNative + 1);
        Require(await RuntimeNative(second) == ClrMemoryPublicHostParameters.SecondNative);
        payload = one.Payload.Reallocate(payload, ClrMemoryPublicHostParameters.GrownNative);
        Require(await RuntimeNative(first) == ClrMemoryPublicHostParameters.GrownNative + 1);
        payload = one.Payload.Reallocate(payload, ClrMemoryPublicHostParameters.ShrunkNative);
        Require(await RuntimeNative(first) == ClrMemoryPublicHostParameters.ShrunkNative + 1);
        var all = Coherent(await Call(actor));
        Require(all[ClrMemoryPublicHostNames.System] >= all[ClrMemoryPublicHostNames.Atom] + all[ClrMemoryPublicHostNames.Binary] + all[ClrMemoryPublicHostNames.Code] + ClrMemoryPublicHostParameters.ShrunkNative + 1 + ClrMemoryPublicHostParameters.SecondNative);
        one.Payload.Free(payload);
        one.Payload.Free(zero);
        two.Payload.Free(other);
        Require(await RuntimeNative(first) == 0 && await RuntimeNative(second) == 0);
    }

    private static Task<BigInteger> RuntimeNative(ProcessRuntime runtime)
        => RuntimeField(runtime, ClrMemoryPublicHostNames.Native);

    private static async Task<BigInteger> RuntimeField(ProcessRuntime runtime, string field)
    {
        object pending = typeof(ProcessRuntime).GetMethod(ClrMemoryPublicHostNames.Capture, ClrMemoryPublicHostNames.Members)!.Invoke(runtime, [CancellationToken.None])!;
        var task = (Task)pending.GetType().GetMethod(ClrMemoryPublicHostNames.AsTask)!.Invoke(pending, null)!;
        await task.WaitAsync(ClrMemoryPublicHostParameters.Deadline);

        return Number(task.GetType().GetProperty(ClrMemoryPublicHostNames.Result)!.GetValue(task)!, field);
    }

    private static async Task ConcurrentQueries()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        using var stop = new CancellationTokenSource(ClrMemoryPublicHostParameters.Deadline);
        Task producer = Task.Run(
            async () =>
        {
            for (int index = 0; index < ClrMemoryPublicHostParameters.ConcurrentCycles; index++)
            {
                stop.Token.ThrowIfCancellationRequested();
                var binary = new BitString(new byte[ClrMemoryPublicHostParameters.Payload]);
                runtime.Send(actor.Process.Pid, binary);
                await Task.Yield();
                GC.KeepAlive(binary);
            }
        },
            stop.Token
        );
        try
        {
            for (int index = 0; index < ClrMemoryPublicHostParameters.ConcurrentCycles; index++)
            {
                stop.Token.ThrowIfCancellationRequested();
                Coherent(await Call(actor));
            }
        }
        finally
        {
            stop.Cancel();
            try
            {
                await producer.WaitAsync(ClrMemoryPublicHostParameters.Deadline);
            }
            catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
        }
    }

    private static void CodeBacking()
    {
        Assembly assembly = typeof(ClrMemoryPublicHostChecks).Assembly;
        string resource = assembly.GetManifestResourceNames().Single(name => name.EndsWith(ClrMemoryPublicHostNames.BeamResource, StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(resource)!;
        using var data = new MemoryStream();
        stream.CopyTo(data);
        var registry = new ModuleRegistry();
        PreparedBeamImage prepared = registry.PrepareBeam(data.ToArray());
        object descriptor = Property(prepared, ClrMemoryPublicHostNames.Descriptor);
        object functions = Property(Property(descriptor, ClrMemoryPublicHostNames.Functions), ClrMemoryPublicHostNames.Items);
        Require(Number(Entry(functions)!, ClrMemoryPublicHostNames.Bytes) > 0);
        object bodies = Property(Property(prepared, ClrMemoryPublicHostNames.Bodies), ClrMemoryPublicHostNames.Items);
        Require(Number(Entry(bodies)!, ClrMemoryPublicHostNames.Bytes) == ((Array)bodies).LongLength * IntPtr.Size);
        object linked = Property(prepared, ClrMemoryPublicHostNames.LinkedCode);
        object structure = Property(linked, ClrMemoryPublicHostNames.Structure);
        object chunk = ((IEnumerable)Property(structure, ClrMemoryPublicHostNames.Chunks)).Cast<object>().First();
        var payload = (ReadOnlyMemory<byte>)Property(chunk, ClrMemoryPublicHostNames.Payload);
        Require(System.Runtime.InteropServices.MemoryMarshal.TryGetArray(payload, out ArraySegment<byte> segment));
        Require(segment.Array is not null && Number(Entry(segment.Array)!, ClrMemoryPublicHostNames.Bytes) == segment.Array.LongLength);
        registry.PublishPreparedBeam(prepared);
        Require(registry.DeleteModule(prepared.Name).Equals(Term.A(ErlangBooleanAtoms.True)));
        Require(Number(Entry(functions)!, ClrMemoryPublicHostNames.Bytes) > 0);
        Require(ReferenceEquals(Entry(segment.Array!), Entry(segment.Array!)));
        // Observe an actual shared binary backing through the same code hook: binary wins.
        var binary = new BitString(new byte[ClrMemoryPublicHostParameters.Payload]);
        object backing = typeof(BitString).GetProperty(ClrMemoryPublicHostNames.Backing, ClrMemoryPublicHostNames.Members)!.GetValue(binary)!;
        Type tracker = typeof(Term).Assembly.GetType(ClrMemoryPublicHostNames.RegistryType, true)!;
        tracker.GetMethod(ClrMemoryPublicHostNames.TrackCode, ClrMemoryPublicHostNames.Members)!.MakeGenericMethod(typeof(byte)).Invoke(null, [backing]);
        Require(Property(Entry(backing)!, ClrMemoryPublicHostNames.Kind).ToString() == ClrMemoryPublicHostNames.ObservedBinary);
        GC.KeepAlive(binary);
        GC.KeepAlive(prepared);
    }

    private static async Task CancelWhileNativeHeld()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await using var killer = new RuntimeNodeFoundationHostActor(runtime);
        using var owner = new NativePayloadProviderHostOwner(runtime);
        Require(owner.Payload.Allocate(ClrMemoryPublicHostParameters.FirstNative) != 0);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task holder = Task.Run(() =>
        {
            object gate = owner.Domain.GetType().GetField(ClrMemoryPublicHostNames.Gate, ClrMemoryPublicHostNames.Members)!.GetValue(owner.Domain)!;
            lock (gate)
            {
                entered.TrySetResult();
                if (!release.Task.Wait(ClrMemoryPublicHostParameters.Deadline))
                    throw new TimeoutException(ClrMemoryPublicHostNames.Failure);
            }
            _ = owner.Domain.GetType().GetProperty(ClrMemoryPublicHostNames.Snapshot, ClrMemoryPublicHostNames.Members)!.GetValue(owner.Domain);
        });
        Task<Term>? query = null;
        try
        {
            await entered.Task.WaitAsync(ClrMemoryPublicHostParameters.Deadline);
            await Error(actor, Term.I(0), ErlangErrorReasons.BadArgument);
            query = Call(actor);
            using var deadline = new CancellationTokenSource(ClrMemoryPublicHostParameters.Deadline);
            while (owner.Domain.GetType().GetField(ClrMemoryPublicHostNames.Available, ClrMemoryPublicHostNames.Members)!.GetValue(owner.Domain) is null)
            {
                deadline.Token.ThrowIfCancellationRequested();
                await Task.Yield();
            }
            Require(!query.IsCompleted && !holder.IsCompleted);
            // This acquires the runtime gate while the query awaits the native gate.
            await killer.Invoke(context =>
            {
                runtime.Exit(context, actor.Process.Pid, Term.A(ClrMemoryPublicHostNames.Kill));

                return true;
            });
            try
            {
                await query.WaitAsync(ClrMemoryPublicHostParameters.Deadline);
                throw new InvalidOperationException(ClrMemoryPublicHostNames.Failure);
            }
            catch (OperationCanceledException) { }
            Require(!holder.IsCompleted);
        }
        finally
        {
            release.TrySetResult();
            await holder.WaitAsync(ClrMemoryPublicHostParameters.Deadline);
            if (query is not null)
            {
                try
                {
                    await query.WaitAsync(ClrMemoryPublicHostParameters.Deadline);
                }
                catch (OperationCanceledException) { }
            }
        }
    }

    private static object? Entry(object backing)
    {
        Type registry = typeof(Term).Assembly.GetType(ClrMemoryPublicHostNames.RegistryType, true)!;
        object entries = registry.GetField(ClrMemoryPublicHostNames.Entries, ClrMemoryPublicHostNames.Members)!.GetValue(null)!;
        object?[] arguments = [backing, null];

        return (bool)entries.GetType().GetMethod(ClrMemoryPublicHostNames.Lookup)!.Invoke(entries, arguments)! ? arguments[1] : null;
    }

    private static object Property(object value, string name)
        => value.GetType().GetProperty(name, ClrMemoryPublicHostNames.Members)?.GetValue(value)
            ?? value.GetType().GetField(name, ClrMemoryPublicHostNames.Members)!.GetValue(value)!;

    private static BigInteger Number(object value, string name)
        => Property(value, name) switch
        {
            BigInteger number => number,
            long number => number,
            _ => throw new InvalidOperationException(ClrMemoryPublicHostNames.Failure)
        };

    private static async Task Error(RuntimeNodeFoundationHostActor actor, Term query, string reason)
    {
        try
        {
            await Call(actor, query);
            throw new InvalidOperationException(ClrMemoryPublicHostNames.Failure);
        }
        catch (ErlangException failure) when (failure.Reason.Equals(Term.A(reason))) { }
    }

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(ClrMemoryPublicHostNames.Failure);
    }
}
