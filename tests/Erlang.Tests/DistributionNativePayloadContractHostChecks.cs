using Erlang;
using System.Threading.Channels;

namespace Erlang.Tests;

internal static class DistributionNativePayloadContractHostChecks
{
    internal static async Task Run()
    {
        await CheckCanonicalTrace(DistributionControlOpcodes.PayloadExitTrace);
        await CheckCanonicalTrace(DistributionControlOpcodes.PayloadExit2Trace);
        await CheckMalformedControllerDeath(DistributionControlOpcodes.PayloadExit);
        await CheckMalformedControllerDeath(DistributionControlOpcodes.PayloadExit2);
        await CheckMalformedControllerDeath(DistributionControlOpcodes.PayloadMonitorExit);
        await CheckTerminatedOriginWatcher();
        await CheckMissingLinkedTarget();
        await CheckOrdinaryExplicitKill();
        await CheckTrailingReason();
    }

    private static async Task CheckCanonicalTrace(int opcode)
    {
        await using var session = await Session.Open(false);
        await session.Controller.InvokeAsync(async context => await Call(
            context,
            SequenceTraceNames.SystemFlag,
            Term.A(SequenceTraceNames.SystemTracer),
            session.Tracer.Process.Pid
        ));
        var ready = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var selected = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle receiver = session.Runtime.Spawn(async context =>
        {
            try
            {
                context.TrapExits = true;
                await Call(
                    context,
                    SequenceTraceNames.Set,
                    Term.A(SequenceTraceNames.Label),
                    DistributionNativePayloadContractHostNames.InitialLabel
                );
                ready.TrySetResult(context);
                Term? message = await context.ReceiveAsync(value => value, DistributionNativePayloadContractHostNames.Deadline);
                Term label = await Call(context, SequenceTraceNames.Info, Term.A(SequenceTraceNames.Label));
                Term serial = await Call(context, SequenceTraceNames.Info, Term.A(SequenceTraceNames.Serial));
                Term token = await Call(
                    context,
                    SequenceTraceNames.Set,
                    Term.A(SequenceTraceNames.Token),
                    Nil.Value
                );
                selected.TrySetResult(Term.Tuple(
                    message!,
                    label,
                    serial,
                    token
                ));
                await release.Task.WaitAsync(DistributionNativePayloadContractHostNames.Deadline);

                return Nil.Value;
            }
            catch (Exception exception)
            {
                ready.TrySetException(exception);
                selected.TrySetException(exception);
                throw;
            }
        });
        ProcessContext owner = await ready.Task.WaitAsync(DistributionNativePayloadContractHostNames.Deadline);
        bool suspended = false;
        try
        {
            if (opcode == DistributionControlOpcodes.PayloadExitTrace)
            {
                await session.PutControl(Term.Tuple(Term.I(DistributionControlOpcodes.Link), session.Remote, receiver.Pid));
                Term links = await session.Controller.InvokeAsync(async context => await Call(
                    context,
                    ProcessInfoNames.Function,
                    receiver.Pid,
                    Term.A(ProcessInfoNames.Links)
                ));
                Check(links.Equals(Term.Tuple(Term.A(ProcessInfoNames.Links), Term.List(session.Remote))));
            }
            await session.Controller.InvokeAsync(async context => await Call(context, ProcessSuspensionNames.Suspend, receiver.Pid));
            suspended = true;
            Term before = await Snapshot(owner);
            Term incoming = Term.Tuple(
                Term.I(DistributionNativePayloadContractHostNames.ReceiveFlag),
                DistributionNativePayloadContractHostNames.PayloadLabel,
                Term.I(DistributionNativePayloadContractHostNames.Serial),
                session.Remote,
                Term.I(DistributionNativePayloadContractHostNames.LastCount)
            );
            await session.Put(Frame(
                Term.Tuple(
                    Term.I(opcode),
                    session.Remote,
                    receiver.Pid,
                    incoming
                ),
                ExternalTermFormat.Encode(DistributionNativePayloadContractHostNames.Reason)
            ));
            Check(!selected.Task.IsCompleted);
            Check((await Snapshot(owner)).Equals(before));
            await session.Controller.InvokeAsync(async context => await Call(context, ProcessSuspensionNames.Resume, receiver.Pid));
            suspended = false;
            Term message = ExitMessage(session.Remote, DistributionNativePayloadContractHostNames.Reason);
            Term counts = Term.Tuple(
                Term.I(DistributionNativePayloadContractHostNames.LastCount),
                Term.I(DistributionNativePayloadContractHostNames.Serial)
            );
            Check((await selected.Task.WaitAsync(DistributionNativePayloadContractHostNames.Deadline)).Equals(Term.Tuple(
                message,
                Term.Tuple(Term.A(SequenceTraceNames.Label), DistributionNativePayloadContractHostNames.PayloadLabel),
                Term.Tuple(Term.A(SequenceTraceNames.Serial), counts),
                incoming
            )));
            Term? trace = await session.Tracer.InvokeAsync(async context => await context.ReceiveAsync(
                value => value,
                DistributionNativePayloadContractHostNames.Deadline
            ));
            Check(trace!.Equals(Term.Tuple(
                Term.A(SequenceTraceNames.Message),
                DistributionNativePayloadContractHostNames.PayloadLabel,
                Term.Tuple(
                    Term.A(SequenceTraceNames.Receive),
                    counts,
                    session.Remote,
                    receiver.Pid,
                    message
                )
            )));
            Check(await session.Tracer.Invoke(context => context.Mailbox.Count) == 0);
            await session.RequireLiveController();
        }
        finally
        {
            if (suspended)
                await session.Controller.InvokeAsync(async context => await Call(context, ProcessSuspensionNames.Resume, receiver.Pid));
            release.TrySetResult();
            await receiver.Completion.WaitAsync(DistributionNativePayloadContractHostNames.Deadline);
        }
    }

