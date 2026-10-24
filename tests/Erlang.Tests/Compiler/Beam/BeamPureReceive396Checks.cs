using static Erlang.Tests.BeamPureReceive396Names;
using System.Collections;
using Erlang;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamPureReceive396Checks
{

    private static byte[] Image(byte[] matcher, string function = ErlangModuleNames.Element, int arity = 2)
        => BeamOpcodeEdgeBytes.Image(
            [.. Start(
                1,
                2,
                2,
                0
            ), 1, U(3), 23, F(6), X(0), .. matcher,
             21, 64, X(1), X(0), 19,
             1, U(5), 24, F(3),
             1, U(6), 22, 64, A(8), X(0), 19],
            labels: 7,
            arity: 0,
            importFunction: function,
            importArity: arity
        );

    private static object Plan(object code)
    {
        object function = ((IEnumerable)Property(code, Exports)).Cast<object>().Single();
        object[] instructions = ((IEnumerable)Property(code, Instructions)).Cast<object>().ToArray();
        int scan = Array.FindIndex(instructions, instruction => (string)Property(Property(instruction, Definition), Name) == Loop);

        return Call(
            Type(Planner),
            Build,
            code,
            function,
            scan
        );
    }

    internal static Task CandidateRollbackAndOpaqueSlots()
    {
        object code = Code(Image([11, F(5), U(0), I(1), X(0), X(1),
            43, F(5), X(1), A(7), 16, U(3), U(2), 164, X(1), 0x17, U(2), X(0), A(8)]));
        object plan = Plan(code);
        object before = New(BeamBinaryHostNames.RegisterState);
        Term sentinel = Term.I(37);
        Call(
            before,
            BeamBinaryHostNames.Write,
            0,
            Nil.Value
        );
        Call(
            before,
            BeamBinaryHostNames.Write,
            1,
            sentinel
        );
        object context = New(BeamBinaryHostNames.MatchContext, new BitString([0x81]));
        Call(
            before,
            BeamBinaryHostNames.WriteContext,
            Op(BeamBinaryHostNames.X, 3),
            context
        );
        object position = New(BinaryPositionType, new BitString([0x82]), 0);
        Call(
            before,
            WritePosition,
            Op(BeamBinaryHostNames.X, 4),
            position
        );
        Pid self = new("pure@host", 1);
        Term rejected = Term.Tuple(Term.A(BeamOpcodeEdgeNames.Other), Term.I(1));
        Require(Call(
            Type(Matcher),
            Select,
            code,
            plan,
            before,
            rejected,
            self
        ) is null);
        Require(ReferenceEquals(Call(before, BeamBinaryHostNames.Read, 1), sentinel));
        Term accepted = Term.Tuple(Term.A(BeamOpcodeEdgeNames.Marker), Term.I(2));
        object selected = Call(
            Type(Matcher),
            Select,
            code,
            plan,
            before,
            accepted,
            self
        );
        object registers = Property(selected, Registers);
        Require(((Term)Call(registers, BeamBinaryHostNames.Read, 1)).Equals(Term.Tuple(accepted, Term.A(BeamOpcodeEdgeNames.Other))));
        Require(ReferenceEquals(Call(before, BeamBinaryHostNames.Read, 1), sentinel));
        Refuses<InvalidDataException>(() => Call(registers, BeamBinaryHostNames.Read, 3));
        Refuses<InvalidDataException>(() => Call(registers, BeamBinaryHostNames.Read, 4));
        Require((int)Property(context, BeamBinaryHostNames.Position) == 0);

        return Task.CompletedTask;
    }

    internal static Task PureBoundariesAndCallbackRefusal()
    {
        Require(((Integer)Call(
            Type(Pure),
            Unary,
            ErlangModuleNames.ByteSize,
            new BitString([0x80], 1)
        )).Value == 1);
        Require(((Term)Call(
            Type(Pure),
            Element,
            Term.I(2),
            Term.Tuple(Term.I(1), Term.I(2))
        )).Equals(Term.I(2)));
        Refuses<ErlangException>(() => Call(
            Type(Pure),
            Element,
            Term.I(0),
            Term.Tuple(Term.I(1))
        ));
        Refuses<ErlangException>(() => Call(
            Type(Pure),
            Unary,
            ErlangModuleNames.ByteSize,
            Nil.Value
        ));
        Refuses<NotSupportedException>(() => Plan(Code(Image([10, F(5), U(0), X(0), X(1)], BeamOpcodeEdgeNames.Length, 1))));
        Refuses<NotSupportedException>(() => Plan(Code(Image([75, U(0)]))));
        Refuses<NotSupportedException>(() => Plan(Code(Image([166, F(5), X(0), U(1), X(1)]))));

        return Task.CompletedTask;
    }
}
