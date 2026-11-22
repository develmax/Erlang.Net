using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamSavedChoiceFrameChecks
{
    internal const string TestName = "compiler/beam/keeps-a-selected-saved-value-in-its-frame-slot";
    private const string Incorrect = "Only the updated choice should write the retained slot before the shared continuation.";
    private const string Unsafe = "An observed scratch register or a shared join must preserve both incoming definitions.";
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte Three = 0x30;
    private const byte Four = 0x40;
    private const byte X0 = 0x03;
    private const byte Y0 = 0x04;
    private const byte Y1 = 0x14;
    private const byte Module = 0x12;
    private const byte Function = 0x22;
    private const byte True = 0x32;
    private const byte Fresh = 0x42;
    private const byte Rejected = 0x35;
    private const byte Joined = 0x45;

    internal static Task Run()
    {
        byte[] entry =
        [
            Label, One, FunctionInfo, Module, Function, One, Label, Two,
            Allocate, Two, One, Move, X0, Y0, Move, X0, Y1
        ];
        byte[] check = [ExactEqualTest, Rejected, X0, True, Move, Fresh, X0];
        byte[] branches = [Jump, Joined, Label, Three, Move, Y0, X0, Label, Four, Move, X0, Y0];
        byte[] finish = [Move, Y1, X0, Deallocate, Two, Return, EndCode];
        byte[] source = [.. entry, .. check, .. branches, .. finish];
        byte[] expected = [.. entry, .. check, Move, X0, Y0, Label, Three, .. finish];
        if (Rewrite(source) is not { } actual || !actual.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(Incorrect);
        }

        byte[] used = [.. entry, .. check, .. branches, Move, X0, Y1, Deallocate, Two, Return, EndCode];
        byte[] shared = [.. entry, ExactNotEqualTest, Joined, X0, Fresh, .. check, .. branches, .. finish];
        if (Rewrite(used) is not null || Rewrite(shared) is not null)
        {
            throw new InvalidOperationException(Unsafe);
        }

        return Task.CompletedTask;
    }

    private static byte[]? Rewrite(byte[] source)
    {
        ReadOnlyMemory<byte> instructions = source;
        object header = New(
            "BeamCodeHeader",
            0,
            (int)Line,
            5,
            1,
            instructions
        );
        var references = new List<(int Start, int End, int Label)>();
        Action<int, int, int> recordLabel = (start, end, label) => references.Add((start, end, label));
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            5,
            0,
            recordLabel,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "KeepSavedChoiceInFrame",
            instructions,
            body,
            references
        );
    }
}
