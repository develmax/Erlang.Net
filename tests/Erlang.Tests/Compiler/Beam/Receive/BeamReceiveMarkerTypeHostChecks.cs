using System.Reflection;
using Erlang;
using static Erlang.Tests.BeamOpcodeEdgeBytes;
using static Erlang.Tests.BeamReceiveMarkerTypeHostNames;
using static Erlang.Tests.BeamReceiveMarkerTypeAccess;

namespace Erlang.Tests;

internal static class BeamReceiveMarkerTypeHostChecks
{
    internal static Task SlotLayout()
    {
        System.Type slot = BeamReceiveMarkerTypeAccess.Type(SlotType);
        FieldInfo[] fields = slot.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Require(fields.Length == 1 && fields[0].FieldType == typeof(object) && ManagedSlotSize() == IntPtr.Size);
        Term term = Term.Tuple(Term.I(381));
        object ordinary = Call(slot, FromTermMethod, term);
        Require(ReferenceEquals(fields[0].GetValue(ordinary), term));

        return Task.CompletedTask;
    }

    internal static Task StorageIdentity()
    {
        object state = New(StateType);
        Call(state, AllocateMethod, 3);
        object marker = Call(new Mailbox(), ReserveMethod);
        object x0 = Operand(XKind, 0), y0 = Operand(YKind, 0), y2 = Operand(YKind, 2);
        Term ordinary = Term.Tuple(Term.I(381));
        Call(
            state,
            WriteMarkerMethod,
            x0,
            marker
        );
        Call(
            state,
            WriteYMethod,
            0,
            ordinary
        );
        Call(
            state,
            CopyMethod,
            x0,
            y2
        );
        object clone = Call(state, CloneMethod);
        Call(
            clone,
            TrimMethod,
            2,
            1
        );
        Require(ReferenceEquals(Call(clone, ReadMarkerMethod, y0), marker));
        Call(
            clone,
            WriteYMethod,
            0,
            ordinary
        );
        Require(ReferenceEquals(Call(state, ReadMarkerMethod, y2), marker));
        Call(
            state,
            SwapMethod,
            x0,
            y0
        );
        Require(ReferenceEquals(Call(state, ReadMarkerMethod, y0), marker));
        Require(ReferenceEquals(Call(state, ReadMethod, 0), ordinary));

        return Task.CompletedTask;
    }

    internal static Task ReadRefusals()
    {
        object state = New(StateType);
        object x0 = Operand(XKind, 0), x1 = Operand(XKind, 1);
        Call(
            state,
            WriteMarkerMethod,
            x0,
            Call(new Mailbox(), ReserveMethod)
        );
        object slot = Call(state, ReadSlotMethod, x0);
        foreach (string method in new[] { ReadTermMethod, ReadTraceMethod, ReadHandlerMethod })
            Refuses(() => Call(slot, method));
        Refuses(() => Call(state, ReadMarkerMethod, x1));
        object ordinary = Call(BeamReceiveMarkerTypeAccess.Type(SlotType), FromTermMethod, Nil.Value);
        Refuses(() => Call(ordinary, ReadMarkerMethod));

        return Task.CompletedTask;
    }

    internal static Task LiveRoots()
    {
        object state = New(StateType);
        object x0 = Operand(XKind, 0);
        object marker = Call(new Mailbox(), ReserveMethod);
        Call(
            state,
            WriteMarkerMethod,
            x0,
            marker
        );
        Call(state, CheckLiveMethod, 1);
        Refuses(() => Call(state, ReadMethod, 0));
        Call(state, ClearMethod, (object)new[] { 0 });
        Require(ReferenceEquals(Call(state, ReadMarkerMethod, x0), marker));
        Call(state, ClearMethod, (object)Array.Empty<int>());
        Refuses(() => Call(state, ReadMarkerMethod, x0));

        return Task.CompletedTask;
    }

    internal static Task BindTypes()
    {
        Reject([Bind, X(0), X(1), Move, A(7), X(0), Return], arity: 2);
        Reject([Reserve, X(0), Reserve, X(1), Bind, X(0), X(1), Move, A(7), X(0), Return]);

        return Task.CompletedTask;
    }

    internal static Task OperandRoles()
    {
        Reject([Reserve, A(7), Move, A(7), X(0), Return]);
        Reject([Reserve, X(0), Bind, X(0), A(7), Move, A(7), X(0), Return]);
        foreach (byte opcode in new[] { Use, Clear })
        {
            Reject([opcode, A(7), Move, A(7), X(0), Return]);
            Reject([Reserve, X(0), opcode, X(0), Move, A(7), X(0), Return]);
        }

        return Task.CompletedTask;
    }

