using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamCoveredCallFrameInitializationChecks
{
    internal const string TestName = "compiler/beam/elides-a-covered-frame-clear-between-ordinary-calls";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("covered_call_frame_initialization");
        byte[] frame =
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            Allocate, U(2), U(1),
            InitYRegisters, ExtendedListOperand, U(2), Y(0), Y(1),
            LocalCall, U(1), F(2),
            GetTupleElement, X(0), U(0), Y(1),
            GetTupleElement, X(0), U(1), X(0)
        ];
        byte[] repeated = [InitYRegisters, ExtendedListOperand, U(1), Y(0)];
        byte[] finish = [ExternalCall, U(1), U(0), Deallocate, U(2), Return, EndCode];

        byte[] edited = [.. frame, .. finish];
        if (Apply([.. frame, .. repeated, .. finish]) is not { } result
            || !result.AsSpan().SequenceEqual(edited)
            || Apply(edited) is not null)
        {
            throw new InvalidOperationException("A nil slot stays initialized across the first ordinary call.");
        }

        byte[] overwrite = [Move, X(0), Y(0)];
        byte[] clearsOtherSlot = [InitYRegisters, ExtendedListOperand, U(2), Y(0), Y(1)];
        if (Apply([.. frame, .. overwrite, .. repeated, .. finish]) is not null
            || Apply([.. frame, .. clearsOtherSlot, .. finish]) is not null
            || Apply([.. frame, Label, U(3), .. repeated, .. finish]) is not null)
        {
            throw new InvalidOperationException("A write or a possible alternate predecessor invalidates the nil proof.");
        }

        return Task.CompletedTask;

        byte[]? Apply(byte[] bytes) => (byte[]?)Call(
            compiler,
            "RemoveCoveredCallFrameInitializations",
            (ReadOnlyMemory<byte>)bytes,
            4
        );
    }
}
