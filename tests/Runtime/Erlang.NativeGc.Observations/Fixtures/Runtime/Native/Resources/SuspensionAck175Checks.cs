using System.Collections;
using System.Runtime.InteropServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.NativeResource524Names;

namespace Erlang.Tests;

// Additional real ACK publication states, never a new native borrowing capability.
internal static class SuspensionAck175Checks
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Destructor(nint environment, nint payload);

    internal static async Task Run(bool owned)
    {
        foreach (int mode in new[] { SuspensionAck175Names.ActiveTarget, SuspensionAck175Names.MissingTarget,
            SuspensionAck175Names.UnreadRequesterDeath, SuspensionAck175Names.MixedUnique })
            await One(owned, mode);
    }

    private static object? ReadStorage175(object heap, Term value)
        => heap.GetType().GetMethod(
            SuspensionAck175Names.Storage,
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic
        )!.Invoke(null, [value]);

    private static async Task One(bool owned, int mode)
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create(owned);
        var provider = Provider(runtime);
        int destroyed = 0, invoked = 0;
        Destructor destructor = (_, _) => Interlocked.Increment(ref destroyed);
        object payload = Allocate(runtime, provider.Provider, Marshal.GetFunctionPointerForDelegate(destructor));
        Marshal.Copy(
            Enumerable.Repeat(SuspensionRootLease821Names.Sentinel, PayloadBytes).ToArray(),
            0,
            (nint)Property(payload, SuspensionRootLease821Names.Pointer),
            PayloadBytes
        );
        BitString binary = Binary(runtime, payload);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var target = runtime.Spawn(async context =>
        {
            entered.TrySetResult();
            if (mode != SuspensionAck175Names.MissingTarget)
            {
                await release.Task.WaitAsync(context.Cancellation);
                await context.ExecutionCheckpoint();
            }

            return Nil.Value;
        });
        await using var requester = new RuntimeNodeFoundationHostActor(runtime);
        RuntimeNodeFoundationHostActor? activator = null;
        Task? imageRetirement = null;
        bool nativeReleased = false;
        try
        {
            await entered.Task.WaitAsync(Deadline);
            if (mode == SuspensionAck175Names.MissingTarget)
                await target.Completion.WaitAsync(Deadline);
            if (mode == SuspensionAck175Names.ActiveTarget)
            {
                activator = new RuntimeNodeFoundationHostActor(runtime);
                await activator.InvokeAsync(async context =>
                {
                    Check((await runtime.Modules.Call(
                        context,
                        ErlangModuleNames.Module,
                        SuspensionAck175Names.Suspend,
                        target.Pid,
                        Term.List(Term.A(SuspensionAck175Names.Asynchronous))
                    )).Equals(Term.A(SuspensionAck175Names.True)));

                    return Nil.Value;
                });
                release.TrySetResult();
                await Until(() => (bool)Call(Property(runtime, SuspensionAck175Names.Suspensions),
                    SuspensionAck175Names.IsActive, target.Pid)!);
            }

            await requester.InvokeAsync(async context =>
            {
                if (mode == SuspensionAck175Names.MissingTarget)
                    Check((await runtime.Modules.Call(
                        context,
                        ErlangModuleNames.Module,
                        SuspensionAck175Names.Alive,
                        target.Pid
                    )).Equals(Term.A(SuspensionAck175Names.False)));
                object heap = Property(context, SuspensionAck175Names.Heap);
                using var frame = (IDisposable)Call(heap, SuspensionAck175Names.Enter)!;
                using var sourceHost = new NativeTermHostScope524(runtime);
                Term tag = binary;
                if (mode == SuspensionAck175Names.MixedUnique)
                {
                    int slot = (int)Call(frame, SuspensionAck175Names.Reserve)!;
                    var ordinary = (BitString)Call(
                        frame,
                        SuspensionAck175Names.Adopt,
                        slot,
                        new BitString(Enumerable.Repeat(SuspensionRootLease821Names.Sentinel, SuspensionAck175Names.OrdinaryBytes).ToArray())
                    )!;
                    // Public stored identity; it has no Source recipe/capability.
                    // UID bytes are a value snapshot. Keep the actual resource
                    // in Environment to prove the real ACK custody contract.
                    var identity = new LocalFunctionIdentity(
                        Term.A(SuspensionAck175Names.Mixed),
                        null,
                        0,
                        0,
                        (BitString)Call(
                            Property(runtime, Domain),
                            SuspensionAck175Names.ResourceBinary,
                            payload,
                            (nint)Property(payload, SuspensionRootLease821Names.Pointer),
                            (nuint)SuspensionAck175Names.UniqueBytes
                        )!
                    );
                    var function = new FunctionTerm(
                        identity,
                        0,
                        [ordinary, binary],
                        (_, _) =>
                    {
                        Interlocked.Increment(ref invoked);

                        return ValueTask.FromResult<Term>(Nil.Value);
                    }
                    );
                    tag = Term.Tuple(Term.A(SuspensionAck175Names.Mixed), function, ordinary);
                    // This is a Host call of an opaque BIF. Export actual cells
                    // through the real held frame; do not grant borrowed authority.
                    Check(ReadStorage175(heap, ordinary) is not null);
                    tag = (Term)Call(
                        heap,
                        SuspensionAck175Names.Export,
                        frame,
                        tag
                    )!;
                    var exported = (TupleTerm)tag;
                    var exportedFunction = (FunctionTerm)exported.Items[SuspensionAck175Names.FunctionIndex];
                    Check(ReadStorage175(heap, exported) is null
                        && ReadStorage175(heap, exportedFunction) is null
                        && ReadStorage175(heap, exportedFunction.Environment[0]) is null
                        && ReadStorage175(heap, exportedFunction.Environment[1]) is null
                        && ReadStorage175(heap, exported.Items[SuspensionAck175Names.OrdinaryIndex]) is null);
                    // OTP copies uniq[16] into its function entry. The opaque
                    // callback/record identity stays unchanged; UID metadata
                    // is managed by value and must not retain the payload.
                    Check(ReferenceEquals(exportedFunction.LocalIdentity, identity)
                        && ReferenceEquals(exportedFunction.LocalIdentity!.Unique, identity.Unique)
                        && ReferenceEquals(Property(exportedFunction.Environment[1], SuspensionAck175Names.NativeIdentity), payload)
                        && Property(identity.Unique, SuspensionAck175Names.NativeIdentity) is null
                        && identity.Unique.ToArray().SequenceEqual(Enumerable.Repeat(
                            SuspensionRootLease821Names.Sentinel, SuspensionAck175Names.UniqueBytes)));
                    ReadTag(tag, mode);
                }
                Check((await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    SuspensionAck175Names.Suspend,
                    target.Pid,
                    Term.List(Term.Tuple(Term.A(SuspensionAck175Names.Asynchronous), tag))
                ))
                    .Equals(Term.A(SuspensionAck175Names.True)));
                if (mode is SuspensionAck175Names.ActiveTarget or SuspensionAck175Names.MissingTarget)
                    Check(context.Mailbox.Count == 1);

                return Nil.Value;
            });
            await requester.InvokeAsync(context =>
            {
                Collect(context);

                return Task.FromResult<Term>(Nil.Value);
            });
            Call(payload, SuspensionRootLease821Names.ReleaseNative);
            nativeReleased = true;
            imageRetirement = (Task)Call(provider.Bindings, SuspensionRootLease821Names.RetireImage)!;
            Check(destroyed == 0 && !imageRetirement.IsCompleted && !provider.Owner.Disposed);
            Check(binary.ToArray()[0] == SuspensionRootLease821Names.Sentinel);
            release.TrySetResult();

            if (mode == SuspensionAck175Names.UnreadRequesterDeath)
            {
                // Actual mailbox publication, not a synthetic ACK notification.
                await Until(async () => await requester.InvokeAsync(context => Task.FromResult(context.Mailbox.Count == 1)));
                Check(destroyed == 0 && !provider.Owner.Disposed);
                await requester.DisposeAsync();
            }
            else
            {
                await requester.InvokeAsync(async context =>
                {
                    // Explicit Host custody ends after all native tag reads in this command.
                    using var host = new NativeTermHostScope524(runtime);
                    Atom? observed = await context.ReceiveAsync<Atom>(
                        message =>
                    {
                        if (message is not TupleTerm ack || ack.Items.Count != SuspensionAck175Names.AckArity)
                            return null;
                        Check(ack.Items[SuspensionAck175Names.StatusIndex].Equals(Term.A(mode == SuspensionAck175Names.MissingTarget
                            ? SuspensionAck175Names.BadArgument : SuspensionAck175Names.Suspended)));
                        Check(destroyed == 0 && !provider.Owner.Disposed);
                        ReadTag(ack.Items[SuspensionAck175Names.TagIndex], mode);

                        return Term.A(SuspensionAck175Names.True);
                    },
                        Deadline
                    );
                    Check(observed is { Name: SuspensionAck175Names.True });
                    if (mode != SuspensionAck175Names.MissingTarget)
                        Check((await runtime.Modules.Call(
                            context,
                            ErlangModuleNames.Module,
                            SuspensionAck175Names.Resume,
                            target.Pid
                        )).Equals(Term.A(SuspensionAck175Names.True)));

                    return Nil.Value;
                });
                if (activator is not null)
                    await activator.InvokeAsync(async context =>
                    {
                        Check((await runtime.Modules.Call(
                            context,
                            ErlangModuleNames.Module,
                            SuspensionAck175Names.Resume,
                            target.Pid
                        )).Equals(Term.A(SuspensionAck175Names.True)));

                        return Nil.Value;
                    });
                await requester.InvokeAsync(async context =>
                {
                    await AwaitReleasedMailbox(context);
                    Collect(context);

                    return Nil.Value;
                });
            }
            await target.Completion.WaitAsync(Deadline);
            await ((Task)Property(payload, SuspensionRootLease821Names.Retired)).WaitAsync(Deadline);
            await imageRetirement.WaitAsync(Deadline);
            Check(destroyed == 1 && provider.Owner.Disposed && invoked == 0);
        }
        finally
        {
            release.TrySetResult();
            if (!nativeReleased) Call(payload, SuspensionRootLease821Names.ReleaseNative);
            await runtime.DisposeAsync();
            if (activator is not null) await activator.DisposeAsync();
            await ((Task)Property(payload, SuspensionRootLease821Names.Retired)).WaitAsync(Deadline);
            imageRetirement ??= (Task)Call(provider.Bindings, SuspensionRootLease821Names.RetireImage)!;
            await imageRetirement.WaitAsync(Deadline);
            GC.KeepAlive(binary);
            GC.KeepAlive(destructor);
        }
    }

    private static void ReadTag(Term tag, int mode)
    {
        if (mode != SuspensionAck175Names.MixedUnique)
        {
            Check(tag is BitString binary && binary.ToArray()[0] == SuspensionRootLease821Names.Sentinel);

            return;
        }
        Check(tag is TupleTerm { Items.Count: SuspensionAck175Names.MixedArity });
        var tuple = (TupleTerm)tag;
        Check(tuple.Items[0].Equals(Term.A(SuspensionAck175Names.Mixed)));
        Check(tuple.Items[SuspensionAck175Names.FunctionIndex] is FunctionTerm);
        var function = (FunctionTerm)tuple.Items[SuspensionAck175Names.FunctionIndex];
        Check(function.LocalIdentity is { Unique: { } unique } && unique.BitLength == SuspensionAck175Names.UniqueBytes * SuspensionAck175Names.BitsPerByte
            && unique.ToArray().SequenceEqual(Enumerable.Repeat(SuspensionRootLease821Names.Sentinel, SuspensionAck175Names.UniqueBytes)));
        Check(function.Environment.Count == 2 && function.Environment[0] is BitString environment
            && environment.ToArray().SequenceEqual(Enumerable.Repeat(SuspensionRootLease821Names.Sentinel, SuspensionAck175Names.OrdinaryBytes)));
        Check(function.Environment[1] is BitString resource
            && Property(resource, SuspensionAck175Names.NativeIdentity) is not null
            && resource.ToArray().SequenceEqual(Enumerable.Repeat(SuspensionRootLease821Names.Sentinel, PayloadBytes)));
        Check(tuple.Items[SuspensionAck175Names.OrdinaryIndex] is BitString ordinary
            && ordinary.ToArray().SequenceEqual(Enumerable.Repeat(SuspensionRootLease821Names.Sentinel, SuspensionAck175Names.OrdinaryBytes)));
    }

    private static void Collect(ProcessContext context)
        => Call(
            Property(context, SuspensionAck175Names.Heap),
            SuspensionAck175Names.Collect,
            true,
            SuspensionAck175Names.FullSweep
        );

    private static async Task AwaitReleasedMailbox(ProcessContext context)
    {
        object tracker = Property(context.Mailbox, SuspensionAck175Names.Retirements);
        var pending = new List<Task>();
        lock (Field(tracker, SuspensionAck175Names.Gate))
            foreach (string list in new[] { SuspensionAck175Names.Active, SuspensionAck175Names.Failed })
                foreach (object work in (IEnumerable)Field(tracker, list))
                    pending.Add((Task)Property(Property(work, SuspensionAck175Names.Completion), SuspensionAck175Names.Task));
        await Task.WhenAll(pending).WaitAsync(Deadline);
    }

    private static object Field(object target, string name)
        => target.GetType().GetField(name, Members)!.GetValue(target)!;

    private static Task Until(Func<bool> condition) => Until(() => Task.FromResult(condition()));

    private static async Task Until(Func<Task<bool>> condition)
    {
        DateTime deadline = DateTime.UtcNow + Deadline;
        while (!await condition())
        {
            Check(DateTime.UtcNow < deadline);
            await Task.Yield();
        }
    }
}
