using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamClosedSelectReturnChecks
{
    internal const string TestName = "compiler/beam/shares-dominated-short-returns-after-catch";
    private const string MissingRelay = "Expected both keys to retain the same closed-frame return.";
    private const string InvalidRelay = "Expected an independently entered, line-marked, unequal or protected return to remain separate.";
    private const byte X0 = 0x03;
    private const byte X1 = 0x13;
    private const byte Y0 = 0x04;
    private const byte Zero = 0x00;
    private const byte One = 0x10;
    private const byte Two = 0x20;
    private const byte Three = 0x30;
    private const byte Four = 0x40;
    private const byte Five = 0x50;
    private const byte Six = 0x60;
    private const byte Landing = 0x35;
    private const byte FirstArm = 0x45;
    private const byte SecondArm = 0x55;
    private const byte Unmatched = 0x65;
    private const byte Module = 0x12;
    private const byte Function = 0x22;
    private const byte Accepted = 0x32;
    private const byte Alternate = 0x42;

    internal static Task Run()
    {
        byte[] prefix =
        [
            Label, One, Line, One, FunctionInfo, Module, Function, One, Label, Two,
            Allocate, One, One, CatchEnter, Y0, Landing, ExternalCall, One, Zero, Label, Three
        ];
        byte[] selector = [SelectValue, X1, Unmatched, ExtendedListOperand, Four, Accepted, FirstArm, Alternate, SecondArm];
        byte[] entry =
        [
            .. prefix, CatchEnd, Y0, IsTuple, Unmatched, X0, TestArity, Unmatched, X0, Two,
            GetTupleElement, X0, Zero, X1, .. selector
        ];
        byte[] left = [Label, Four, Deallocate, One, Return];
        byte[] right = [Label, Five, Deallocate, One, Return];
        byte[] failure = [Label, Six, Line, Two, CaseEnd, X0, EndCode];
        var relays = ReadRelays([.. entry, .. left, .. right, .. failure]);
        if (relays.Count != 1 || relays.Values.Single().Target != 5)
        {
            throw new InvalidOperationException(MissingRelay);
        }

        byte[] alternateEntry = [.. entry, .. left, .. right, Label, Six, Jump, FirstArm, EndCode];
        byte[] marked = [.. entry, .. left, Label, Five, Line, Three, Deallocate, One, Return, .. failure];
        byte[] wrongWidth = [.. entry, .. left, Label, Five, Deallocate, Two, Return, .. failure];
        byte[] active =
        [
            .. prefix, Move, X0, X1, .. selector, .. left, .. right,
            Label, Six, CatchEnd, Y0, CaseEnd, X0, EndCode
        ];
        if (ReadRelays(alternateEntry).Count != 0 || ReadRelays(marked).Count != 0
            || ReadRelays(wrongWidth).Count != 0 || ReadRelays(active).Count != 0)
        {
            throw new InvalidOperationException(InvalidRelay);
        }

        return Task.CompletedTask;
    }

    private static Dictionary<int, (int Target, int End)> ReadRelays(byte[] source)
    {
        ReadOnlyMemory<byte> instructions = source;
        object header = New(
            "BeamCodeHeader",
            0,
            (int)DebugLine,
            7,
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
        var relays = new Dictionary<int, (int Target, int End)>();
        Call(
            typeof(BeamModuleCompiler),
            "CollectClosedSelectReturnRelays",
            body,
            references,
            relays
        );

        return relays;
    }
}
