using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamEntryTupleFunctionCallChecks
{
    internal const string TestName = "compiler/beam/schedules-checked-entry-tuple-operands-after-frame-allocation";
    private const string Module = "beam_entry_tuple_function";
    private const string Pass = "ScheduleEntryTupleFunctionCall";

    internal static Task Run()
    {
        byte[] original = Fixture();
        byte[] expected = Fixture(scheduled: true);
        if (Rewrite(original) is not { } actual || !actual.SequenceEqual(expected))
        {
            throw new InvalidOperationException(BeamEntryTupleFunctionCallDiagnostics.Schedule);
        }

        CheckUnchanged(expected);
        CheckUnchanged(Fixture(extraLive: 1));
        CheckUnchanged(Fixture(arityFailure: 1));
        CheckUnchanged(Fixture(beforeCall: [Move, X(2), X(1)]));

        return Task.CompletedTask;
    }

    private static void CheckUnchanged(byte[] input)
    {
        if (Rewrite(input) is not null)
        {
            throw new InvalidOperationException(BeamEntryTupleFunctionCallDiagnostics.Excluded);
        }
    }

    private static byte[]? Rewrite(ReadOnlyMemory<byte> code)
        => (byte[]?)Call(
            new BeamModuleCompiler(Module),
            Pass,
            code,
            4
        );

    private static byte[] Fixture(
        bool scheduled = false,
        int extraLive = 0,
        int arityFailure = 3,
        byte[]? beforeCall = null
    )
    {
        byte[] staged = scheduled
            ? []
            : [GetTupleElement, X(0), U(0), X(2), GetTupleElement, X(0), U(1), X(3)];
        byte[] arguments = scheduled
            ? [GetTupleElement, X(0), U(0), X(1), GetTupleElement, X(0), U(1), X(0)]
            : [Move, X(3), X(0), Move, X(2), X(1)];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            IsTuple, F(3), X(0),
            TestArity, F(arityFailure), X(0), U(2),
            .. staged,
            Allocate, U(1), U((scheduled ? 2 : 4) + extraLive),
            Move, X(1), Y(0),
            .. arguments,
            .. (beforeCall ?? []),
            Line, U(1),
            CallFun, U(1),
            Move, Y(0), X(1),
            Deallocate, U(1), Return,
            Label, U(3),
            Jump, F(1),
            EndCode
        ];
    }
}
