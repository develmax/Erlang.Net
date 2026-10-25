using static Erlang.Tests.BeamOpcodeEdgeBytes;
using static Erlang.Tests.BeamClosedWait412Names;

namespace Erlang.Tests;

internal static class BeamClosedWait480Bytes
{
    internal static byte[] Image(
        bool returning = false,
        bool foreign = false,
        bool roots = false,
        bool unknown = false
    )
    {
        byte[] caller = [.. Start(
            RunInfo,
            RunEntry,
            RunAtom,
            1
        ),
            Allocate, U(1), U(1), Try, Y(0), F(Handler), Call, U(1), F(MiddleEntry),
            Label, U(Handler), TryCase, Y(0), Move, A(MarkerAtom), X(0), Deallocate, U(1), Return];
        byte[] frame = roots ? [Allocate, U(1), U(1)] : [];
        byte[] finish = returning ? [Return] : [ExternalTail, U(1), U(0)];
        byte[] middle = [.. Start(
            MiddleInfo,
            MiddleEntry,
            MiddleAtom,
            1
        ), .. frame,
            Label, U(Scan), Loop, F(Empty), X(0),
            BeamTupleSelectHostNames.TupleTestOpcode, F(Rejected), X(0),
            BeamTupleSelectHostNames.SelectOpcode, X(0), F(Rejected), BeamTupleSelectHostNames.ExtendedList,
            U(2), U(1), F(foreign ? Handler : BeamClosedWait480Names.Accepted),
            Label, U(BeamClosedWait480Names.Accepted), Remove, .. finish,
            Label, U(Rejected), LoopEnd, F(Scan), Label, U(Empty), Wait, F(Scan)];

        return BeamOpcodeEdgeBytes.Image(
            [.. caller, .. middle],
            labels: BeamClosedWait480Names.LabelCount,
            functions: 2,
            importFunction: unknown ? LengthFunction : ErrorFunction
        );
    }
}
