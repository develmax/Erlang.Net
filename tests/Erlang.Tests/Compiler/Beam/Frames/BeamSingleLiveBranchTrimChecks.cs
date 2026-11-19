using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamSingleLiveBranchTrimChecks
{
    internal const string TestName = "compiler/beam/trims-owned-branch-with-one-retained-result";
    private const string MissingTrim = "Expected the final branch call to retain only its saved result on the stack.";
    private const string InvalidTrim = "A branch trim changed a live higher slot or a join with distinct frame owners.";
    private const byte X0 = 0x03;
    private const byte X1 = 0x13;
    private const byte Y0 = 0x04;
    private const byte Y1 = 0x14;
    private const byte LabelOne = 0x10;
    private const byte LabelThree = 0x30;
    private const byte LabelFour = 0x40;
    private const byte TargetOne = 0x15;
    private const byte TargetThree = 0x35;
    private const byte TargetFour = 0x45;
    private const byte One = 0x10;
    private const byte Two = 0x20;

    internal static Task Run()
    {
        byte[] frame = [Allocate, Two, Two, InitYRegisters, ExtendedListOperand, One, Y0, Move, X1, Y1];
        byte[] branch = [IsNil, TargetThree, X0, Jump, TargetThree, Label, LabelThree];
        byte[] saveAndLoad = [Move, X0, Y0, Move, Y1, X0];
        byte[] call = [Line, One, LocalCall, One, TargetOne];
        byte[] output = [Move, Y0, X0, Deallocate, Two, Return, EndCode];
        byte[] candidate = [Label, LabelOne, .. frame, .. branch, .. saveAndLoad, .. call, .. output];
        byte[] expected =
        [
            Label, LabelOne, .. frame, .. branch, .. saveAndLoad,
            Move, Y0, Y1, TrimFrame, One, One,
            .. call, Move, Y0, X0, Deallocate, One, Return, EndCode
        ];
        if (Rewrite(candidate) is not { } optimized || !optimized.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(MissingTrim);
        }

        byte[] keepsHigher =
        [
            Label, LabelOne, .. frame, .. branch, .. saveAndLoad, .. call,
            Move, Y1, X0, Deallocate, Two, Return, EndCode
        ];
        byte[] distinctOwners =
        [
            Label, LabelOne, IsNil, TargetThree, X0, .. frame, Jump, TargetFour,
            Label, LabelThree, .. frame, Label, LabelFour,
            .. saveAndLoad, .. call, .. output
        ];
        if (Rewrite(keepsHigher) is not null || Rewrite(distinctOwners) is not null)
        {
            throw new InvalidOperationException(InvalidTrim);
        }

        return Task.CompletedTask;
    }

    private static byte[]? Rewrite(byte[] source)
    {
        var compiler = new BeamModuleCompiler("beam_single_live_branch");

        return (byte[]?)Call(
            compiler,
            "TrimStackBetweenCalls",
            new ReadOnlyMemory<byte>(source),
            5
        );
    }
}
