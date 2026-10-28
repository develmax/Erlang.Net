using static Erlang.Tests.BeamReceiveMap397Names;
using System.Collections;
using Erlang;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamReceiveMap397Checks
{

    private static byte[] Image(byte[] matcher, bool acceptMissing = false)
        => BeamOpcodeEdgeBytes.Image(
            [.. Start(
                1,
                2,
                2,
                0
            ), 1, U(3), 23, F(6), X(0), .. matcher,
             21, 64, X(1), X(0), 19,
             1, U(4), .. (acceptMissing ? new byte[] { 21, 64, X(1), X(0), 19 } : new byte[] { 24, F(3) }),
             1, U(5), 24, F(3),
             1, U(6), 22, 64, A(8), X(0), 19],
            labels: 7,
            arity: 0
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

    private static MapTerm Map(params (Term Key, Term Value)[] entries)
        => new(entries.Select(entry => new KeyValuePair<Term, Term>(entry.Key, entry.Value)));

    internal static Task AliasAndCandidateRollback()
    {
        // Wire opcode 156 is is_map; 158 is get_map_elements. The source
        // aliases the first destination, so all values must be read first.
        object code = Code(Image([156, F(5), X(0), 158, F(5), X(0), 0x17, U(4), A(7), X(0), A(8), X(1)]));
        object plan = Plan(code);
        object before = New(BeamBinaryHostNames.RegisterState);
        Term sentinel = Term.I(37);
        Call(
            before,
            BeamBinaryHostNames.Write,
            1,
            sentinel
        );
        Term retained = Term.A(BeamOpcodeEdgeNames.Marker);
        Term returned = Term.A(BeamOpcodeEdgeNames.Other);
        Pid self = new(NodeName, 1);
        Require(Call(
            Type(Matcher),
            Select,
            code,
            plan,
            before,
            Map((retained, Term.I(1))),
            self
        ) is null);
        Require(ReferenceEquals(Call(before, BeamBinaryHostNames.Read, 1), sentinel));
        object selection = Call(
            Type(Matcher),
            Select,
            code,
            plan,
            before,
            Map((retained, Term.I(1)), (returned, Term.I(2))),
            self
        );
        using var selectionOwner507 = (IDisposable)selection;
        object registers = Call(selection, BeamReceiveSelectionHostNames.TakeRegisters);
        using var registersOwner507 = (IDisposable)registers;
        Require(((Term)Call(registers, BeamBinaryHostNames.Read, 0)).Equals(Term.I(1)));
        Require(((Term)Call(registers, BeamBinaryHostNames.Read, 1)).Equals(Term.I(2)));
        Require(ReferenceEquals(Call(before, BeamBinaryHostNames.Read, 1), sentinel));

        return Task.CompletedTask;
    }

    internal static Task FailureWritesNoDestinationsAndOpaqueReadRefused()
    {
        object code = Code(Image([156, F(5), X(0), 158, F(4), X(0), 0x17, U(4), A(7), X(1), A(8), X(2)], true));
        object plan = Plan(code);
        object before = New(BeamBinaryHostNames.RegisterState);
        Term sentinel = Term.I(37);
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
            Op(BeamBinaryHostNames.X, 2),
            context
        );
        object position = New(BinaryPositionType, new BitString([0x82]), 0);
        Call(
            before,
            WritePosition,
            Op(BeamBinaryHostNames.X, 3),
            position
        );
        Pid self = new(NodeName, 1);
        object selection = Call(
            Type(Matcher),
            Select,
            code,
            plan,
            before,
            Map((Term.A(BeamOpcodeEdgeNames.Marker), Term.I(1))),
            self
        );
        using var selectionOwner507 = (IDisposable)selection;
        object registers = Call(selection, BeamReceiveSelectionHostNames.TakeRegisters);
        using var registersOwner507 = (IDisposable)registers;
        Require(ReferenceEquals(Call(registers, BeamBinaryHostNames.Read, 1), sentinel));
        Refuses<InvalidDataException>(() => Call(registers, BeamBinaryHostNames.Read, 2));
        Refuses<InvalidDataException>(() => Call(registers, BeamBinaryHostNames.Read, 3));
        object opaqueCode = Code(Image([158, F(5), X(2), 0x17, U(2), A(7), X(1)]));
        object opaquePlan = Plan(opaqueCode);
        Refuses<InvalidDataException>(() => Call(
            Type(Matcher),
            Select,
            opaqueCode,
            opaquePlan,
            before,
            Nil.Value,
            self
        ));
        Require((int)Property(context, BeamBinaryHostNames.Position) == 0);

        return Task.CompletedTask;
    }

    internal static Task FrameDestinationAndCallbackRemainRefused()
    {
        Refuses<NotSupportedException>(() => Plan(Code(Image([158, F(5), X(0), 0x17, U(2), A(7), Y(0)]))));
        Refuses<NotSupportedException>(() => Plan(Code(Image([75, U(0)]))));
        Refuses<NotSupportedException>(() => Plan(Code(Image([166, F(5), X(0), U(1), X(1)]))));

        return Task.CompletedTask;
    }
}
