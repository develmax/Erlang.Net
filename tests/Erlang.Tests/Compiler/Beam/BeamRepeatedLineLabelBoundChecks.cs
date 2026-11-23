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
    private const int FinalWriteLabel = 3;
    private const string IncorrectFinalLocation = "Final duplicate cleanup must preserve distinct and protected source locations.";
    private const string IncorrectLine = "Expected line cleanup to use the current physical label range.";

    internal static Task Run()
    {
        CheckFixture(ending: [Move, A(1), X(0), Return], location: 1);
        CheckFixture(ending: [TailLocalCall, U(0), F(2)], location: 0);
        CheckFinalWrite([], secondLocation: 1, removeDuplicate: true);
        CheckFinalWrite([], secondLocation: 2, removeDuplicate: false);
        CheckFinalWrite([DebugLine, A(1), U(1), U(1), U(1)], secondLocation: 1, removeDuplicate: false);
        CheckFinalWrite([ExecutableLine, U(1), U(1)], secondLocation: 1, removeDuplicate: false);
        CheckFinalWrite([Allocate, U(1), U(0), CatchEnter, Y(0), F(FinalWriteLabel)],
            secondLocation: 1,
            removeDuplicate: false
        );

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

    private static void CheckFinalWrite(byte[] markers, int secondLocation, bool removeDuplicate)
    {
        byte[] prefix =
        [
            .. Start(
                1,
                2,
                1,
                0
            ),
            .. markers,
            Line, U(1), Jump, F(FinalWriteLabel), Label, U(FinalWriteLabel)
        ];
        byte[] original = [.. prefix, Line, U(secondLocation), TailLocalCall, U(0), F(2), EndCode];
        byte[] expected = removeDuplicate ? [.. prefix, TailLocalCall, U(0), F(2), EndCode] : original;
        var compiler = new BeamModuleCompiler("beam_final_repeated_locations_probe");
        SetCompilerField(compiler, "nextLabel", PhysicalLabelCount);
        SetCompilerField(compiler, "highestOpcode", (int)DebugLine);
        SetCompilerField(compiler, "lineInstructions", 2);
        using var output = new MemoryStream();
        _ = Call(
            compiler,
            "WriteCode",
            output,
            new ReadOnlyMemory<byte>(original),
            PhysicalLabelCount
        );
        int lineCount = (int)typeof(BeamModuleCompiler)
            .GetField("lineInstructions", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(compiler)!;
        if (!output.ToArray().AsSpan(sizeof(uint) + CodeHeaderSize).SequenceEqual(expected)
            || lineCount != (removeDuplicate ? 1 : 2))
        {
            throw new InvalidOperationException(IncorrectFinalLocation);
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
