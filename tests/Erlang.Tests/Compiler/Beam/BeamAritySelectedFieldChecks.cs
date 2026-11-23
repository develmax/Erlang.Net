using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamAritySelectedFieldChecks
{
    internal const string TestName = "compiler/beam/shares-a-tuple-field-across-tag-arms";

    internal static Task Run()
    {
        if (Rewrite(Layout(shared: false)) is not { } result || !result.SequenceEqual(Layout(shared: true))
            || Rewrite(Layout(shared: false, readsOriginalOnFailure: true)) is not null
            || Rewrite(result) is not null)
        {
            throw new InvalidOperationException("A checked tuple field must be shared only when the original register is dead on rejection.");
        }

        return Task.CompletedTask;
    }

    private static byte[] Layout(bool shared, bool readsOriginalOnFailure = false)
    {
        byte[] tag = [GetTupleElement, X(1), U(0), X(shared ? 3 : 2)];
        byte[] field = [GetTupleElement, X(1), U(1), X(2)];
        byte[] beforeBranch = shared ? field : [];
        byte[] armField = shared ? [] : field;

        return
        [
            .. Start(
                1,
                2,
                2,
                3
            ),
            IsTuple, F(7), X(1),
            SelectTupleArity, X(1), F(7), ExtendedListOperand, U(2), U(2), F(3),
            Label, U(3), .. tag, .. beforeBranch,
            SelectValue, X(shared ? 3 : 2), F(7), ExtendedListOperand, U(6), A(3), F(4), A(4), F(5), A(5), F(6),
            Label, U(4), .. armField, Move, X(2), X(0), Return,
            Label, U(5), .. armField, Move, X(2), X(0), Return,
            Label, U(6), .. armField, Move, X(2), X(0), Return,
            Label, U(7), Move, X(readsOriginalOnFailure ? 2 : 1), X(0), Return,
            EndCode
        ];
    }

    private static byte[]? Rewrite(byte[] code)
    {
        var image = new ReadOnlyMemory<byte>(code);
        object header = New(
            "BeamCodeHeader",
            0,
            MinimumHighestOpcode,
            8,
            1,
            image
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            6,
            0,
            null,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "RewriteAritySelectedTupleFields",
            image,
            body
        );
    }
}
