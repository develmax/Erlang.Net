using Erlang;
using System.Reflection;

namespace Erlang.Tests;

internal static class DistributionDeferredReasonHostChecks
{
    internal static async Task Run()
    {
        await CheckPrivateCallbacks();
        await CheckRealConnectionGeneration();
    }

    private static async Task CheckPrivateCallbacks()
    {
        await using var runtime = new ProcessRuntime();
        Term reason = Term.Tuple(Term.A(DistributionDeferredReasonHostNames.Payload), Term.I(7));
        byte[] bytes = ExternalTermFormat.Encode(reason);
        object owned = Create(runtime, bytes);
        Array.Clear(bytes);
        int eligible = 0, failed = 0;
        EtfDecodedTerm? decoded = await Prepare(owned, () =>
 {
     eligible++;

     return true;
 }, _ => failed++);
        Check(decoded is not null && decoded.Term.Equals(reason));
        Check(eligible == 1 && failed == 0);
        Check(ReferenceEquals(
            decoded,
            await Prepare(owned, () => throw new InvalidOperationException(DistributionDeferredReasonHostNames.Failure), _ => failed++)
        ));
        Check(eligible == 1 && failed == 0);
        object dropped = Create(runtime, new byte[] { DistributionDeferredReasonHostNames.EtfVersion });
        Check(await Prepare(dropped, () => false, _ => failed++) is null);
        Check(failed == 0);
        object malformed = Create(runtime, new byte[] { DistributionDeferredReasonHostNames.EtfVersion });
        await Reject(Prepare(malformed, () => true, _ => failed++));
        Check(failed == 1);
        await Reject(Prepare(malformed, () => throw new InvalidOperationException(DistributionDeferredReasonHostNames.Failure), _ => failed++));
        Check(failed == 1);
        object callbackFailure = Create(runtime, ExternalTermFormat.Encode(reason));
        try
        {
            _ = await Prepare(callbackFailure, () => throw new InvalidOperationException(DistributionDeferredReasonHostNames.Failure), _ => failed++);
            Check(false);
        }
        catch (InvalidOperationException)
        {
            Check(failed == 1);
        }
        object disposed = Create(runtime, ExternalTermFormat.Encode(reason));
        ((IDisposable)disposed).Dispose();
        Check(await Prepare(disposed, () => throw new InvalidOperationException(DistributionDeferredReasonHostNames.Failure), _ => failed++) is null);
        object runtimeGate = typeof(ProcessRuntime).GetField(DistributionDeferredReasonHostNames.RuntimeGate,
            BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(runtime)!;
        object gated = Create(runtime, ExternalTermFormat.Encode(reason));
        lock (runtimeGate)
        {
            try
            {
                _ = Prepare(gated, () => true, _ => failed++);
                Check(false);
            }
            catch (TargetInvocationException exception) when (exception.InnerException is InvalidOperationException)
            {
                Check(true);
            }
        }
        Check((await Prepare(gated, () => true, _ => failed++))!.Term.Equals(reason));
        object mailboxGate = new();
        object additional = Create(runtime, ExternalTermFormat.Encode(reason), () => Monitor.IsEntered(mailboxGate));
        lock (mailboxGate)
        {
            try
            {
                _ = Prepare(additional, () => true, _ => failed++);
                Check(false);
            }
            catch (TargetInvocationException exception) when (exception.InnerException is InvalidOperationException)
            {
                Check(true);
            }
        }
        Check((await Prepare(additional, () => true, _ => failed++))!.Term.Equals(reason));
        Check(failed == 1);
    }

    private static async Task CheckRealConnectionGeneration()
    {
        await using var runtime = new ProcessRuntime();
        await using var kernel = new RuntimeNodeFoundationHostActor(runtime);
        await using var controller = new RuntimeNodeFoundationHostActor(runtime);
        await kernel.InvokeAsync(async context =>
        {
            runtime.Register(ConnectedDistributionHostNames.Kernel, context.Self);
            _ = await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ConnectedIngressHostNames.SetNode,
                Term.A(DistributionDeferredReasonHostNames.Local),
                Term.I(0)
            );

            return true;
        });
        var pending = runtime.DistributionPending;
        Term handle = pending.BeginPending(Term.A(DistributionDeferredReasonHostNames.Remote));
        await Adopt();
        Action<Exception> oldFailure = Capture(pending, handle);
        pending.CloseConnected(handle);
        handle = pending.BeginPending(Term.A(DistributionDeferredReasonHostNames.Remote));
        await Adopt();
        object stale = Create(runtime, new byte[] { DistributionDeferredReasonHostNames.EtfVersion });
        await Reject(Prepare(stale, () => true, oldFailure));
        using var state = pending.Acquire(handle);
        Check(state.Snapshot().State == DistributionPendingState.Connected);
        Check(!controller.Process.Completion.IsCompleted);
        Action<Exception> currentFailure = Capture(pending, handle);
        object current = Create(runtime, new byte[] { DistributionDeferredReasonHostNames.EtfVersion });
        await Reject(Prepare(current, () => true, currentFailure));
        Check(state.Snapshot().State == DistributionPendingState.Exiting);
        _ = controller.InvokeAsync(async context =>
        {
            _ = await context.ReceiveAsync<Term>(message => message, DistributionDeferredReasonHostNames.Deadline);

            return true;
        });
        Term termination = await controller.Process.Completion.WaitAsync(DistributionDeferredReasonHostNames.Deadline);
        Check(termination.Equals(Term.A(ProcessExitReasons.Killed)));

        Task<Term> Adopt() => controller.InvokeAsync(async context =>
            await DistributionControllerAdapters.SetNode(
                context,
                Term.A(DistributionDeferredReasonHostNames.Remote),
                context.Self,
                Term.Tuple(new Integer(DistributionPassThroughFlags.Mandatory | DistributionPassThroughFlags.ExitPayload), Term.I(0))
            ));
    }

    private static object Create(ProcessRuntime runtime, byte[] bytes, Func<bool>? additional = null)
        => typeof(ProcessRuntime).GetMethod(DistributionDeferredReasonHostNames.Factory,
            BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(runtime, [new ReadOnlyMemory<byte>(bytes), additional])!;

    private static Task<EtfDecodedTerm?> Prepare(object pending, Func<bool> predicate, Action<Exception> failure)
        => (Task<EtfDecodedTerm?>)pending.GetType().GetMethod(DistributionDeferredReasonHostNames.Prepare,
            BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(pending, [predicate, failure])!;

    private static Action<Exception> Capture(RuntimeDistributionPending pending, Term handle)
        => (Action<Exception>)typeof(RuntimeDistributionPending).GetMethod(DistributionDeferredReasonHostNames.CaptureFailure,
            BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(pending, [handle])!;

    private static async Task Reject(Task<EtfDecodedTerm?> attempt)
    {
        try
        {
            _ = await attempt;
            Check(false);
        }
        catch (ArgumentException)
        {
            Check(true);
        }
    }

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(DistributionDeferredReasonHostNames.Failure);
    }
}
