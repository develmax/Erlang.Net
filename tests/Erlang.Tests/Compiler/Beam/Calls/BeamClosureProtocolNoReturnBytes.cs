using static Erlang.Tests.BeamOpcodeEdgeBytes;
using static Erlang.Tests.BeamReceiveMarkerTypeHostNames;
using static Erlang.Tests.BeamClosureProtocolNames;

namespace Erlang.Tests;

internal static class BeamClosureProtocolNoReturnBytes
{
    internal static byte[] Image(BeamClosureProtocolShape shape)
    {
        // Synthetic admission images, never claimed as original OTP programs.
        // The caller has no normal continuation: only a proven no-return callee
        // can keep its following handler label from becoming an ordinary edge.
        byte[] caller = [.. Start(
            RunInfo,
            RunEntry,
            RunAtom,
            1
        ),
            Allocate, U(1), U(1), Try, Y(0), F(RunHandler),
            Call, U(1), F(MiddleEntry),
            Label, U(RunHandler), TryCase, Y(0), Move, A(RetainedAtom), X(0), Deallocate, U(1), Return];
        byte[] middle = shape switch
        {
            BeamClosureProtocolShape.LocalCycle => [CallOnly, U(1), F(MiddleEntry)],
            BeamClosureProtocolShape.CaughtReturn => [
                Allocate, U(1), U(1), Try, Y(0), F(FirstArm),
                ExternalCall, U(1), U(0),
                Label, U(FirstArm), TryCase, Y(0), Move, A(ReturnedAtom), X(0), Deallocate, U(1), Return],
            _ => Branches(shape)
        };

        return BeamOpcodeEdgeBytes.Image(
            [.. caller, .. Start(
                MiddleInfo,
                MiddleEntry,
                MiddleAtom,
                1
            ), .. middle],
            labels: LabelCount,
            functions: FunctionCount,
            arity: 1,
            importFunction: ErrorImport
        );
    }

    private static byte[] Branches(BeamClosureProtocolShape shape)
    {
        int fallback = shape == BeamClosureProtocolShape.DefaultReturns ? SharedArm
            : shape == BeamClosureProtocolShape.EntryCycle ? MiddleEntry : MiddleInfo;
        byte[] first = shape == BeamClosureProtocolShape.SharedDag
            ? [Jump, F(SharedArm)] : [ExternalTail, U(1), U(0)];
        byte[] second = shape == BeamClosureProtocolShape.SharedDag
            ? [Jump, F(SharedArm)]
            : shape == BeamClosureProtocolShape.OneReturns ? [Move, A(ReturnedAtom), X(0), Return]
            : [ExternalTail, U(1), U(0)];
        byte[] common = shape == BeamClosureProtocolShape.SharedDag
            ? [Label, U(SharedArm), ExternalTail, U(1), U(0)]
            : shape == BeamClosureProtocolShape.DefaultReturns ? [Label, U(SharedArm), Move, A(ReturnedAtom), X(0), Return]
            : [];

        return [Select, X(0), F(fallback), ArgumentList, U(4), A(RetainedAtom), F(FirstArm), A(ReturnedAtom), F(SecondArm),
            Label, U(FirstArm), .. first, Label, U(SecondArm), .. second, .. common];
    }
}
