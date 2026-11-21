using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamProjectedLeafComparisonChecks
{
    internal const string TestName = "compiler/beam/projected-exact-comparison-has-no-frame";
    private const string Difference = "The projected leaf comparison frame did not retain its result and failure path.";
    private const byte X0 = 0x03;
    private const byte Y0 = 0x04;
    private const byte F0 = 0x05;
    private const byte F3 = 0x35;
    private const byte U0 = 0x00;
    private const byte U1 = 0x10;
    private const byte U2 = 0x20;
    private const byte U3 = 0x30;
    private const byte A1 = 0x12;
    private const byte A2 = 0x22;
    private const byte A3 = 0x32;
    private const byte A4 = 0x42;
    private const byte I1 = 0x11;

    internal static Task Run()
    {
        byte[] entry = [Label, U1, FunctionInfo, A1, A2, U1, Label, U2];
        byte[] frame = [Allocate, U1, U1];
        byte[] save = [Move, X0, Y0];
        byte[] compare = [BifTwo, F0, U1, Y0, A3, X0];
        byte[] direct = [BifTwo, F0, U1, X0, A3, X0];
        byte[] release = [Deallocate, U1];
        byte[] element = [Line, U1, BifTwo, F0, U0, I1, X0, X0];
        byte[] guard = [IsTaggedTuple, F3, X0, U2, A4];
        byte[] field = [GetTupleElement, X0, U1, X0];
        byte[] rejected = [Label, U3, Line, U2, BadRecord, X0, EndCode];

        Check(
            [.. entry, .. frame, InitYRegisters, ExtendedListOperand, U1, Y0, .. element,
                .. save, .. compare, .. release, Return, EndCode],
            [.. entry, .. element, .. direct, Return, EndCode]
        );
        Check(
            [.. entry, .. guard, .. frame, .. field, .. save, .. compare, .. release, Return, .. rejected],
            [.. entry, .. guard, .. field, .. direct, Return, .. rejected]
        );
        Check(
            [.. entry, .. frame, .. element, TestHeap, U2, U1, .. save, .. compare, .. release, Return, EndCode],
            null
        );
        Check(
            [.. entry, .. frame, .. element, .. save, BifTwo, F0, U2, Y0, A3, X0,
                .. release, Return, EndCode],
            null
        );

        return Task.CompletedTask;
    }

    private static void Check(byte[] source, byte[]? expected)
    {
        ReadOnlyMemory<byte> bytes = source;
        object header = New(
            "BeamCodeHeader",
            0,
            (int)DebugLine,
            4,
            1,
            bytes
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            5,
            0,
            null,
            null
        );
        byte[]? actual = Call(
            typeof(BeamModuleCompiler),
            "RewriteProjectedLeafComparison",
            bytes,
            body,
            0,
            1,
            3
        ) as byte[];
        if (expected is null ? actual is not null : actual is null || !expected.AsSpan().SequenceEqual(actual))
        {
            throw new InvalidOperationException(Difference);
        }
    }
}
