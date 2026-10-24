using System.Numerics;
using Erlang;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamBinaryHostNames;

namespace Erlang.Tests;

internal static class BeamBinaryHostChecks
{
    private static object O(string kind, int value) => Op(kind, value);

    private static object U(int value) => O(Unsigned, value);

    private static object A(int value) => O(AtomKind, value);

    private static object Xr(int value) => O(X, value);

    private static object Yr(int value) => O(Y, value);

    private static object I(int value) => O(IntegerKind, value);

    private static object N() => O(NilKind, 0);

    private static object State(params Term[] values)
    {
        object state = New(RegisterState);
        for (int index = 0; index < values.Length; index++)
            Call(
                state,
                Write,
                index,
                values[index]
            );

        return state;
    }

    private static Array MatchOps(
        int required = 8,
        int bits = 8,
        int expected = 0,
        int live = 1,
        int destination = 1
    )
        => Ops(
            O(Label, 3),
            Xr(0),
            O(Count, 11),
            A(EnsureAtom),
            U(required),
            U(1),
            A(EqualAtom),
            N(),
            U(bits),
            U(expected),
            A(GetTailAtom),
            U(live),
            U(1),
            Xr(destination)
        );

    private static Array CreateOps(
        int kind,
        object value,
        object size,
        int unit = 1
    )
        => Ops(
            O(Failed, 0),
            U(0),
            U(1),
            U(1),
            Xr(0),
            O(Count, 6),
            A(kind),
            U(0),
            U(unit),
            N(),
            value,
            size
        );

    private static BitString Create(object code, object state, Array operands)
    {
        Validate(
            code,
            CreateName,
            CreateOpcode,
            6,
            operands
        );

        return (BitString)Call(
            Type(Execution),
            CreateMethod,
            null!,
            code,
            state,
            operands
        );
    }

    private static bool Match(object code, object state, Array operands)
        => (bool)Call(
            Type(Execution),
            MatchMethod,
            code,
            state,
            operands
        );

    internal static Task Storage()
    {
        BitString source = new([0xab, 0xcd]);
        object context = New(MatchContext, source);
        object state = State(source);
        Call(state, Allocate, 2);
        Call(
            state,
            WriteY,
            0,
            source
        );
        Call(
            state,
            WriteContext,
            Xr(0),
            context
        );
        Call(
            state,
            Copy,
            Xr(0),
            Yr(1)
        );
        object clone = Call(state, Clone);
        Call(
            clone,
            Trim,
            1,
            1
        );
        Require(ReferenceEquals(Call(clone, ReadContext, Yr(0)), context));
        BeamBinaryHostAccess.SetPosition(Call(clone, ReadContext, Yr(0)), 5);
        Require((int)Property(Call(state, ReadContext, Xr(0)), Position) == 5 && ReferenceEquals(Property(context, Source), source));
        Call(
            state,
            Swap,
            Xr(0),
            Yr(0)
        );
        Require(ReferenceEquals(Call(state, ReadContext, Yr(0)), context) && ReferenceEquals(Call(state, Read, 0), source));
        object slot = Call(state, ReadSlot, Yr(0));
        foreach (string method in new[] { "ReadTerm", ReadTrace, ReadMarker, ReadHandler })
            Refuses<InvalidDataException>(() => Call(slot, method));
        Refuses<InvalidDataException>(() => Call(state, ReadContext, Xr(0)));
        Call(
            state,
            WriteContext,
            Xr(0),
            context
        );
        Call(state, CheckLive, 1);
        Refuses<InvalidDataException>(() => Call(state, Read, 0));

        return Task.CompletedTask;
    }

    internal static Task StartAlias()
    {
        object code = Code(BeamBinaryHostBytes.Image());
        BitString source = new([0xab, 0xcd]);
        object state = State(source);
        Array operands = Ops(
            A(NoFailAtom),
            U(1),
            Xr(0),
            Xr(0)
        );
        Validate(
            code,
            StartName,
            StartOpcode,
            4,
            operands
        );
        Call(
            Type(Execution),
            StartMethod,
            code,
            state,
            operands
        );
        object context = Call(state, ReadContext, Xr(0));
        Require(ReferenceEquals(Property(context, Source), source) && (int)Property(context, Position) == 0);
        Refuses<InvalidDataException>(() => Call(state, Read, 0));
        object separate = State(source);
        Call(
            Type(Execution),
            StartMethod,
            code,
            separate,
            Ops(
                A(NoFailAtom),
                U(1),
                Xr(0),
                Xr(1)
            )
        );
        Require(ReferenceEquals(Call(separate, Read, 0), source));

        return Task.CompletedTask;
    }

