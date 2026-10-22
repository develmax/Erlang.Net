using System.Collections;
using Erlang;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamCallbackPlan399Bytes;

using static Erlang.Tests.BeamCallbackPlan399Names;

namespace Erlang.Tests;

internal static class BeamCallbackPlan399Checks
{
    private static object Entry(object code)
        => ((IEnumerable)Property(code, Exports)).Cast<object>().Single();

    private static object Plan(object code)
    {
        object[] instructions = ((IEnumerable)Property(code, Instructions)).Cast<object>().ToArray();
        int scan = Array.FindIndex(
            instructions,
            instruction => (string)Property(Property(instruction, Definition), InstructionName) == LoopReceiveOpcode
        );

        return Call(
            Type(PlannerType),
            BuildMethod,
            code,
            Entry(code),
            scan
        );
    }

    internal static Task ExactShapeAndFrameProof()
    {
        object code = Code(Image());
        object plan = Plan(code);
        Require(Property(plan, Callback) is not null);
        Call(
            Type(RegisterValidationType),
            ValidateMethod,
            code,
            Entry(code)
        );
        Refuses<NotSupportedException>(() => Plan(Code(Image(callArity: 0))));
        Refuses<NotSupportedException>(() => Plan(Code(Image(atom: 7))));
        object invalidFrame = Code(Image(calleeY: 2));
        Refuses<InvalidDataException>(() => Call(
            Type(RegisterValidationType),
            ValidateMethod,
            invalidFrame,
            Entry(invalidFrame)
        ));
        object registers = New(BeamBinaryHostNames.RegisterState);
        Call(
            registers,
            BeamBinaryHostNames.Write,
            1,
            Term.I(0)
        );
        Call(
            registers,
            BeamBinaryHostNames.WriteContext,
            Op(BeamBinaryHostNames.X, 0),
            New(BeamBinaryHostNames.MatchContext, new BitString([1]))
        );
        Refuses<InvalidDataException>(() => Call(
            Type(EntryProtocolType),
            RequireEntryMethod,
            code,
            Entry(code),
            registers
        ));
        Call(
            registers,
            WritePositionMethod,
            Op(BeamBinaryHostNames.X, 0),
            New(
                PositionType,
                new BitString([1]),
                0
            )
        );
        Refuses<InvalidDataException>(() => Call(
            Type(EntryProtocolType),
            RequireEntryMethod,
            code,
            Entry(code),
            registers
        ));

        return Task.CompletedTask;
    }
}
