using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Erlang.Tests;

internal static class DistributionStatisticsHostChecks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        await using var foreign = new ProcessRuntime();
        await using var kernel = new RuntimeNodeFoundationHostActor(runtime);
        await using var controller = new RuntimeNodeFoundationHostActor(runtime);
        await using var other = new RuntimeNodeFoundationHostActor(runtime);
        var pending = runtime.DistributionPending;
        var remote = Term.A(DistributionStatisticsHostNames.Remote);
        Term handle = pending.BeginPending(remote);
        Zero(Statistics(pending, handle));
        Reject(pending, Nil.Value);
        Reject(pending, remote);
        Reject(pending, controller.Process.Pid);
        Reject(pending, runtime.MakeReference());
        Reject(foreign.DistributionPending, handle);
        var tuple = (TupleTerm)handle;
        Zero(Statistics(pending, Term.Tuple(
            new Integer(((Integer)tuple.Items[0]).Value + ((System.Numerics.BigInteger)uint.MaxValue + 1)),
            tuple.Items[1]
        )));
        Reject(pending, Term.Tuple(tuple.Items[0], runtime.MakeReference()));
        Require(pending.AbortPending(handle));
        Zero(Statistics(pending, handle));
        Term replacement = pending.BeginPending(remote);
        Require(!replacement.Equals(handle));
        Reject(pending, handle);
        handle = replacement;
        await kernel.InvokeAsync(async context =>
        {
            runtime.Register(DistributionStatisticsHostNames.Kernel, context.Self);
            Require((await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                DistributionStatisticsHostNames.SetNode,
                Term.A(DistributionStatisticsHostNames.Local),
                Term.I(0)
            )).Equals(Term.A(ErlangBooleanAtoms.True)));

            return true;
        });
        Term opaque = await controller.InvokeAsync(async context => await DistributionControllerAdapters.SetNode(
            context,
            remote,
            context.Self,
            Term.Tuple(new Integer(DistributionPassThroughFlags.Mandatory), Term.I(1))
        ));
        Require(opaque is TupleTerm && opaque.Equals(handle));
        Zero(Statistics(pending, opaque));
        Reject(pending, ((TupleTerm)opaque).Items[1]);
        await other.Invoke(_ =>
        {
            Zero(Statistics(pending, handle));

            return true;
        });
        await controller.Invoke(context =>
        {
            pending.SetConnectedInputHandler(handle, context, context.Self);
            Require(DistributionInputAdapter.PutData(context, opaque, Nil.Value).Equals(Term.A(CommonAtomNames.Ok)));
            Require(DistributionInputAdapter.PutData(context, opaque, new BitString(Array.Empty<byte>())).Equals(Term.A(CommonAtomNames.Ok)));
            try
            {
                DistributionInputAdapter.PutData(context, opaque, Term.I(1));
                throw new InvalidOperationException(DistributionStatisticsHostNames.Failure);
            }
            catch (ErlangException failure) when (failure.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
            {
            }
            Require(Values(Statistics(pending, handle)) == (3, 0, 0));
            Require(pending.ConnectedSnapshot(handle).AcceptedInputPackets == 0);
            byte[] input = DistributionPassThroughCodec.Encode(DistributionPassThroughCodec.Send(kernel.Process.Pid, Term.A(DistributionStatisticsHostNames.Payload)));
            Require(DistributionInputAdapter.PutData(context, opaque, new BitString(input)).Equals(Term.A(CommonAtomNames.Ok)));
            Require(Values(Statistics(pending, handle)) == (4, 0, 0));
            Require(pending.ConnectedSnapshot(handle).AcceptedInputPackets == 1);

            return true;
        });
        var target = new Pid(DistributionStatisticsHostNames.Remote, 1, 1);
        var message = DistributionPassThroughCodec.Send(target, Term.A(DistributionStatisticsHostNames.Payload));
        pending.EnqueueConnectedOutput(handle, message);
        var first = Values(Statistics(pending, handle));
        long payloadBytes = pending.ConnectedSnapshot(handle).QueuedPacketBytes;
        Require(first.Input == 4 && first.Output == 0 && first.Pending > payloadBytes && payloadBytes > 0);
        pending.EnqueueConnectedOutput(handle, message);
        Require(Values(Statistics(pending, handle)) == (4, 0, checked(first.Pending * 2)));
        Require(pending.ConnectedSnapshot(handle).QueuedPacketBytes == checked(payloadBytes * 2));
        await controller.Invoke(context =>
        {
            Require(pending.TakeConnectedOutput(handle, context, out _) is not null);
            Require(Values(Statistics(pending, handle)) == (4, 1, first.Pending));
            Require(pending.TakeConnectedOutput(handle, context, out _) is not null);
            Require(Values(Statistics(pending, handle)) == (4, 2, 0));
            Require(pending.TakeConnectedOutput(handle, context, out _) is null);
            Require(Values(Statistics(pending, handle)) == (4, 2, 0));

            return true;
        });
        pending.EnqueueConnectedOutput(handle, message);
        Require(Values(Statistics(pending, handle)).Pending == first.Pending);
        pending.CloseConnected(handle);
        Zero(Statistics(pending, handle));
        replacement = pending.BeginPending(remote);
        Reject(pending, handle);
        Zero(Statistics(pending, replacement));
        Require(pending.AbortPending(replacement));
        Zero(Statistics(pending, replacement));
        OwnsSnapshot();
    }

    private static void OwnsSnapshot()
    {
        byte[] source = [1, 2, 3];
        ReadOnlyMemory<byte>[] segments = [source];
        var buffer = new HostDistributionBuffer(segments);
        source[0] = 0;
        segments[0] = ReadOnlyMemory<byte>.Empty;
        Require(buffer.PayloadBytes == 3 && buffer.SegmentCount == 1 && buffer.Segments[0].ToArray()[0] == 1);
        Require(buffer.Segments is not BitString[]);
    }

    private static Term Statistics(RuntimeDistributionPending pending, Term handle)
    {
        try
        {
            return (Term)typeof(RuntimeDistributionPending).GetMethod(DistributionStatisticsHostNames.Statistics, DistributionStatisticsHostNames.Members)!.Invoke(pending, [handle])!;
        }
        catch (TargetInvocationException failure) when (failure.InnerException is { } inner)
        {
            ExceptionDispatchInfo.Capture(inner).Throw();
            throw;
        }
    }

    private static (long Input, long Output, long Pending) Values(Term result)
    {
        Require(result is TupleTerm { Items.Count: 4 });
        var tuple = (TupleTerm)result;
        Require(tuple.Items[0].Equals(Term.A(CommonAtomNames.Ok)));

        return ((long)((Integer)tuple.Items[1]).Value, (long)((Integer)tuple.Items[2]).Value, (long)((Integer)tuple.Items[3]).Value);
    }

    private static void Zero(Term result) => Require(Values(result) == (0, 0, 0));

    private static void Reject(RuntimeDistributionPending pending, Term handle)
    {
        try
        {
            Statistics(pending, handle);
        }
        catch (ErlangException failure) when (failure.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
        {
            return;
        }
        throw new InvalidOperationException(DistributionStatisticsHostNames.Failure);
    }

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(DistributionStatisticsHostNames.Failure);
    }
}
