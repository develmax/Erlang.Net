using System.Numerics;
using Erlang;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamBinaryField391Checks
{
    private static object U(int value) => Op(BeamBinaryHostNames.Unsigned, value);

    private static object X(int value) => Op(BeamBinaryHostNames.X, value);

    private static object I(int value) => Op(BeamBinaryHostNames.IntegerKind, value);

    private static object Fail() => Op(BeamBinaryHostNames.Label, 3);

    private static object State(BitString source, Term second, out object match)
    {
        object state = New(BeamBinaryHostNames.RegisterState);
        match = New(BeamBinaryHostNames.MatchContext, source);
        Call(
            state,
            BeamBinaryHostNames.WriteContext,
            X(0),
            match
        );
        Call(
            state,
            BeamBinaryHostNames.Write,
            1,
            second
        );

        return state;
    }

    private static Array Utf8Operands(bool skip = false, int flags = 0)
        => skip ? Ops(
            Fail(),
            X(0),
            U(1),
            U(flags)
        ) : Ops(
            Fail(),
            X(0),
            U(1),
            U(flags),
            X(1)
        );

    private static Array IntegerOperands(int size, int unit = 1, int flags = 0)
        => Ops(
            Fail(),
            X(0),
            U(1),
            I(size),
            U(unit),
            U(flags),
            X(1)
        );

    private static Array SkipOperands(int unit = 8, int flags = 0)
        => Ops(
            Fail(),
            X(0),
            X(1),
            U(unit),
            U(flags)
        );

    private static void Valid(
        object code,
        string name,
        byte opcode,
        Array operands
    )
        => Validate(
            code,
            name,
            opcode,
            operands.Length,
            operands
        );

    internal static Task Utf8Failures()
    {
        object code = Code(BeamBinaryHostBytes.Image());
        Term sentinel = Term.A(BeamOpcodeEdgeNames.Marker);
        BitString[] invalid = [
            new([]), new([0], 7), new([0x80]), new([0xc0, 0x80]), new([0xc1, 0xbf]),
            new([0xc2]), new([0xe2, 0x82]), new([0xf0, 0x90, 0x80]), new([0xe2, 0x41, 0xac]),
            new([0xe0, 0x80, 0x80]), new([0xed, 0xa0, 0x80]), new([0xf0, 0x80, 0x80, 0x80]),
            new([0xf4, 0x90, 0x80, 0x80]), new([0xf5, 0x80, 0x80, 0x80]), new([0xff])
        ];
        foreach (BitString source in invalid)
        {
            foreach (bool skip in new[] { false, true })
            {
                object state = State(source, sentinel, out object match);
                Array operands = Utf8Operands(skip);
                Valid(
                    code,
                    skip ? BeamBinaryField391Names.SkipUtf8OpcodeName : BeamBinaryField391Names.GetUtf8OpcodeName,
                    skip ? BeamBinaryField391Names.SkipUtf8Opcode : BeamBinaryField391Names.GetUtf8Opcode,
                    operands
                );
                bool success = (bool)Call(
                    Type(BeamBinaryField391Names.Execution),
                    BeamBinaryField391Names.Utf8,
                    state,
                    operands,
                    skip
                );
                Require(!success && (int)Property(match, BeamBinaryHostNames.Position) == 0);
                Require(ReferenceEquals(Call(state, BeamBinaryHostNames.Read, 1), sentinel));
            }
        }

        return Task.CompletedTask;
    }

    internal static Task Utf8Unaligned()
    {
        BitString source = Shifted([0xe2, 0x82, 0xac, 0x21]);
        foreach (bool skip in new[] { false, true })
        {
            Term sentinel = Term.A(BeamOpcodeEdgeNames.Marker);
            object state = State(source, sentinel, out object match);
            SetPosition(match, BeamBinaryField391Names.BitOffset);
            Require((bool)Call(
                Type(BeamBinaryField391Names.Execution),
                BeamBinaryField391Names.Utf8,
                state,
                Utf8Operands(skip),
                skip
            ));
            Require((int)Property(match, BeamBinaryHostNames.Position) == BeamBinaryField391Names.BitOffset + 24);
            Require(skip ? ReferenceEquals(Call(state, BeamBinaryHostNames.Read, 1), sentinel)
                : ((Term)Call(state, BeamBinaryHostNames.Read, 1)).Equals(Term.I(0x20ac)));
            Require((bool)Call(
                Type(BeamBinaryField391Names.Execution),
                BeamBinaryField391Names.Utf8,
                state,
                Utf8Operands(),
                false
            ));
            Require(((Term)Call(state, BeamBinaryHostNames.Read, 1)).Equals(Term.I(0x21)));
            int position = (int)Property(match, BeamBinaryHostNames.Position);
            Require(!(bool)Call(
                Type(BeamBinaryField391Names.Execution),
                BeamBinaryField391Names.Utf8,
                state,
                Utf8Operands(),
                false
            ));
            Require((int)Property(match, BeamBinaryHostNames.Position) == position);
        }

        return Task.CompletedTask;
    }

    internal static Task IntegerWide()
    {
        object code = Code(BeamBinaryHostBytes.Image());
        byte[] bytes = [0x80, 0, 0, 0, 0, 0, 0, 0, 0, 1];
        BitString source = Shifted(bytes);
        object state = State(source, Erlang.Nil.Value, out object match);
        SetPosition(match, BeamBinaryField391Names.BitOffset);
        Array operands = IntegerOperands(40, unit: 2);
        Valid(
            code,
            BeamBinaryField391Names.GetIntegerOpcodeName,
            BeamBinaryField391Names.GetIntegerOpcode,
            operands
        );
        Require((bool)Call(
            Type(BeamBinaryField391Names.Execution),
            BeamBinaryField391Names.GetInteger,
            code,
            state,
            operands
        ));
        Require(((Term)Call(state, BeamBinaryHostNames.Read, 1)).Equals(new Integer((BigInteger.One << 79) | BigInteger.One)));
        int position = (int)Property(match, BeamBinaryHostNames.Position);
        Require((bool)Call(
            Type(BeamBinaryField391Names.Execution),
            BeamBinaryField391Names.GetInteger,
            code,
            state,
            IntegerOperands(0)
        ));
        Require(((Term)Call(state, BeamBinaryHostNames.Read, 1)).Equals(Term.I(0)) && (int)Property(match, BeamBinaryHostNames.Position) == position);
        Term sentinel = Term.A(BeamOpcodeEdgeNames.Marker);
        Call(
            state,
            BeamBinaryHostNames.Write,
            1,
            sentinel
        );
        Require(!(bool)Call(
            Type(BeamBinaryField391Names.Execution),
            BeamBinaryField391Names.GetInteger,
            code,
            state,
            IntegerOperands(1)
        ));
        Require(ReferenceEquals(Call(state, BeamBinaryHostNames.Read, 1), sentinel) && (int)Property(match, BeamBinaryHostNames.Position) == position);

        return Task.CompletedTask;
    }

    internal static Task SkipBounds()
    {
        object code = Code(BeamBinaryHostBytes.Image());
        foreach (Term size in new Term[] { Term.I(-1), new Integer(BigInteger.One << 128), Term.I(5), Term.A(BeamOpcodeEdgeNames.Other) })
        {
            object state = State(new([1, 2, 3, 4]), size, out object match);
            Require(!(bool)Call(
                Type(BeamBinaryField391Names.Execution),
                BeamBinaryField391Names.Skip,
                code,
                state,
                SkipOperands()
            ));
            Require((int)Property(match, BeamBinaryHostNames.Position) == 0 && ReferenceEquals(Call(state, BeamBinaryHostNames.Read, 1), size));
        }
        object valid = State(new([1, 2, 3, 4]), Term.I(0), out object cursor);
        Require((bool)Call(
            Type(BeamBinaryField391Names.Execution),
            BeamBinaryField391Names.Skip,
            code,
            valid,
            SkipOperands()
        ));
        Require((int)Property(cursor, BeamBinaryHostNames.Position) == 0);
        Call(
            valid,
            BeamBinaryHostNames.Write,
            1,
            Term.I(4)
        );
        Require((bool)Call(
            Type(BeamBinaryField391Names.Execution),
            BeamBinaryField391Names.Skip,
            code,
            valid,
            SkipOperands()
        ));
        Require((int)Property(cursor, BeamBinaryHostNames.Position) == 32);

        return Task.CompletedTask;
    }

    internal static Task RoleRefusals()
    {
        object code = Code(BeamBinaryHostBytes.Image());
        foreach (int unit in new[] { 0, 257 })
        {
            Refuses<NotSupportedException>(() => Valid(
                code,
                BeamBinaryField391Names.GetIntegerOpcodeName,
                BeamBinaryField391Names.GetIntegerOpcode,
                IntegerOperands(1, unit)
            ));
            Refuses<NotSupportedException>(() => Valid(
                code,
                BeamBinaryField391Names.SkipBitsOpcodeName,
                BeamBinaryField391Names.SkipBitsOpcode,
                SkipOperands(unit)
            ));
        }
        Refuses<NotSupportedException>(() => Valid(
            code,
            BeamBinaryField391Names.GetIntegerOpcodeName,
            BeamBinaryField391Names.GetIntegerOpcode,
            IntegerOperands(1, flags: 1)
        ));
        Refuses<NotSupportedException>(() => Valid(
            code,
            BeamBinaryField391Names.SkipBitsOpcodeName,
            BeamBinaryField391Names.SkipBitsOpcode,
            SkipOperands(flags: 1)
        ));
        foreach (bool skip in new[] { false, true })
            Refuses<NotSupportedException>(() => Valid(
                code,
                skip ? BeamBinaryField391Names.SkipUtf8OpcodeName : BeamBinaryField391Names.GetUtf8OpcodeName,
                skip ? BeamBinaryField391Names.SkipUtf8Opcode : BeamBinaryField391Names.GetUtf8Opcode,
                Utf8Operands(skip, flags: 1)
            ));

        return Task.CompletedTask;
    }

    internal static async Task OpaqueContext()
    {
        byte[] accepted = Image(returnContext: false);
        BeamModule module = BeamModule.Prepare(accepted);
        Require(module.Supports(BeamOpcodeEdgeNames.Run, 1));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Require((await actor.InvokeAsync(context => module.Invoke(context, BeamOpcodeEdgeNames.Run,
            new BitString([0xe2, 0x82, 0xac])).AsTask()).WaitAsync(BeamBinaryField391Names.Deadline)).Equals(Term.I(0x20ac)));
        bool refused;
        try
        {
            refused = !BeamModule.Prepare(Image(returnContext: true)).Supports(BeamOpcodeEdgeNames.Run, 1);
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            refused = true;
        }
        Require(refused);
    }

    private static byte[] Image(bool returnContext)
        => BeamBinaryHostBytes.Image(BeamBinaryHostBytes.Join(
            BeamBinaryHostBytes.Start(1, 0, 0),
            BeamBinaryHostBytes.Instruction(
                BeamBinaryField391Names.GetUtf8Opcode,
                BeamBinaryHostBytes.F(3),
                BeamBinaryHostBytes.X(0),
                BeamBinaryHostBytes.U(1),
                BeamBinaryHostBytes.U(0),
                BeamBinaryHostBytes.X(returnContext ? 1 : 0)
            ),
            [BeamBinaryField391Names.ReturnOpcode, BeamBinaryField391Names.LabelOpcode, .. BeamBinaryHostBytes.U(3)],
            BeamBinaryHostBytes.Instruction(BeamBinaryField391Names.MoveOpcode, BeamBinaryHostBytes.A(7), BeamBinaryHostBytes.X(0)),
            [BeamBinaryField391Names.ReturnOpcode]
        ));

    private static BitString Shifted(byte[] bytes)
    {
        int length = bytes.Length * 8 + BeamBinaryField391Names.BitOffset;
        byte[] result = new byte[(length + 7) / 8];
        for (int index = 0; index < bytes.Length * 8; index++)
            result[(index + BeamBinaryField391Names.BitOffset) / 8] |= (byte)(((bytes[index / 8] >> (7 - index % 8)) & 1)
                << (7 - (index + BeamBinaryField391Names.BitOffset) % 8));

        return new(result, length);
    }
}