    private static async Task CheckMalformedControllerDeath(int opcode)
    {
        bool monitor = opcode == DistributionControlOpcodes.PayloadMonitorExit;
        await using var session = await Session.Open(monitor);
        await using var receiver = new RuntimeNodeFoundationHostActor(session.Runtime);
        ReferenceTerm death = await session.Observe(session.Controller.Process.Pid);
        if (opcode == DistributionControlOpcodes.PayloadExit)
        {
            await session.PutControl(Term.Tuple(Term.I(DistributionControlOpcodes.Link), session.Remote, receiver.Process.Pid));
            await Drain(receiver);
            Term links = await receiver.InvokeAsync(async context => await Call(
                context,
                ProcessInfoNames.Function,
                context.Self,
                Term.A(ProcessInfoNames.Links)
            ));
            Check(links.Equals(Term.Tuple(Term.A(ProcessInfoNames.Links), Term.List(session.Remote))));
        }
        TupleTerm control = monitor
            ? Term.Tuple(
                Term.I(opcode),
                session.Remote,
                receiver.Process.Pid,
                session.Runtime.MakeReference()
            )
            : Term.Tuple(Term.I(opcode), session.Remote, receiver.Process.Pid);
        byte[] malformed = monitor
            ? [DistributionNativePayloadContractHostNames.EtfVersion, DistributionNativePayloadContractHostNames.InvalidEtfTag]
            : [DistributionNativePayloadContractHostNames.EtfVersion];
        await session.Put(Frame(control, malformed));
        await Drain(receiver);
        await DrainForTermination(session.Controller);
        Term killed = Term.A(ProcessExitReasons.Killed);
        Check((await session.Controller.Process.Completion.WaitAsync(DistributionNativePayloadContractHostNames.Deadline)).Equals(killed));
        await session.RequireDown(death, session.Controller.Process.Pid, killed);
        Check(session.Runtime.IsAlive(receiver.Process.Pid));
    }