    internal static Task ConsumerLeaks()
    {
        Reject([Reserve, X(0), Return]);
        Reject([Reserve, X(0), PutTuple, X(0), ArgumentList, U(1), X(0), Return]);
        Reject([Reserve, X(0), PutList, X(0), A(0), X(0), Return]);
        Reject([Reserve, X(0), Move, A(7), X(1), Send, Return]);
        Reject([Allocate, U(0), U(0), Reserve, X(0), Move, A(5), X(1), Move, A(6), X(2), BeamApplyHostNames.Opcode, U(1), Deallocate, U(0), Return]);

        return Task.CompletedTask;
    }

    internal static Task JoinKinds()
    {
        Reject(
            [EqualOpcode, F(3), X(0), A(7), Reserve, X(1), Jump, F(4),
            Label, U(3), Move, A(7), X(1), Label, U(4), Move, X(1), X(0), Return],
            arity: 1,
            labels: 5
        );

        return Task.CompletedTask;
    }

    internal static Task CallClobber()
    {
        Reject([Reserve, X(1), Allocate, U(0), U(2), CallFun, U(0),
            Move, X(1), X(0), Deallocate, U(0), Return], arity: 1);

        return Task.CompletedTask;
    }

    internal static Task TimeoutClobber()
    {
        RejectImage(BeamReceiveMarkerTypeBytes.Receive(staleTimeout: true), 2);

        return Task.CompletedTask;
    }

    internal static async Task TrimFlow()
    {
        byte[] image = BeamReceiveMarkerTypeBytes.Image(
            [
            Allocate, U(3), U(1), Move, A(7), Y(0), Move, X(0), Y(1), Reserve, Y(2),
            Trim, U(1), U(2), Bind, Y(1), Y(0), Clear, Y(0),
            Move, A(7), X(0), Deallocate, U(2), Return
        ],
            arity: 1
        );
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        BeamModule module = Prepare(image, 1);
        Term result = await actor.InvokeAsync(context => module.Invoke(context, BeamOpcodeEdgeNames.Run, runtime.MakeReference()).AsTask());
        Require(result.Equals(Term.A(BeamOpcodeEdgeNames.Marker)));
    }

    internal static Task HandlerOverlap()
    {
        object state = New(StateType);
        Call(state, AllocateMethod, 1);
        object function = New(
            FunctionType,
            BeamOpcodeEdgeNames.Module,
            BeamOpcodeEdgeNames.Run,
            0,
            1,
            2,
            0,
            8
        );
        object checkpoint = New(
            CheckpointType,
            function,
            3,
            0,
            0,
            0,
            1
        );
        Call(
            state,
            InstallHandlerMethod,
            0,
            checkpoint
        );
        object y0 = Operand(YKind, 0);
        Refuses(() => Call(
            state,
            WriteMarkerMethod,
            y0,
            Call(new Mailbox(), ReserveMethod)
        ));
        Refuses(() => Call(state, ReadMarkerMethod, y0));
        Reject(
            [Allocate, U(1), U(0), Try, Y(0), F(3), Reserve, Y(0), TryEnd, Y(0),
            Move, A(7), X(0), Deallocate, U(1), Return,
            Label, U(3), TryCase, Y(0), Move, A(7), X(0), Deallocate, U(1), Return],
            labels: 4
        );

        return Task.CompletedTask;
    }

    internal static Task ReceiveSelected() => ReceiveObserved(true);

    internal static Task ReceiveTimedOut() => ReceiveObserved(false);

