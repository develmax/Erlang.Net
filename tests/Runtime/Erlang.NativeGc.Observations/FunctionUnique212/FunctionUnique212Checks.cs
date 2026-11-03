using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.NativeResource524Names;
using static Erlang.Tests.FunctionUnique212Names;

namespace Erlang.Tests;

internal static class FunctionUnique212Checks
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Destructor(nint environment, nint payload);

    internal static async Task Run()
    {
        var failures = new List<Exception>();
        foreach (bool owned in new[] { false, true })
        {
            try
            {
                await FunctionIdentityValue255Checks.Run(owned);
                await One(owned);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        }
        if (failures.Count != 0)
            throw new AggregateException(failures);
    }

    private static async Task One(bool owned)
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create(owned);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        var provider = Provider(runtime);
        int destroyed = 0, invoked = 0;
        Destructor destructor = (_, _) => Interlocked.Increment(ref destroyed);
        object payload = Allocate(runtime, provider.Provider, Marshal.GetFunctionPointerForDelegate(destructor));
        Marshal.Copy(
            Enumerable.Repeat(Sentinel, PayloadBytes).ToArray(),
            0,
            (nint)Property(payload, PayloadPointer),
            PayloadBytes
        );
        // Prepare a genuinely native resource INPUT before the UID value copy.
        // A short SliceBits could otherwise hide the input snapshot boundary.
        var unique = (BitString)Call(
            Property(runtime, Domain),
            BinaryMethod,
            payload,
            (nint)Property(payload, PayloadPointer),
            (nuint)UniqueBytes
        )!;
        Task retired = (Task)Property(payload, Retired);
        ModuleCodeDescriptor fingerprint = Fingerprint274Checks.Create(unique);
        ModuleCodeDescriptor? ownedFingerprint = null;
        var identity = new LocalFunctionIdentity(
            Term.A(IdentityModule),
            null,
            IdentityIndex,
            OldUnique,
            unique
        );
        var source = new FunctionTerm(
            identity,
            Arity,
            Array.Empty<Term>(),
            (_, _) =>
        {
            Interlocked.Increment(ref invoked);
            throw new InvalidOperationException(CallbackFailure);
        }
        );
        FunctionTerm? held = null;
        IDisposable? frame = null;
        bool released = false;
        Task? imageRetirement = null;
        Exception? primary = null;
        var cleanup = new List<Exception>();

        void RequireValueAfterRetirement()
        {
            var counts = ReadCounts(payload);
            Report(
                owned,
                beforeProducerRelease: false,
                payload,
                Volatile.Read(ref destroyed)
            );
            // Pinned OTP erl_fun.h stores uniq[16] INLINE; erl_fun.c copies it.
            // The source resource must be able to retire while the function lives.
            if (counts.Native != 0 || counts.Roots != 0 || !counts.Completed
                || Volatile.Read(ref destroyed) != 1 || !retired.IsCompleted
                || imageRetirement?.IsCompleted != true || !provider.Owner.Disposed)
                throw new InvalidOperationException(SourceRetirementFailure255 + JsonSerializer.Serialize(new
                {
                    Owned = owned,
                    counts.Native,
                    counts.Roots,
                    counts.Queued,
                    counts.Completed,
                    Destroyed = Volatile.Read(ref destroyed),
                    PayloadRetired = retired.IsCompleted,
                    ImageRetired = imageRetirement?.IsCompleted == true,
                    ProviderDisposed = provider.Owner.Disposed
                }));
            Fingerprint274Checks.Verify(fingerprint, Enumerable.Repeat(Sentinel, UniqueBytes).ToArray());
            Fingerprint274Checks.Verify(ownedFingerprint!, Enumerable.Repeat(Sentinel, UniqueBytes).ToArray());
            Fingerprint274Checks.PublishAndVerify(runtime, fingerprint, Enumerable.Repeat(Sentinel, UniqueBytes).ToArray());
            BitString actual = held!.LocalIdentity!.Unique;
            if (actual.BitLength != UniqueBytes * BitsPerByte
                || !actual.ToArray().SequenceEqual(Enumerable.Repeat(Sentinel, UniqueBytes))
                || Property(actual, SuspensionAck175Names.NativeIdentity) is not null
                || Volatile.Read(ref invoked) != 0)
                throw new InvalidOperationException(PreparationFailure);
        }

        try
        {
            await FunctionIdentityValue255Checks.ResourceInputs(runtime, actor, Binary(runtime, payload));
            ownedFingerprint = await Fingerprint274Checks.PhysicalResource(actor, unique);
            await actor.InvokeAsync(context =>
            {
                object heap = Property(context, Heap);
                frame = (IDisposable)Call(heap, Enter)!;
                held = (FunctionTerm)Call(
                    frame,
                    Adopt,
                    (int)Call(frame, Reserve)!,
                    source
                )!;
                if (ReadStorage(heap, held) is null || held.Environment.Count != 0
                    || !ReferenceEquals(held.LocalIdentity, identity)
                    || ReferenceEquals(held.LocalIdentity!.Unique, unique)
                    || !held.LocalIdentity.Unique.ToArray().SequenceEqual(Enumerable.Repeat(Sentinel, UniqueBytes)))
                    throw new InvalidOperationException(PreparationFailure);
                // No separate resource holder exists; UID bytes are immutable
                // by-value metadata, separate from the function Environment.
                CollectNow(context);
                Report(
                    owned,
                    beforeProducerRelease: true,
                    payload,
                    Volatile.Read(ref destroyed)
                );

                return Task.FromResult(true);
            });
            Call(payload, ReleaseNative);
            released = true;
            imageRetirement = (Task)Call(provider.Bindings, RetireImage)!;
            await retired.WaitAsync(Deadline);
            await imageRetirement.WaitAsync(Deadline);
            await actor.InvokeAsync(context =>
            {
                RequireValueAfterRetirement();
                CollectNow(context);
                RequireValueAfterRetirement();

                return Task.FromResult(true);
            });
        }
        catch (Exception error)
        {
            primary = error;
        }
        // Every cleanup is attempted even when the current regression is RED.
        // No arbitrary delay or CLR collection is a native lifetime witness.
        await Attempt(
            async () =>
        {
            await actor.InvokeAsync(context =>
            {
                IDisposable? current = frame;
                frame = null;
                current?.Dispose();

                return Task.FromResult(true);
            });
        },
            cleanup
        );
        await Attempt(async () =>
        {
            await actor.InvokeAsync(context =>
            {
                CollectNow(context);

                return Task.FromResult(true);
            });
        }, cleanup);
        if (!released)
            await Attempt(() =>
            {
                Call(payload, ReleaseNative);
                released = true;

                return Task.CompletedTask;
            }, cleanup);
        await Attempt(async () => await retired.WaitAsync(Deadline), cleanup);
        await Attempt(
            async () =>
        {
            imageRetirement ??= (Task)Call(provider.Bindings, RetireImage)!;
            await imageRetirement.WaitAsync(Deadline);
        },
            cleanup
        );
        // Prove live cleanup, not a destructor rescued by VM shutdown.
        await Attempt(
            async () =>
        {
            await actor.InvokeAsync(_ =>
            {
                if (!retired.IsCompleted || imageRetirement?.IsCompleted != true
                    || Volatile.Read(ref destroyed) != 1 || !provider.Owner.Disposed
                    || Volatile.Read(ref invoked) != 0)
                    throw new InvalidOperationException(FinalReleaseFailure);
                Console.WriteLine(LiveCleanupEvidence + JsonSerializer.Serialize(new
                {
                    Owned = owned,
                    Destroyed = Volatile.Read(ref destroyed),
                    Invoked = Volatile.Read(ref invoked),
                    PayloadRetired = retired.IsCompleted,
                    ImageRetired = imageRetirement!.IsCompleted,
                    ProviderDisposed = provider.Owner.Disposed
                }));

                return Task.FromResult(true);
            });
        },
            cleanup
        );
        await Attempt(async () => await runtime.DisposeAsync(), cleanup);
        if (Volatile.Read(ref destroyed) != 1 || !provider.Owner.Disposed || Volatile.Read(ref invoked) != 0)
            cleanup.Add(new InvalidOperationException(FinalReleaseFailure));
        Console.WriteLine(CleanupEvidence + JsonSerializer.Serialize(new
        {
            Owned = owned,
            Destroyed = Volatile.Read(ref destroyed),
            Invoked = Volatile.Read(ref invoked),
            PayloadRetired = retired.IsCompleted,
            ImageRetired = imageRetirement?.IsCompleted == true,
            ProviderDisposed = provider.Owner.Disposed,
            CleanupFailures = cleanup.Count
        }));
        GC.KeepAlive(unique);
        GC.KeepAlive(source);
        GC.KeepAlive(held);
        GC.KeepAlive(destructor);
        if (primary is not null)
        {
            if (cleanup.Count != 0)
                throw new AggregateException(new[] { primary }.Concat(cleanup));
            ExceptionDispatchInfo.Capture(primary).Throw();
        }
        if (cleanup.Count != 0)
            throw new AggregateException(cleanup);
    }

    private static async Task Attempt(Func<Task> action, List<Exception> failures)
    {
        try
        {
            await action();
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
    }

    private static object? ReadStorage(object heap, Term value)
        => heap.GetType().GetMethod(Storage, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!
            .Invoke(null, [value]);

    private static object Field(object target, string name)
        => target.GetType().GetField(name, Members)!.GetValue(target)!;

    private static (long Native, long Roots, bool Queued, bool Completed) ReadCounts(object payload)
    {
        lock (Field(payload, PayloadGate))
            return ((long)Field(payload, NativeCount), (long)Field(payload, RootCount),
                (bool)Field(payload, Queued), (bool)Field(payload, Completed));
    }

    private static void Report(
        bool owned,
        bool beforeProducerRelease,
        object payload,
        int destroyed
    )
    {
        var counts = ReadCounts(payload);
        Console.WriteLine(Evidence + JsonSerializer.Serialize(new
        {
            Owned = owned,
            BeforeProducerRelease = beforeProducerRelease,
            counts.Native,
            counts.Roots,
            counts.Queued,
            counts.Completed,
            Destroyed = destroyed
        }));
    }

    private static void CollectNow(ProcessContext context)
    {
        Call(
            Property(context, Heap),
            Collect,
            true,
            Major
        );
    }
}



