using Erlang;
namespace Erlang.Tests;

internal static class InternalSystemCheck431HostAccess
{
    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(InternalSystemCheck431HostNames.Failure);
    }

    internal static ErlangFunction Body(ProcessRuntime runtime)
    {
        object?[] args = [runtime, InternalSystemCheck431HostNames.Module, InternalSystemCheck431HostNames.Function, 1, null];
        var type = typeof(ProcessRuntime).Assembly.GetType(InternalSystemCheck431HostNames.NativeType, true)!;
        Require((bool)type.GetMethod(InternalSystemCheck431HostNames.Capture, InternalSystemCheck431HostNames.Flags)!.Invoke(null, args)!);

        return (ErlangFunction)args[4]!;
    }

    internal static async Task Initialize(ProcessRuntime runtime)
    {
        await runtime.InitializeModulesAsync(registry =>
        {
            var image = registry.PrepareBeam(
                InternalSystemCheckOriginal431Access.Resource(InternalSystemCheckOriginal431Names.FacetBeam),
                new BeamDependencySelection([InternalSystemCheck431HostNames.Module, InternalSystemCheckOriginal431Names.Runtime])
            );
            Require(registry.PublishPreparedBeam([image]).Status == BeamPublicationStatus.Published);
            Require(ReferenceEquals(registry.SourceOwner(InternalSystemCheck431HostNames.Module, InternalSystemCheck431HostNames.Gather, 1), image));
        });
    }

    internal static Task BlockLane(ProcessRuntime runtime, TaskCompletionSource entered, ManualResetEventSlim release)
    {
        object dispatcher = typeof(ProcessRuntime).GetField(InternalSystemCheck431HostNames.Dispatcher, InternalSystemCheck431HostNames.Flags)!.GetValue(runtime)!;
        Action<int> body = lane =>
        {
            if (lane != InternalSystemCheck431HostNames.HeldLane) return;
            entered.TrySetResult();
            if (!release.Wait(InternalSystemCheck431HostNames.Deadline)) throw new TimeoutException(InternalSystemCheck431HostNames.Failure);
        };
        object batch = dispatcher.GetType().GetMethod(InternalSystemCheck431HostNames.Prepare, InternalSystemCheck431HostNames.Flags)!.Invoke(dispatcher, [body])!;
        batch.GetType().GetMethod(InternalSystemCheck431HostNames.Publish, InternalSystemCheck431HostNames.Flags)!.Invoke(batch, null);

        return (Task)batch.GetType().GetProperty(InternalSystemCheck431HostNames.Completion, InternalSystemCheck431HostNames.Flags)!.GetValue(batch)!;
    }

    internal static IReadOnlyList<Term> Snapshot(ProcessContext context) => (IReadOnlyList<Term>)typeof(Mailbox).GetMethod(InternalSystemCheck431HostNames.Snapshot, InternalSystemCheck431HostNames.Flags)!.Invoke(context.Mailbox, null)!;

    internal static async Task WaitForAck(ProcessContext context)
    {
        long started = Environment.TickCount64;
        while (Environment.TickCount64 - started < InternalSystemCheck431HostNames.Deadline.TotalMilliseconds)
        {
            var messages = Snapshot(context);
            if (messages.Count(value => value is ReferenceTerm) == 1) return;
            await Task.Delay(1);
        }
        throw new TimeoutException(InternalSystemCheck431HostNames.Failure);
    }
}