    private static async Task ReceiveObserved(bool sendSelected)
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        BeamModule module = Prepare(BeamReceiveMarkerTypeBytes.Receive(), 2);
        int callbacks = 0;
        var callback = new FunctionTerm(
            0,
            (owner, _) =>
        {
            callbacks++;
            if (sendSelected)
                ((ProcessContext)owner).Mailbox.Send(Term.A(BeamOpcodeEdgeNames.Other));

            return ValueTask.FromResult<Term>(Nil.Value);
        }
        );
        await actor.InvokeAsync(async context =>
        {
            Term older = Term.A(BeamOpcodeEdgeNames.Marker);
            context.Mailbox.Send(older);
            Term result = await module.Invoke(
                context,
                BeamOpcodeEdgeNames.Run,
                runtime.MakeReference(),
                callback
            );
            Require(result.Equals(Term.A(sendSelected ? BeamOpcodeEdgeNames.Other : BeamOpcodeEdgeNames.Marker)));
            Require(context.Mailbox.Count == 1 && callbacks == 1);
            Require((await context.ReceiveAsync(value => value, TimeSpan.Zero))!.Equals(older));

            return true;
        }).WaitAsync(Deadline);
    }

    internal static Task RejectedScratch()
    {
        RejectImage(BeamReceiveMarkerTypeBytes.Receive(rejectedScratch: true), 0);

        return Task.CompletedTask;
    }

    internal static Task ReceiveTryRefused()
    {
        // Retain the historical negative identifier. Ordinary protected receive
        // is supported now; this handler instead reads uninitialized X3.
        Reject(
            [Allocate, U(1), U(0), Try, Y(0), F(7),
            Label, U(3), LoopReceive, F(6), X(0), EqualOpcode, F(5), X(0), A(8),
            RemoveMessage, TryEnd, Y(0), Deallocate, U(1), Return,
            Label, U(5), LoopReceiveEnd, F(3),
            Label, U(6), WaitTimeout, F(3), I(0), TimeoutOpcode, TryEnd, Y(0), Move, A(7), X(0), Deallocate, U(1), Return,
            Label, U(7), TryCase, Y(0), Move, X(3), X(0), Deallocate, U(1), Return],
            labels: 8
        );

        return Task.CompletedTask;
    }

    internal static async Task GcRoots()
    {
        byte[] image = BeamReceiveMarkerTypeBytes.Image([Reserve, X(0), Allocate, U(1), U(1),
            Move, X(0), Y(0), HeapTest, U(0), U(1), Move, A(7), X(0), Deallocate, U(1), Return]);
        BeamModule module = Prepare(image, 0);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Require((await actor.InvokeAsync(context => module.Invoke(context, BeamOpcodeEdgeNames.Run).AsTask())).Equals(Term.A(BeamOpcodeEdgeNames.Marker)));
    }

    internal static Task MissingReserve()
    {
        Reject([Bind, X(1), X(0), Move, A(7), X(0), Return], arity: 1);
        Reject([Use, X(0), Move, A(7), X(0), Return]);

        return Task.CompletedTask;
    }

    internal static Task ClearBeforeRejection()
    {
        // Paired with ClearThenExtract: only moving clear ahead of a failing test is refused.
        RejectImage(BeamReceiveMarkerTypeBytes.ClearMatcher(beforeRejection: true), 2);

        return Task.CompletedTask;
    }

    internal static async Task ClearThenExtract()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        BeamModule module = Prepare(BeamReceiveMarkerTypeBytes.ClearMatcher(beforeRejection: false), 2);
        Term payload = Term.Tuple(Term.I(381));
        int callbacks = 0;
        var callback = new FunctionTerm(
            0,
            (owner, _) =>
        {
            callbacks++;
            ((ProcessContext)owner).Mailbox.Send(Term.Tuple(Term.A(BeamOpcodeEdgeNames.Other), payload));

            return ValueTask.FromResult<Term>(Nil.Value);
        }
        );
        await actor.InvokeAsync(async context =>
        {
            Term older = Term.A(BeamOpcodeEdgeNames.Marker);
            context.Mailbox.Send(older);
            Term result = await module.Invoke(
                context,
                BeamOpcodeEdgeNames.Run,
                runtime.MakeReference(),
                callback
            );
            Require(result.Equals(payload) && callbacks == 1 && context.Mailbox.Count == 1);
            Require((await context.ReceiveAsync(value => value, TimeSpan.Zero))!.Equals(older));

            return true;
        }).WaitAsync(Deadline);
    }

    private static BeamModule Prepare(byte[] image, int arity)
    {
        BeamModule module = BeamModule.Prepare(image);
        Require(module.Supports(BeamOpcodeEdgeNames.Run, arity));

        return module;
    }

    private static void Reject(byte[] code, int arity = 0, int labels = 3)
        => RejectImage(BeamReceiveMarkerTypeBytes.Image(code, arity, labels), arity);

    private static void RejectImage(byte[] image, int arity)
    {
        bool refused = false;
        try
        {
            refused = !BeamModule.Prepare(image).Supports(BeamOpcodeEdgeNames.Run, arity);
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            refused = true;
        }
        Require(refused);
    }
}
