using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamClosedTaggedAtomGuardChecks
{
    internal const string TestName = "compiler/beam/selects-distinct-atom-guards-after-a-common-tag-check";
    private const string Failure = "The exclusive guard pair should form a sorted two-key selection.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_closed_tagged_atom_guard");
        byte on = A((int)Call(compiler, "Intern", "omega"));
        byte off = A((int)Call(compiler, "Intern", "alpha"));
        byte[] first = [ExactEqualTest, F(3), X(1), on, Move, I(1), X(0), Return];
        byte[] retry = [Label, U(3), ExactEqualTest, F(4), X(1), off, Move, I(2), X(0), Return];
        byte[] fallback = [Label, U(4), Move, I(3), X(0), Return, EndCode];
        byte[] code = [.. Start(
            1,
            2,
            1,
            2
        ), IsTaggedTuple, F(4), X(0), U(2), A(1), .. first, .. retry, .. fallback];
        byte[] expected =
        [
            .. Start(
                1,
                2,
                1,
                2
            ), IsTaggedTuple, F(5), X(0), U(2), A(1),
            SelectValue, X(1), F(5), ExtendedListOperand, U(4), off, F(4), on, F(3),
            Label, U(3), Move, I(1), X(0), Return,
            Label, U(4), Move, I(2), X(0), Return,
            Label, U(5), Move, I(3), X(0), Return, EndCode
        ];
        byte[] competing =
        [
            .. Start(
                1,
                2,
                1,
                2
            ), IsAtom, F(3), X(1), IsTaggedTuple, F(4), X(0), U(2), A(1),
            .. first, .. retry, .. fallback
        ];
        if (Rewrite(code) is not { } actual || actual.LabelCount != 6 || !actual.Code.Span.SequenceEqual(expected)
            || Rewrite(competing) is not null)
        {
            throw new InvalidOperationException(Failure);
        }

        return Task.CompletedTask;

        (ReadOnlyMemory<byte> Code, int LabelCount)? Rewrite(ReadOnlyMemory<byte> input) =>
            ((ReadOnlyMemory<byte>, int)?)Call(
                compiler,
                "SelectClosedTaggedAtomGuards",
                input,
                5
            );
    }
}