    internal static Task CursorConsumption()
    {
        object code = Code(BeamBinaryHostBytes.Image());
        BitString source = new([0xab, 0xcd]);
        object context = New(MatchContext, source);
        Term sentinel = Term.A(BeamOpcodeEdgeNames.Marker);
        object state = State(Erlang.Nil.Value, sentinel);
        Call(
            state,
            WriteContext,
            Xr(0),
            context
        );
        Array ensureFailure = MatchOps(required: 17, bits: 4, expected: 10);
        Validate(
            code,
            MatchName,
            MatchOpcode,
            3,
            ensureFailure
        );
        Require(!Match(code, state, ensureFailure) && (int)Property(context, Position) == 0);
        Require(ReferenceEquals(Call(state, Read, 1), sentinel));
        Array mismatch = MatchOps(required: 4, bits: 4, expected: 5);
        Validate(
            code,
            MatchName,
            MatchOpcode,
            3,
            mismatch
        );
        Require(!Match(code, state, mismatch) && (int)Property(context, Position) == 4);
        Require(ReferenceEquals(Call(state, Read, 1), sentinel));
        Array tail = MatchOps(required: 0, bits: 0);
        Validate(
            code,
            MatchName,
            MatchOpcode,
            3,
            tail
        );
        Require(Match(code, state, tail) && ((Term)Call(state, Read, 1)).Equals(source.SliceBits(4, 12)));

        return Task.CompletedTask;
    }

    internal static Task TailAlias()
    {
        object code = Code(BeamBinaryHostBytes.Image());
        BitString source = new([0xab, 0xcd]);
        object context = New(MatchContext, source);
        BeamBinaryHostAccess.SetPosition(context, 4);
        object state = State();
        Call(
            state,
            WriteContext,
            Xr(0),
            context
        );
        Array tail = MatchOps(required: 0, bits: 0);
        Require(Match(code, state, tail));
        Term expected = source.SliceBits(4, 12);
        Require(((Term)Call(state, Read, 1)).Equals(expected) && (int)Property(context, Position) == 4);
        Require(Match(code, state, tail) && ((Term)Call(state, Read, 1)).Equals(expected) && (int)Property(context, Position) == 4);
        Require(Match(code, state, MatchOps(required: 0, bits: 0, destination: 0)));
        Require(((Term)Call(state, Read, 0)).Equals(expected) && (int)Property(context, Position) == 4);
        Refuses<InvalidDataException>(() => Call(state, ReadContext, Xr(0)));

        return Task.CompletedTask;
    }

    internal static Task IntegerTruncation()
    {
        object code = Code(BeamBinaryHostBytes.Image());
        foreach ((BigInteger value, int bits, byte[] expected) in new[]
        {
            (new BigInteger(0x1ff), 8, new byte[] { 0xff }),
            (new BigInteger(-257), 8, new byte[] { 0xff }),
            ((BigInteger.One << 40) + 0x12345678, 32, new byte[] { 0x12, 0x34, 0x56, 0x78 }),
            (new BigInteger(-1), 32, new byte[] { 0xff, 0xff, 0xff, 0xff })
        })
        {
            Term integer = new Erlang.Integer(value);
            object state = State(integer);
            BitString actual = Create(code, state, CreateOps(IntegerAtom, Xr(0), I(bits)));
            Require(actual.BitLength == bits && actual.ToArray().AsSpan().SequenceEqual(expected));
            Require(ReferenceEquals(Call(state, Read, 0), integer));
            Call(
                state,
                Write,
                0,
                actual
            );
            Require(ReferenceEquals(Call(state, Read, 0), actual));
        }

        return Task.CompletedTask;
    }

    internal static Task BinaryAll()
    {
        object code = Code(BeamBinaryHostBytes.Image());
        BitString shifted = new BitString([0x55, 0xaa]).SliceBits(1, 8);
        Require(Create(code, State(shifted), CreateOps(
            BinaryAtom,
            Xr(0),
            A(AllAtom),
            unit: 8
        )).Equals(shifted));
        BitString partial = new([0xbc, 0xd0], 12);
        Require(Create(code, State(partial), CreateOps(BinaryAtom, Xr(0), A(AllAtom))).Equals(partial));
        Refuses<ErlangException>(() => Create(code, State(partial), CreateOps(
            BinaryAtom,
            Xr(0),
            A(AllAtom),
            unit: 8
        )));
        object mixed = State(Term.I(5), new BitString([0xd8], 5));
        Array segments = Ops(
            O(Failed, 0),
            U(0),
            U(2),
            U(1),
            Xr(0),
            O(Count, 12),
            A(IntegerAtom),
            U(0),
            U(1),
            N(),
            Xr(0),
            I(3),
            A(BinaryAtom),
            U(1),
            U(1),
            N(),
            Xr(1),
            A(AllAtom)
        );
        Require(Create(code, mixed, segments).Equals(new BitString([0xbb])));

        return Task.CompletedTask;
    }