    private static async Task CheckTerminatedOriginWatcher()
    {
        await using var session = await Session.Open(true);
        await using var watcher = new RuntimeNodeFoundationHostActor(session.Runtime);
        ReferenceTerm reference = await watcher.Invoke(context => session.Pending.BeginConnectedMonitorSignal(
            session.Handle,
            context,
            session.Remote
        ));
        Check((await session.TakeControl())!.Equals(Term.Tuple(
            Term.I(DistributionControlOpcodes.Monitor),
            watcher.Process.Pid,
            session.Remote,
            reference
        )));
        ReferenceTerm death = await session.Observe(watcher.Process.Pid);
        await watcher.DisposeAsync();
        Term normal = Term.A(ProcessExitReasons.Normal);
        Check((await watcher.Process.Completion.WaitAsync(DistributionNativePayloadContractHostNames.Deadline)).Equals(normal));
        await session.RequireDown(death, watcher.Process.Pid, normal);
        Check((await session.TakeControl())!.Equals(Term.Tuple(
            Term.I(DistributionControlOpcodes.Demonitor),
            watcher.Process.Pid,
            session.Remote,
            reference
        )));
        Check(!session.Runtime.IsAlive(watcher.Process.Pid));
        await session.Put(Frame(
            Term.Tuple(
                Term.I(DistributionControlOpcodes.PayloadMonitorExit),
                session.Remote,
                watcher.Process.Pid,
                reference
            ),
            [DistributionNativePayloadContractHostNames.EtfVersion, DistributionNativePayloadContractHostNames.InvalidEtfTag]
        ));
        // A dead watcher cannot acknowledge a barrier. Inspect the actual surviving controller instead.
        await session.RequireLiveController();
        Check(await session.TakeControl() is null);
    }

    private static async Task CheckMissingLinkedTarget()
    {
        await using var session = await Session.Open(false);
        var missing = new Pid(session.Runtime.Node, DistributionNativePayloadContractHostNames.MissingLocalId, session.Kernel.Process.Pid.Creation);
        Check(!session.Runtime.IsAlive(missing));
        await session.Put(Frame(
            Term.Tuple(Term.I(DistributionControlOpcodes.PayloadExit), session.Remote, missing),
            [DistributionNativePayloadContractHostNames.EtfVersion]
        ));
        await session.RequireLiveController();
        Check(await session.TakeControl() is null);
    }

    private static async Task CheckOrdinaryExplicitKill()
    {
        await using var session = await Session.Open(false);
        await using var receiver = new RuntimeNodeFoundationHostActor(session.Runtime);
        await receiver.Invoke(context =>
        {
            context.TrapExits = true;

            return true;
        });
        ReferenceTerm death = await session.Observe(receiver.Process.Pid);
        await session.Put(Frame(
            Term.Tuple(Term.I(DistributionControlOpcodes.PayloadExit2), session.Remote, receiver.Process.Pid),
            ExternalTermFormat.Encode(Term.A(ProcessExitSignals.Kill))
        ));
        await DrainForTermination(receiver);
        Term killed = Term.A(ProcessExitReasons.Killed);
        Check((await receiver.Process.Completion.WaitAsync(DistributionNativePayloadContractHostNames.Deadline)).Equals(killed));
        await session.RequireDown(death, receiver.Process.Pid, killed);
        await session.RequireLiveController();
    }

