using Erlang;
using static Erlang.Tests.BeamBinaryHostBytes;
using static Erlang.Tests.BeamWritableReceive411Names;

namespace Erlang.Tests;

internal static class BeamWritableReceive411Checks
{
    private static byte[] Y(int index) => [BeamOpcodeEdgeBytes.Y(index)];
    private static byte[] I(int value) => [BeamOpcodeEdgeBytes.I(value)];
    private static byte[] Scan(byte[] accepted) => Join(
        Instruction(Label, U(3)), Instruction(Loop, F(4), X(0)),
        Instruction(EqualOpcode, F(5), X(0), A(7)), [Remove], accepted,
        Instruction(Label, U(5)), Instruction(LoopEnd, F(3)),
        Instruction(Label, U(4)), Instruction(Wait, F(3))
    );
    private static BeamModule Prepare(byte[] body, int arity = 0)
        => BeamModule.Prepare(Image(body, arity: arity, labels: 6));

    internal static async Task After()
    {
        BeamModule module = Prepare(Scan(Join(Instruction(Move, I(4), X(0)), [Initialize, Return])));
        BeamBinaryHostAccess.Require(module.Supports(BeamOpcodeEdgeNames.Run, 0));
        await using var runtime = new ProcessRuntime(1);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            context.Mailbox.Send(Term.A(BeamOpcodeEdgeNames.Other));
            context.Mailbox.Send(Term.A(BeamOpcodeEdgeNames.Marker));
            Term value = await module.Invoke(context, BeamOpcodeEdgeNames.Run);
            BeamBinaryHostAccess.Require(value is BitString { BitLength: 0 } && context.Mailbox.Count == 1);
            Term? retained = await context.ReceiveAsync(item => item, TimeSpan.Zero);
            BeamBinaryHostAccess.Require(retained!.Equals(Term.A(BeamOpcodeEdgeNames.Other)));
            return value;
        });
    }

    internal static async Task Before()
    {
        BeamModule module = Prepare(Join([Initialize], Instruction(Allocate, U(1), U(1)),
            Instruction(Move, X(0), Y(0)), Scan(Join(Instruction(Move, Y(0), X(0)), Instruction(Deallocate, U(1)), [Return]))), 1);
        BeamBinaryHostAccess.Require(module.Supports(BeamOpcodeEdgeNames.Run, 1));
        await using var runtime = new ProcessRuntime(1);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            context.Mailbox.Send(Term.A(BeamOpcodeEdgeNames.Other));
            context.Mailbox.Send(Term.A(BeamOpcodeEdgeNames.Marker));
            Term value = await module.Invoke(context, BeamOpcodeEdgeNames.Run, Term.I(4));
            BeamBinaryHostAccess.Require(value is BitString { BitLength: 0 } && context.Mailbox.Count == 1);
            _ = await context.ReceiveAsync(item => item, TimeSpan.Zero);
            return value;
        });
        // Register cloning shares ordinary Terms. Fresh initialization affects
        // only the clone's X bank and cannot overwrite an existing binary alias.
        object registers = BeamBinaryHostAccess.New(BeamBinaryHostNames.RegisterState);
        BeamBinaryHostAccess.Call(registers, BeamBinaryHostNames.Write, 0, Term.I(4));
        BeamBinaryHostAccess.Call(BeamBinaryHostAccess.Type(BeamBinaryHostNames.Execution), InitializeMethod, registers);
        var first = (BitString)BeamBinaryHostAccess.Call(registers, BeamBinaryHostNames.Read, 0);
        object clone = BeamBinaryHostAccess.Call(registers, CloneMethod);
        BeamBinaryHostAccess.Call(BeamBinaryHostAccess.Type(BeamBinaryHostNames.Execution), InitializeMethod, clone);
        var second = (BitString)BeamBinaryHostAccess.Call(clone, BeamBinaryHostNames.Read, 0);
        BeamBinaryHostAccess.Require(!ReferenceEquals(first, second) && first.BitLength == 0 && second.BitLength == 0);
        BeamBinaryHostAccess.Require(ReferenceEquals(first, BeamBinaryHostAccess.Call(registers, BeamBinaryHostNames.Read, 0)));
    }

    internal static Task Refuse()
    {
        // Initialization inside candidate scratch is still forbidden.
        byte[] matcher = Join(Instruction(Label, U(3)), Instruction(Loop, F(4), X(0)), [Initialize, Remove, Return],
            Instruction(Label, U(4)), Instruction(Wait, F(3)));
        BeamBinaryHostAccess.Require(!Prepare(matcher).Supports(BeamOpcodeEdgeNames.Run, 0));
        // Ordinary initialization does not license any opaque-producing role
        // outside the reviewed adjacent candidate pair.
        byte[] opaque = Join(Instruction(Move, I(4), X(0)), [Initialize], Start(1, 0, 1), Scan(Join(Instruction(Move, A(7), X(0)), [Return])));
        BeamBinaryHostAccess.Require(!Prepare(opaque).Supports(BeamOpcodeEdgeNames.Run, 0));
        return Task.CompletedTask;
    }
}