    internal static Task StringRange()
    {
        object code = Code(BeamBinaryHostBytes.Image(strings: [10, 20, 30, 40]));
        Array segment = CreateOps(
            StringAtom,
            U(1),
            I(2),
            unit: 8
        );
        Require(Create(code, State(Erlang.Nil.Value), segment).Equals(new BitString([20, 30])));
        Refuses<NotSupportedException>(() => Validate(
            code,
            CreateName,
            CreateOpcode,
            6,
            CreateOps(
                StringAtom,
                U(3),
                I(2),
                unit: 8
            )
        ));
        Refuses<NotSupportedException>(() => Validate(
            code,
            CreateName,
            CreateOpcode,
            6,
            CreateOps(
                StringAtom,
                U(0),
                I(1),
                unit: 1
            )
        ));

        return Task.CompletedTask;
    }

    internal static Task MalformedShapes()
    {
        object code = Code(BeamBinaryHostBytes.Image());
        Refuses<NotSupportedException>(() => Validate(
            code,
            MatchName,
            MatchOpcode,
            3,
            Ops(MatchOps().Cast<object>().Take(13).ToArray())
        ));
        Refuses<NotSupportedException>(() => Validate(
            code,
            StartName,
            StartOpcode,
            4,
            Ops(
                A(7),
                U(1),
                Xr(0),
                Xr(0)
            )
        ));
        foreach ((int index, object value) in new[]
        {
            (2, O(Count, 10)), (3, A(IntegerAtom)), (5, U(0)), (7, A(7)),
            (8, U(MaximumEqualityBits + 1)), (9, U(256)), (12, U(0)), (13, I(0))
        })
        {
            Array operands = MatchOps();
            operands.SetValue(value, index);
            Refuses<NotSupportedException>(() => Validate(
                code,
                MatchName,
                MatchOpcode,
                3,
                operands
            ));
        }
        object context = New(MatchContext, new BitString([0]));
        object state = State();
        Call(
            state,
            WriteContext,
            Xr(0),
            context
        );
        Refuses<InvalidDataException>(() => Match(code, state, MatchOps(required: 0, bits: 0, live: 2)));

        return Task.CompletedTask;
    }

    internal static async Task ContextFlow()
    {
        Reject(BeamBinaryHostBytes.Image(BeamBinaryHostBytes.Join(BeamBinaryHostBytes.Start(1, 0, 0), [19])));
        Reject(BeamBinaryHostBytes.Image(BeamBinaryHostBytes.Join(BeamBinaryHostBytes.Start(1, 0, 0), [164, 3, 0x17, 0x10, 3, 19])));
        byte[] match = BeamBinaryHostBytes.Join(BeamBinaryHostBytes.Start(1, 0, 0), BeamBinaryHostBytes.Match(0),
            [64, 0x13, 3, 19, 1, 0x30, 64, 0x72, 3, 19]);
        Reject(BeamBinaryHostBytes.Image(match));
        byte[] sourceRetained = BeamBinaryHostBytes.Image(BeamBinaryHostBytes.Join(BeamBinaryHostBytes.Start(0, 1, 0), [64, 0x13, 3, 19]), arity: 2);
        BeamModule prepared = BeamModule.Prepare(sourceRetained);
        Require(prepared.Supports(BeamOpcodeEdgeNames.Run, 2));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        BitString source = new([0xab]);
        Require(ReferenceEquals(
            await actor.InvokeAsync(context => prepared.Invoke(
                context,
                BeamOpcodeEdgeNames.Run,
                Erlang.Nil.Value,
                source
            ).AsTask()),
            source
        ));
        byte[] dead = BeamBinaryHostBytes.Image(BeamBinaryHostBytes.Join(BeamBinaryHostBytes.Start(0, 1, 0), [64, 0x23, 3, 19]), arity: 3);
        Reject(dead, 3);
    }

    private static void Reject(byte[] image, int arity = 1)
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