    private static async Task CheckTrailingReason()
    {
        await using var session = await Session.Open(false);
        await using var receiver = new RuntimeNodeFoundationHostActor(session.Runtime);
        await receiver.Invoke(context =>
        {
            context.TrapExits = true;

            return true;
        });
        byte[] reason = ExternalTermFormat.Encode(DistributionNativePayloadContractHostNames.Reason);
        byte[] withTrailing = new byte[reason.Length + 1];
        reason.CopyTo(withTrailing, 0);
        withTrailing[^1] = DistributionNativePayloadContractHostNames.TrailingReasonByte;
        await session.Put(Frame(
            Term.Tuple(Term.I(DistributionControlOpcodes.PayloadExit2), session.Remote, receiver.Process.Pid),
            withTrailing
        ));
        Term? actual = await receiver.InvokeAsync(async context => await context.ReceiveAsync(
            value => value,
            DistributionNativePayloadContractHostNames.Deadline
        ));
        Check(actual!.Equals(ExitMessage(session.Remote, DistributionNativePayloadContractHostNames.Reason)));
        Check(session.Runtime.IsAlive(receiver.Process.Pid));
        await session.RequireLiveController();
    }

    private static async Task DrainForTermination(RuntimeNodeFoundationHostActor actor)
    {
        if (actor.Process.Completion.IsCompleted)
            return;
        try
        {
            await Drain(actor);
        }
        catch (OperationCanceledException)
        {
            // The required Completion/DOWN assertion must still prove actual killed termination.
        }
        catch (ChannelClosedException) when (actor.Process.Completion.IsCompleted)
        {
        }
    }

    private static Task<Term?> Drain(RuntimeNodeFoundationHostActor actor)
        => actor.InvokeAsync(async context => await context.ReceiveAsync<Term>(_ => null, TimeSpan.Zero));

    private static async Task<Term> Snapshot(ProcessContext context)
        => Term.Tuple(
            await Call(context, SequenceTraceNames.Info, Term.A(SequenceTraceNames.Label)),
            await Call(context, SequenceTraceNames.Info, Term.A(SequenceTraceNames.Serial)),
            await Call(context, SequenceTraceNames.Info, Term.A(SequenceTraceNames.Receive))
        );

    private static Term ExitMessage(Pid remote, Term reason)
        => Term.Tuple(Term.A(ProcessMessageTags.LinkedExit), remote, reason);

    private static byte[] Frame(TupleTerm control, byte[] reason)
    {
        byte[] first = ExternalTermFormat.Encode(control);
        byte[] packet = new byte[DistributionPassThroughLayout.MarkerBytes + first.Length + reason.Length];
        packet[0] = DistributionPassThroughLayout.Marker;
        first.CopyTo(packet, DistributionPassThroughLayout.MarkerBytes);
        reason.CopyTo(packet, DistributionPassThroughLayout.MarkerBytes + first.Length);

        return packet;
    }

