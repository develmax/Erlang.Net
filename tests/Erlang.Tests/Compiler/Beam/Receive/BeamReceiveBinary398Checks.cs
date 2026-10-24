using static Erlang.Tests.BeamReceiveBinary398Names;
using System.Collections;
using Erlang;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamBinaryHostBytes;

namespace Erlang.Tests;

internal static class BeamReceiveBinary398Checks
{

    private static byte[] Pair(int tail = 0, bool overwriteContext = false)
        => Join(
            Instruction(
                166,
                F(5),
                X(0),
                U(2),
                X(0)
            ),
            Instruction(
                BeamBinaryHostNames.MatchOpcode,
                F(5),
                X(0),
                [0x17, .. U(13), .. A(BeamBinaryHostNames.EnsureAtom), .. U(32), .. U(8),
                 .. A(BeamBinaryHostNames.IntegerAtom), .. U(2), .. A(0), .. U(32), .. U(1), .. X(overwriteContext ? 0 : 2),
                 .. A(BeamBinaryHostNames.GetTailAtom), .. U(3), .. U(8), .. X(tail)]
            )
        );

    private static byte[] Image(byte[] pair, bool rejectConsumes = false)
        => BeamBinaryHostBytes.Image(
            Join(
                Instruction(64, A(7), X(1)),
                Instruction(1, U(3)),
                Instruction(23, F(6), X(0)),
                pair,
                Instruction(
                    43,
                    F(5),
                    X(2),
                    [BeamOpcodeEdgeBytes.I(1)]
                ),
                [21, 19],
                Instruction(1, U(5)),
                rejectConsumes ? new byte[] { 21, 19 } : Instruction(24, F(3)),
                Instruction(1, U(6)),
                [22],
                Instruction(64, A(8), X(0)),
                [19]
            ),
            arity: 0,
            labels: 7
        );

    private static object Entry(object code)
        => ((IEnumerable)Property(code, Exports)).Cast<object>().Single();

    private static object Plan(object code)
    {
        object[] instructions = ((IEnumerable)Property(code, Instructions)).Cast<object>().ToArray();
        int scan = Array.FindIndex(instructions, instruction => (string)Property(Property(instruction, Definition), Name) == Loop);

        return Call(
            Type(Planner),
            Build,
            code,
            Entry(code),
            scan
        );
    }

    internal static Task FreshContextAndCandidateRollback()
    {
        object code = Code(Image(Pair()));
        object plan = Plan(code);
        Call(
            Type(RoleBeamRegisterValidation),
            RoleValidate,
            code,
            Entry(code)
        );
        object before = New(BeamBinaryHostNames.RegisterState);
        Call(
            before,
            BeamBinaryHostNames.Write,
            1,
            Term.I(37)
        );
        var source = new BitString([0x81, 0x82]);
        object retained = New(BeamBinaryHostNames.MatchContext, source);
        SetPosition(retained, 3);
        Call(
            before,
            BeamBinaryHostNames.WriteContext,
            Op(BeamBinaryHostNames.X, 4),
            retained
        );
        object position = New(PositionType, source, 3);
        Call(
            before,
            WriteBinaryPosition,
            Op(BeamBinaryHostNames.X, 5),
            position
        );
        Pid self = new(NodeName, 1);
        Require(Call(
            Type(Matcher),
            Select,
            code,
            plan,
            before,
            new BitString([0, 0]),
            self
        ) is null);
        Require(Call(
            Type(Matcher),
            Select,
            code,
            plan,
            before,
            new BitString([0, 0, 0, 2, 98]),
            self
        ) is null);
        Require(Call(
            Type(Matcher),
            Select,
            code,
            plan,
            before,
            new BitString([0, 0, 0, 1, 0x80], 33),
            self
        ) is null);
        object selected = Call(
            Type(Matcher),
            Select,
            code,
            plan,
            before,
            new BitString([0, 0, 0, 1, 99]),
            self
        );
        object registers = Property(selected, Registers);
        Require(((Term)Call(registers, BeamBinaryHostNames.Read, 0)).Equals(new BitString([99])));
        Require(((Term)Call(registers, BeamBinaryHostNames.Read, 2)).Equals(Term.I(1)));
        Require(((Term)Call(before, BeamBinaryHostNames.Read, 1)).Equals(Term.I(37)));
        Require(ReferenceEquals(Call(registers, BeamBinaryHostNames.ReadContext, Op(BeamBinaryHostNames.X, 4)), retained));
        Refuses<InvalidDataException>(() => Call(registers, BeamBinaryHostNames.Read, 4));
        Refuses<InvalidDataException>(() => Call(registers, BeamBinaryHostNames.Read, 5));
        Require((int)Property(retained, BeamBinaryHostNames.Position) == 3);

        return Task.CompletedTask;
    }

    internal static Task IncomingOpaqueContextAndPositionRefused()
    {
        object code = Code(Image(Pair()));
        object[] instructions = ((IEnumerable)Property(code, Instructions)).Cast<object>().ToArray();
        int start = Array.FindIndex(instructions, instruction => (string)Property(Property(instruction, Definition), Name) == LegacyStartOpcode);
        object registers = New(BeamBinaryHostNames.RegisterState);
        Call(
            registers,
            BeamBinaryHostNames.Write,
            1,
            Term.I(37)
        );
        var source = new BitString([0, 0, 0, 1]);
        object retained = New(BeamBinaryHostNames.MatchContext, source);
        SetPosition(retained, 8);
        Call(
            registers,
            BeamBinaryHostNames.WriteContext,
            Op(BeamBinaryHostNames.X, 0),
            retained
        );
        Refuses<InvalidDataException>(() => Call(
            Type(Pairs),
            Execute,
            code,
            registers,
            start
        ));
        Require((int)Property(retained, BeamBinaryHostNames.Position) == 8);
        Call(
            registers,
            WriteBinaryPosition,
            Op(BeamBinaryHostNames.X, 0),
            New(PositionType, source, 8)
        );
        Refuses<InvalidDataException>(() => Call(
            Type(Pairs),
            Execute,
            code,
            registers,
            start
        ));

        return Task.CompletedTask;
    }

    internal static Task UnconsumedAndMutatingPairsRefused()
    {
        Refuses<NotSupportedException>(() => Plan(Code(Image(Pair(tail: 1)))));
        Refuses<NotSupportedException>(() => Plan(Code(Image(Pair(overwriteContext: true)))));
        Refuses<NotSupportedException>(() => Plan(Code(Image(Pair(), rejectConsumes: true))));
        Refuses<NotSupportedException>(() => Plan(Code(Image(Join(Instruction(
            166,
            F(5),
            X(0),
            U(2),
            X(0)
        ), [19])))));

        return Task.CompletedTask;
    }
}
