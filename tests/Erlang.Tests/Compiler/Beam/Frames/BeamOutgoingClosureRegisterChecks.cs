using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamOutgoingClosureRegisterChecks
{
    internal const string TestName = "compiler/beam/forwards-closure-to-outgoing-register";
    private const string Module = "beam_outgoing_closure_register_fixture";
    private const string Pass = "ForwardOutgoingClosureRegisters";
    private const string MissingForwarding = "Expected the closure to capture the old outgoing value and replace it directly.";
    private const string InvalidForwarding = "The closure copy was removed across a live register or collecting boundary.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler(Module);
        byte[] independent = [Move, X(3), X(2)];
        byte[] overwritten = [Move, A(1), X(1)];
        AssertForwarded(compiler, independent, overwritten);
        AssertForwarded(
            compiler,
            independent,
            [],
            arity: 1
        );

        AssertUnchanged(compiler, Fixture(beforeCopy: [Move, X(0), X(2)]));
        AssertUnchanged(compiler, Fixture(beforeCopy: [Move, X(3), X(0)]));
        AssertUnchanged(compiler, Fixture(beforeCopy: [Move, X(1), X(2)]));
        AssertUnchanged(compiler, Fixture(beforeCopy: [TestHeap, U(1), U(4)]));
        AssertUnchanged(compiler, Fixture(beforeCopy: [Label, U(3)]));
        AssertUnchanged(compiler, Fixture(afterCopy: [Move, X(1), X(2), .. overwritten]));
        AssertUnchanged(compiler, Fixture(afterCopy: [Move, X(3), X(0), .. overwritten]));
        AssertUnchanged(compiler, Fixture());

        return Task.CompletedTask;
    }

    private static void AssertForwarded(
        BeamModuleCompiler compiler,
        byte[] before,
        byte[] after,
        int arity = 3
    )
    {
        if (Rewrite(compiler, Fixture(
                beforeCopy: before,
                afterCopy: after,
                arity: arity
            )) is not { } rewritten
            || !rewritten.SequenceEqual(Fixture(
                forward: true,
                beforeCopy: before,
                afterCopy: after,
                arity: arity
            )))
        {
            throw new InvalidOperationException(MissingForwarding);
        }
    }

    private static void AssertUnchanged(BeamModuleCompiler compiler, byte[] code)
    {
        if (Rewrite(compiler, code) is not null)
        {
            throw new InvalidOperationException(InvalidForwarding);
        }
    }

    private static byte[]? Rewrite(BeamModuleCompiler compiler, ReadOnlyMemory<byte> code) =>
        (byte[]?)Call(
            compiler,
            Pass,
            code,
            5
        );

    private static byte[] Fixture(
        bool forward = false,
        byte[]? beforeCopy = null,
        byte[]? afterCopy = null,
        int arity = 3
    )
    {
        byte[] copy = forward ? [] : [Move, X(1), X(0)];

        return
        [
            .. Start(
                1,
                2,
                1,
                4
            ),
            TestHeap, (byte)(U(AllocationList) | ExtendedTag), U(AllocationKinds),
            U(AllocationWords), U(0), U(AllocationFloats), U(0), U(AllocationFuns), U(1), U(4),
            MakeFun, U(0), forward ? X(0) : X(1), ExtendedListOperand, U(1), X(0),
            .. (beforeCopy ?? []),
            .. copy,
            .. (afterCopy ?? []),
            Line, U(1),
            TailExternalCall, U(arity), U(0),
            EndCode
        ];
    }
}