    private static ValueTask<Term> Call(ProcessContext context, string function, params Term[] arguments)
        => context.Runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            function,
            arguments
        );

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(DistributionNativePayloadContractHostNames.Failure);
    }

    private sealed class Session : IAsyncDisposable
    {
        private readonly bool monitor;
        private Term opaque = Nil.Value;

        private Session(bool monitor)
        {
            this.monitor = monitor;
            Runtime = new();
            Kernel = new(Runtime);
            Controller = new(Runtime);
            Input = new(Runtime);
            Tracer = new(Runtime);
            Remote = new(
                RemoteNode.Name,
                (ulong)(monitor ? DistributionNativePayloadContractHostNames.MonitorRemoteId : DistributionNativePayloadContractHostNames.PayloadRemoteId),
                DistributionNativePayloadContractHostNames.RemoteCreation
            );
        }

        internal ProcessRuntime Runtime
        {
            get;
        }
        internal RuntimeDistributionPending Pending => Runtime.DistributionPending;
        internal RuntimeNodeFoundationHostActor Kernel
        {
            get;
        }
        internal RuntimeNodeFoundationHostActor Controller
        {
            get;
        }
        internal RuntimeNodeFoundationHostActor Input
        {
            get;
        }
        internal RuntimeNodeFoundationHostActor Tracer
        {
            get;
        }
        internal Pid Remote
        {
            get;
        }
        internal Term Handle { get; private set; } = Nil.Value;
        private Atom LocalNode => monitor ? DistributionNativePayloadContractHostNames.MonitorLocalNode : DistributionNativePayloadContractHostNames.PayloadLocalNode;
        private Atom RemoteNode => monitor ? DistributionNativePayloadContractHostNames.MonitorRemoteNode : DistributionNativePayloadContractHostNames.PayloadRemoteNode;
        private ulong Flags => DistributionPassThroughFlags.Mandatory | DistributionPassThroughFlags.ExitPayload |
            (monitor ? DistributionPassThroughFlags.ProcessMonitors : 0);

        internal static async Task<Session> Open(bool monitor)
        {
            var session = new Session(monitor);
            try
            {
                await session.Kernel.InvokeAsync(async context =>
                {
                    session.Runtime.Register(ConnectedDistributionHostNames.Kernel, context.Self);
                    await Call(
                        context,
                        ConnectedIngressHostNames.SetNode,
                        session.LocalNode,
                        Term.I(DistributionNativePayloadContractHostNames.RequestedLocalCreation)
                    );

                    return true;
                });
                session.Handle = session.Pending.BeginPending(session.RemoteNode);
                session.opaque = await session.Controller.InvokeAsync(async context => await DistributionControllerAdapters.SetNode(
                    context,
                    session.RemoteNode,
                    context.Self,
                    Term.Tuple(new Integer(session.Flags), Term.I(DistributionNativePayloadContractHostNames.RemoteCreation))
                ));
                await session.Controller.Invoke(context => DistributionControllerAdapters.SetInputHandler(context, session.opaque, session.Input.Process.Pid));

                return session;
            }
            catch
            {
                await session.DisposeAsync();
                throw;
            }
        }

        internal async Task Put(byte[] packet)
        {
            Term result = await Input.Invoke(context => DistributionInputAdapter.PutData(context, opaque, new BitString(packet)));
            Check(result.Equals(DistributionNativePayloadContractHostNames.Accepted));
        }

        internal Task PutControl(TupleTerm control)
            => Put(DistributionPassThroughCodec.EncodeProcessControl(control));

        internal Task<ReferenceTerm> Observe(Pid process)
            => Kernel.Invoke(context => Runtime.Monitor(context, process));

        internal async Task RequireDown(ReferenceTerm reference, Pid process, Term reason)
        {
            Term expected = Term.Tuple(
                Term.A(ProcessMessageTags.MonitorDown),
                reference,
                Term.A(ProcessMonitorKinds.Process),
                process,
                reason
            );
            Term? actual = await Kernel.InvokeAsync(async context => await context.ReceiveAsync(
                value => value.Equals(expected) ? value : null,
                DistributionNativePayloadContractHostNames.Deadline
            ));
            Check(actual!.Equals(expected));
        }

        internal async Task RequireLiveController()
        {
            await Drain(Controller);
            Check(!Controller.Process.Completion.IsCompleted);
            Check(Runtime.IsAlive(Controller.Process.Pid));
        }

        internal Task<TupleTerm?> TakeControl()
            => Controller.Invoke<TupleTerm?>(context =>
            {
                HostDistributionBuffer? output = Pending.TakeConnectedOutput(Handle, context, out _);
                if (output is null)
                    return null;
                Check(output.SegmentCount == DistributionNativePayloadContractHostNames.SingleOutputSegment);

                return DistributionPassThroughCodec.DecodeProcessPacket(output.Segments[0].ToArray(), negotiatedFlags: Flags).Control;
            });

        public async ValueTask DisposeAsync()
        {
            try
            {
                await Tracer.DisposeAsync();
                await Input.DisposeAsync();
                await Controller.DisposeAsync();
                await Kernel.DisposeAsync();
            }
            finally
            {
                await Runtime.DisposeAsync();
            }
        }
    }
}
