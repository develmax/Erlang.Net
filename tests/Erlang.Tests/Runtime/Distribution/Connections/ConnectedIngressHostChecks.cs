using System.Collections;
using System.Reflection;
using System.Text.Json;
using Erlang;
using Erlang.Tests;

namespace Erlang.Tests;

internal static class ConnectedIngressHostChecks
{
    internal static async Task Run()
    {
        int checks = 0;
        await using var runtime = new ProcessRuntime();
        await using var foreign = new ProcessRuntime();
        await using var kernel = new RuntimeNodeFoundationHostActor(runtime);
        await using var controller = new RuntimeNodeFoundationHostActor(runtime);
        await using var input = new RuntimeNodeFoundationHostActor(runtime);
        await using var recipient = new RuntimeNodeFoundationHostActor(runtime);
        await using var outsider = new RuntimeNodeFoundationHostActor(foreign);
        await kernel.InvokeAsync(async context =>
        {
            runtime.Register(ConnectedDistributionHostNames.Kernel, context.Self);
            Check((await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ConnectedIngressHostNames.SetNode,
                Term.A(ConnectedIngressHostNames.Local),
                Term.I(0)
            ))
                .Equals(Term.A(ErlangBooleanAtoms.True)));

            return true;
        });
        var pending = runtime.DistributionPending;
        var handle = pending.BeginPending(Term.A(ConnectedDistributionHostNames.Remote));
        await controller.Invoke(context =>
        {
            _ = pending.AdoptConnected(
                handle,
                context,
                DistributionPassThroughFlags.Mandatory,
                1
            );
            pending.SetConnectedInputHandler(handle, context, input.Process.Pid);

            return true;
        });
        object known = typeof(ProcessRuntime).GetProperty(ConnectedIngressHostNames.KnownNodes,
            BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(runtime)!;
        object epochGate = known.GetType().GetField(ConnectedIngressHostNames.Gate,
            BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(known)!;
        var reservations = (IDictionary)known.GetType().GetField(ConnectedIngressHostNames.ReservedEpochs,
            BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(known)!;
        var identity = new LocalFunctionIdentity(
            Term.A(ConnectedIngressHostNames.CookieModule),
            null,
            ConnectedIngressHostNames.FunctionIndex,
            ConnectedIngressHostNames.FunctionOldUnique,
            new BitString(new byte[ConnectedIngressHostNames.UniqueBytes])
        );
        Action? duringDecode = null;
        bool foreignEnvironment = false;
        _ = runtime.Functions.Register(
            identity,
            ConnectedIngressHostNames.FunctionArity,
            ConnectedIngressHostNames.FunctionEnvironment,
            environment =>
        {
            duringDecode?.Invoke();
            IEnumerable<Term> captured = foreignEnvironment ? [outsider.Process.Pid] : environment;

            return new FunctionTerm(
                identity,
                ConnectedIngressHostNames.FunctionArity,
                captured,
                (_, _) => ValueTask.FromResult<Term>(Nil.Value)
            );
        }
        );
        var cookie = runtime.Functions.Restore(
            runtime,
            identity,
            ConnectedIngressHostNames.FunctionArity,
            []
        );
        var creator = new Pid(
            ConnectedIngressHostNames.CreatorNode,
            ConnectedIngressHostNames.IdentifierNumber,
            ConnectedIngressHostNames.CreatorEpoch
        );
        var encoding = new EtfEncodingOptions { FunctionOwner = creator };
        var control = Term.Tuple(Term.I(DistributionPassThroughLayout.SendOpcode), cookie, recipient.Process.Pid);
        var payload = Term.Tuple(
            new Pid(
                ConnectedIngressHostNames.PayloadNode,
                ConnectedIngressHostNames.IdentifierNumber,
                ConnectedIngressHostNames.PayloadEpoch
            ),
            new PortTerm(
                ConnectedIngressHostNames.PayloadNode,
                ConnectedIngressHostNames.IdentifierNumber,
                ConnectedIngressHostNames.PayloadEpoch
            ),
            new ReferenceTerm(
                ConnectedIngressHostNames.PayloadNode,
                ConnectedIngressHostNames.IdentifierNumber,
                ConnectedIngressHostNames.PayloadEpoch
            )
        );
        byte[] frame = DistributionPassThroughCodec.Encode(new(control, payload), options: encoding);
        Check(!Reserved(ConnectedIngressHostNames.CreatorNode, ConnectedIngressHostNames.CreatorEpoch));
        Check(!Reserved(ConnectedIngressHostNames.PayloadNode, ConnectedIngressHostNames.PayloadEpoch));
        duringDecode = () =>
        {
            Check(Reserved(ConnectedIngressHostNames.CreatorNode, ConnectedIngressHostNames.CreatorEpoch));
            Check(Reserved(ConnectedIngressHostNames.PayloadNode, ConnectedIngressHostNames.PayloadEpoch));
        };
        await input.Invoke(context =>
        {
            pending.AcceptConnectedPassThroughInput(handle, context, frame);

            return true;
        });
        await Expect(payload);
        Check(!Reserved(ConnectedIngressHostNames.CreatorNode, ConnectedIngressHostNames.CreatorEpoch));
        Check(!Reserved(ConnectedIngressHostNames.PayloadNode, ConnectedIngressHostNames.PayloadEpoch));
        Check(pending.ConnectedSnapshot(handle).AcceptedInputPackets == 1);
        Term original = Term.A(ConnectedIngressHostNames.Before);
        Term replacement = Term.A(ConnectedIngressHostNames.After);
        byte[] callerFrame = DistributionPassThroughCodec.Encode(new(control, original), options: encoding);
        int payloadOffset = DistributionPassThroughLayout.MarkerBytes + ExternalTermFormat.Encode(control, encoding).Length;
        byte[] changedPayload = ExternalTermFormat.Encode(replacement);
        Check(payloadOffset + changedPayload.Length == callerFrame.Length);
        duringDecode = () => changedPayload.CopyTo(callerFrame, payloadOffset);
        await input.Invoke(context =>
        {
            pending.AcceptConnectedPassThroughInput(handle, context, callerFrame);

            return true;
        });
        Check(callerFrame.AsSpan(payloadOffset).SequenceEqual(changedPayload));
        await Expect(original);
        await ExpectNone();
        Check(pending.ConnectedSnapshot(handle).AcceptedInputPackets == 2);
        duringDecode = null;
        foreignEnvironment = true;
        await input.Invoke(context => Reject<ArgumentException>(() => pending.AcceptConnectedPassThroughInput(handle, context, frame)));
        foreignEnvironment = false;
        Check(pending.ConnectedSnapshot(handle).AcceptedInputPackets == 2);
        Check(!Reserved(ConnectedIngressHostNames.CreatorNode, ConnectedIngressHostNames.CreatorEpoch));
        Check(!Reserved(ConnectedIngressHostNames.PayloadNode, ConnectedIngressHostNames.PayloadEpoch));
        await ExpectNone();
        duringDecode = () => pending.CloseConnected(handle);
        await input.Invoke(context => Reject<InvalidOperationException>(() => pending.AcceptConnectedPassThroughInput(handle, context, frame)));
        Check(!Reserved(ConnectedIngressHostNames.CreatorNode, ConnectedIngressHostNames.CreatorEpoch));
        Check(!Reserved(ConnectedIngressHostNames.PayloadNode, ConnectedIngressHostNames.PayloadEpoch));
        await ExpectNone();
        using (var lease = pending.Acquire(handle))
            Check(lease.Snapshot().State == DistributionPendingState.Idle);
        if (checks != ConnectedIngressHostNames.ExpectedAssertions)
            throw new InvalidOperationException(ConnectedDistributionHostNames.Failure);
        Console.WriteLine(JsonSerializer.Serialize(new { Status = ConnectedIngressHostNames.Status, Checks = checks, Failed = 0, NativeExecuted = false }));

        bool Reserved(string node, uint creation)
        {
            lock (epochGate)
                return reservations.Contains((node, creation));
        }

        void Check(bool value)
        {
            checks++;
            if (!value)
                throw new Exception(ConnectedDistributionHostNames.Failure);
        }

        bool Reject<T>(Action operation) where T : Exception
        {
            try
            {
                operation();
            }
            catch (T)
            {
                checks++;

                return true;
            }
            throw new Exception(ConnectedDistributionHostNames.Failure);
        }

        async Task Expect(Term expected) => await recipient.InvokeAsync(async context =>
        {
            Check(await context.ReceiveAsync<Term>(message => message.Equals(expected) ? message : null, ConnectedDistributionHostNames.Deadline) is not null);

            return true;
        });

        async Task ExpectNone() => await recipient.InvokeAsync(async context =>
        {
            Check(await context.ReceiveAsync<Term>(message => message, TimeSpan.Zero) is null);

            return true;
        });
    }
}
