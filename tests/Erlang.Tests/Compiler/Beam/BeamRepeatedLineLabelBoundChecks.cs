using System.Reflection;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamRepeatedLineLabelBoundChecks
{
    internal const string TestName = "compiler/beam/removes-repeated-lines-with-rewritten-label-count";
    private const int PreviousLabelCount = 5;
    private const int PhysicalLabelCount = PreviousLabelCount + 1;
    private const string IncorrectLine = "Expected line cleanup to use the current physical label range.";

    internal static Task Run()
    {
        CheckFixture(ending: [Move, A(1), X(0), Return], location: 1);
        CheckFixture(ending: [TailLocalCall, U(0), F(2)], location: 0);

        return Task.CompletedTask;
    }

    private static void CheckFixture(byte[] ending, int location)
    {
        byte[] prefix =
        [
            .. Start(
                1,
                2,
                1,
                0
            ),
            Line, U(location),
            Jump, F(PreviousLabelCount),
            Label, U(PreviousLabelCount)
        ];
        byte[] original = [.. prefix, Line, U(location), .. ending, EndCode];
        byte[] expected = [.. prefix, .. ending, EndCode];
        var compiler = new BeamModuleCompiler("beam_repeated_line_label_bound_probe");
        SetCompilerField(compiler, "nextLabel", PreviousLabelCount);
        SetCompilerField(compiler, "lineInstructions", 2);
        if (!Rewrite(compiler, original, PreviousLabelCount).Span.SequenceEqual(original)
            || !Rewrite(compiler, original, PhysicalLabelCount).Span.SequenceEqual(expected))
        {
            throw new InvalidOperationException(IncorrectLine);
        }
    }

    private static void SetCompilerField(BeamModuleCompiler compiler, string name, int value)
    {
        typeof(BeamModuleCompiler).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(compiler, value);
    }

    private static ReadOnlyMemory<byte> Rewrite(BeamModuleCompiler compiler, ReadOnlyMemory<byte> code, int labelCount)
        => (ReadOnlyMemory<byte>)Call(
            compiler,
            "RemoveRepeatedLines",
            code,
            labelCount
        );
}
