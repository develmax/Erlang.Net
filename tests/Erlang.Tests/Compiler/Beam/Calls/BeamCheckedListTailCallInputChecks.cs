using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamCheckedListTailCallInputChecks
{
    internal const string TestName = "compiler/beam/keeps-a-checked-list-tail-in-its-outgoing-argument";
    private const int SyntheticAtomCount = 3;
    private const string LayoutError = "Expected both call arms to retain the checked list tail.";

    internal static void Run()
    {
        CheckBoundProvenance();
        CheckReusedListSlot();
        byte[] original = Body(compact: false, rejectedValue: A(2));
        byte[] expected = Body(compact: true, rejectedValue: A(2));
        byte[] observedOnFailure = Body(compact: false, rejectedValue: X(3));
        if (Rewrite(original) is not { } actual || !expected.SequenceEqual(actual)
            || Rewrite(expected) is not null || Rewrite(observedOnFailure) is not null)
        {
            throw new InvalidOperationException(LayoutError);
        }
    }

    private static void CheckReusedListSlot()
    {
        var compiler = new BeamModuleCompiler("beam_reused_list_call_slot");
        if (Apply(Shape()) is not { } transformed || !transformed.SequenceEqual(Shape(compacted: true))
            || Apply(transformed) is not null || Apply(Shape(readOldResult: true)) is not null
            || Apply(Shape(overwriteLaterArgument: true)) is not null
            || Apply(Shape(sharedResume: true)) is not null)
        {
            throw new InvalidOperationException(LayoutError);
        }

        byte[]? Apply(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "ReleaseReusedListCallSlot",
            code,
            5
        );
    }

    private static byte[] Shape(
        bool compacted = false,
        bool readOldResult = false,
        bool overwriteLaterArgument = false,
        bool sharedResume = false
    )
    {
        byte saved = compacted ? Y(1) : Y(2);
        byte[] oldHead = compacted ? [] : [Move, Y(1), X(0), InitYRegisters, ExtendedListOperand, U(1), Y(1)];
        byte[] newCons = compacted ? [] : [Move, X(0), Y(1)];
        byte[] oldCons = compacted ? [] : [Move, Y(1), X(2)];
        byte[] extra = overwriteLaterArgument ? [Move, A(1), X(2)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                3
            ),
            IsNonemptyList, F(sharedResume ? 4 : 3), X(2), Allocate, U(compacted ? 2 : 3), U(3),
            Move, X(1), saved, GetList, X(2), compacted ? X(0) : Y(1), Y(0), Jump, F(4),
            Label, U(3), Line, U(1), BadMatch, A(0), Label, U(4), .. oldHead,
            Line, U(2), LocalCall, U(1), F(2), TestHeap, U(2), U(1),
            PutList, X(0), Y(0), compacted ? X(2) : X(0), .. newCons,
            Line, U(3), BifOne, F(0), U(0), readOldResult ? X(0) : saved, X(0), .. extra,
            .. oldCons, Move, saved, X(1), TailLocalCallWithFrame, U(3), F(2), U(compacted ? 2 : 3),
            EndCode
        ];
    }

    private static void CheckBoundProvenance()
    {
        const string source = """
            -module(closed_arithmetic_bounds).
            -export([start/2]).
            start(Low, High) -> loop(Low - 1, High + 1, []).
            loop(Low, High, [_ | Rest]) -> loop(Low, High, Rest);
            loop(Low, High, []) -> {Low, High}.
            """;
        Check(source, true);
        Check(source + "\nunknown(Value) -> loop(Value, Value, []).", false);
        Check(source + "\nescape() -> fun loop/3.", false);

        static void Check(string text, bool expected)
        {
            ModuleDefinition module = new Parser(text).ParseModule();
            var compiler = new BeamModuleCompiler(module.Name, sourceModule: module);
            FunctionDefinition loop = module.DeclaredFunctions.Single(function => function.Name == "loop");
            foreach (int index in new[] { 0, 1 })
            {
                bool proven = (bool)Call(
                    compiler,
                    "HasClosedNumericEntryArgument",
                    loop,
                    index,
                    module.DeclaredFunctions
                );
                if (proven != expected)
                {
                    throw new InvalidOperationException(LayoutError);
                }
            }
        }
    }

    private static byte[] Body(bool compact, byte rejectedValue)
    {
        byte tail = compact ? X(3) : X(5);
        byte field = compact ? X(5) : X(6);
        byte[] copy = compact ? [] : [Move, X(5), X(3)];

        return
        [
            .. Start(
                1,
                2,
                1,
                4
            ), IsNonemptyList, F(4), X(3),
            GetList, X(3), X(4), tail, IsTuple, F(4), X(4), TestArity, F(4), X(4), U(2),
            GetTupleElement, X(4), U(0), field, IsLess, F(3), field, X(0),
            .. copy, TailLocalCall, U(4), F(2), Label, U(3), IsGreaterOrEqual, F(4), X(2), field,
            .. copy, Allocate, U(0), U(5), Line, U(1), LocalCall, U(5), F(2),
            Deallocate, U(0), Return, Label, U(4), Move, rejectedValue, X(0), Return, EndCode
        ];
    }

    private static byte[]? Rewrite(byte[] bytes)
    {
        ReadOnlyMemory<byte> code = bytes;
        object header = New(
            "BeamCodeHeader",
            0,
            MinimumHighestOpcode,
            5,
            1,
            code
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            SyntheticAtomCount,
            0,
            null,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "RewriteCheckedListTailCallInput",
            code,
            body,
            null
        );
    }
}
