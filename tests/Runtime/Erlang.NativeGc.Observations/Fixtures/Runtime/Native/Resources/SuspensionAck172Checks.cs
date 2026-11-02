using System.Runtime.InteropServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.NativeResource524Names;

namespace Erlang.Tests;

// Real suspend_process ACKs/cancellation with actual native payload/image roots.
// The old821 reserve/admit/race controls remain in the same permanent mode.
internal static class SuspensionAck172Checks
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Destructor(nint environment, nint payload);

    internal static async Task Run(bool owned)
    {
        await SuspensionAck173TraceChecks.Run(owned);
        foreach (int mode in new[] { SuspensionAck172Names.Ack, SuspensionAck172Names.RequesterDeath,
            SuspensionAck172Names.TargetDeath, SuspensionAck172Names.VmDispose })
            await One(owned, mode);
    }

    private static async Task One(bool owned, int mode)
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create(owned);
        var provider = Provider(runtime);
        int destroyed = 0;
        Destructor destructor = (_, _) => Interlocked.Increment(ref destroyed);
        object payload = Allocate(runtime, provider.Provider, Marshal.GetFunctionPointerForDelegate(destructor));
        Marshal.WriteByte(
            (nint)Property(payload, SuspensionRootLease821Names.Pointer),
            SuspensionAck172Names.SentinelIndex,
            SuspensionRootLease821Names.Sentinel
        );
        BitString binary = Binary(runtime, payload);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var target = runtime.Spawn(async context =>
        {
            entered.TrySetResult();
            await release.Task.WaitAsync(context.Cancellation);
            if (mode == SuspensionAck172Names.TargetDeath)
                throw new ErlangException(Term.A(SuspensionAck172Names.TargetExit));
            await context.ExecutionCheckpoint();

            return Nil.Value;
        });
        await using var requester = new RuntimeNodeFoundationHostActor(runtime);
        bool nativeReleased = false;
        Task? imageRetirement = null;
        try
        {
            await entered.Task.WaitAsync(Deadline);
            await requester.InvokeAsync(async context =>
            {
                using var host = new NativeTermHostScope524(runtime);
                Term response = await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    SuspensionAck172Names.Suspend,
                    target.Pid,
                    Term.List(Term.Tuple(Term.A(SuspensionAck172Names.Asynchronous), binary))
                );
                Check(response.Equals(Term.A(SuspensionAck172Names.True)));
                Call(
                    Property(context, SuspensionAck172Names.Heap),
                    SuspensionAck172Names.Collect,
                    true,
                    SuspensionAck172Names.FullSweep
                );

                return Nil.Value;
            });
            // The actual request has returned; the target has not checkpointed.
            // Remove the producer and request real provider-image retirement.
            Call(payload, SuspensionRootLease821Names.ReleaseNative);
            nativeReleased = true;
            imageRetirement = (Task)Call(provider.Bindings, SuspensionRootLease821Names.RetireImage)!;
            Check(destroyed == SuspensionAck172Names.NotDestroyed && !imageRetirement.IsCompleted && !provider.Owner.Disposed);
            Check(binary.ToArray()[SuspensionAck172Names.SentinelIndex] == SuspensionRootLease821Names.Sentinel);
            if (mode == SuspensionAck172Names.RequesterDeath)
            {
                await requester.DisposeAsync();
                // Cancellation must retire the ACK root while the VM is alive.
                await ((Task)Property(payload, SuspensionRootLease821Names.Retired)).WaitAsync(Deadline);
                await imageRetirement.WaitAsync(Deadline);
                Check(destroyed == SuspensionAck172Names.DestroyedOnce && provider.Owner.Disposed);
                release.TrySetResult();
                await target.Completion.WaitAsync(Deadline);
            }
            else if (mode == SuspensionAck172Names.VmDispose)
            {
                await runtime.DisposeAsync();
                await ((Task)Property(payload, SuspensionRootLease821Names.Retired)).WaitAsync(Deadline);
                await imageRetirement.WaitAsync(Deadline);
                Check(destroyed == SuspensionAck172Names.DestroyedOnce && provider.Owner.Disposed);
            }
            else
            {
                release.TrySetResult();
                await requester.InvokeAsync(async context =>
                {
                    using var host = new NativeTermHostScope524(runtime);
                    TupleTerm? ack = await context.ReceiveAsync(message => message as TupleTerm, Deadline);
                    Check(ack is not null && ack.Items.Count == SuspensionAck172Names.AckArity
                        && ack.Items[SuspensionAck172Names.TagIndex] is BitString actual
                        && actual.ToArray()[SuspensionAck172Names.SentinelIndex] == SuspensionRootLease821Names.Sentinel
                        && ack.Items[SuspensionAck172Names.StatusIndex].Equals(Term.A(mode == SuspensionAck172Names.TargetDeath
                            ? SuspensionAck172Names.Exited : SuspensionAck172Names.Suspended)));
                    if (mode == SuspensionAck172Names.Ack)
                        await runtime.Modules.Call(
                            context,
                            ErlangModuleNames.Module,
                            SuspensionAck172Names.Resume,
                            target.Pid
                        );

                    return Nil.Value;
                });
                await requester.InvokeAsync(context =>
                {
                    Call(
                        Property(context, SuspensionAck172Names.Heap),
                        SuspensionAck172Names.Collect,
                        true,
                        SuspensionAck172Names.FullSweep
                    );

                    return Task.FromResult<Term>(Nil.Value);
                });
                await target.Completion.WaitAsync(Deadline);
                await ((Task)Property(payload, SuspensionRootLease821Names.Retired)).WaitAsync(Deadline);
                await imageRetirement.WaitAsync(Deadline);
                Check(destroyed == SuspensionAck172Names.DestroyedOnce && provider.Owner.Disposed);
            }
        }
        finally
        {
            release.TrySetResult();
            if (!nativeReleased) Call(payload, SuspensionRootLease821Names.ReleaseNative);
            await runtime.DisposeAsync();
            await ((Task)Property(payload, SuspensionRootLease821Names.Retired)).WaitAsync(Deadline);
            imageRetirement ??= (Task)Call(provider.Bindings, SuspensionRootLease821Names.RetireImage)!;
            await imageRetirement.WaitAsync(Deadline);
            GC.KeepAlive(binary);
            GC.KeepAlive(destructor);
        }
    }
}
