using System.Reflection;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamTerminalConstructionScratchChecks
{
    internal const string TestName = "compiler/beam/forwards-reused-terminal-construction-scratch";
    private const string WrongLayout = "Each constructor must receive its original value without keeping a dead stack copy.";

    internal static Task Run()
    {
        byte[] original = Fixture();
        byte[] expected = Fixture(rewritten: true);
        if (Rewrite(original) is not { } result || !result.AsSpan().SequenceEqual(expected)
            || Rewrite(expected) is not null
            || Rewrite(Fixture(clobbersResult: true)) is not null
            || Rewrite(Fixture(readsSavedLater: true)) is not null
            || Rewrite(Fixture(joinsAtProjection: true)) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        byte[] frameless = FramelessFixture();
        byte[] framelessExpected = FramelessFixture(rewritten: true);
        if (Rewrite(frameless) is not { } forwarded || !forwarded.AsSpan().SequenceEqual(framelessExpected)
            || Rewrite(framelessExpected) is not null
            || Rewrite(FramelessFixture(readsScratch: true)) is not null
            || Rewrite(FramelessFixture(collects: true)) is not null
            || Rewrite(FramelessFixture(aliasesTail: true)) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        byte[] retired = RetiredRecord();
        if (RewriteRetired(retired) is not { } direct || !direct.AsSpan().SequenceEqual(RetiredRecord(rewritten: true))
            || RewriteRetired(direct) is not null
            || RewriteRetired(RetiredRecord(readsLater: true)) is not null
            || RewriteRetired(RetiredRecord(), reverse: false) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;
    }

    private static byte[] RetiredRecord(bool rewritten = false, bool readsLater = false)
    {
        byte[] save = rewritten ? [] : [Move, X(0), Y(0)];
        byte[] observe = readsLater ? [Move, Y(0), X(0)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            IsTaggedTuple, F(1), X(1), U(2), A(2),
            Allocate, U(1), U(2), Move, X(1), Y(0), ExternalCall, U(1), U(0),
            TestHeap, U(5), U(1),
            UpdateRecord, A(2), U(2), Y(0), X(0), ExtendedListOperand, U(2), U(2),
            ExtendedTypedRegisterOperand, X(0), U(1),
            .. save, PutList, rewritten ? X(0) : Y(0), A(0), X(0),
            .. observe, Deallocate, U(1), Return, EndCode
        ];
    }

    private static byte[]? RewriteRetired(byte[] bytes, bool reverse = true)
    {
        var compiler = new BeamModuleCompiler("beam_retired_record_cons_probe");
        _ = Call(compiler, "Intern", "reuse");
        _ = Call(
            compiler,
            "Import",
            reverse ? "lists" : "erlang",
            reverse ? "reverse" : "list_to_atom",
            1
        );
        int listType = (int)typeof(BeamModuleCompiler)
            .GetField("ProperListTypeBits", BindingFlags.Static | BindingFlags.NonPublic)!.GetRawConstantValue()!;
        _ = Call(
            compiler,
            "GetOperandTypeIndex",
            (listType, (long?)null, (long?)null, (int?)null)
        );

        return (byte[]?)Call(
            compiler,
            "ForwardRetiredRecordConsCopy",
            new ReadOnlyMemory<byte>(bytes),
            4
        );
    }

    private static byte[] FramelessFixture(
        bool rewritten = false,
        bool readsScratch = false,
        bool collects = false,
        bool aliasesTail = false
    )
    {
        byte[] save = rewritten ? [] : [Move, X(0), X(4)];
        byte result = rewritten ? X(0) : X(4);
        byte[] collector = collects ? [TestHeap, U(8), U(3)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                3
            ),
            TestHeap, U(14), U(3),
            PutTuple, X(0), ExtendedListOperand, U(1), X(0),
            .. save,
            PutList, result, aliasesTail ? X(0) : X(1), X(0),
            .. collector,
            UpdateRecord, A(2), U(3), X(2), X(0), ExtendedListOperand, U(2), U(3), X(0),
            .. save,
            PutList, result, X(1), X(0),
            UpdateRecord, A(2), U(3), X(2), X(0), ExtendedListOperand, U(2), U(3), readsScratch ? X(4) : X(0),
            Return, EndCode
        ];
    }

    private static byte[] Fixture(
        bool rewritten = false,
        bool clobbersResult = false,
        bool readsSavedLater = false,
        bool joinsAtProjection = false
    )
    {
        byte projected = clobbersResult ? X(0) : X(1);
        byte[] store = rewritten ? [] : [Move, X(0), Y(0)];
        byte[] join = joinsAtProjection ? [Label, U(3)] : [];
        byte[] observe = readsSavedLater ? [Move, Y(0), X(0)] : [];
        byte construction = rewritten ? X(0) : Y(0);

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            Allocate, U(2), U(2), Move, X(1), Y(1),
            InitYRegisters, ExtendedListOperand, U(1), Y(0),
            TestHeap, U(9), U(1),
            PutTuple, X(0), ExtendedListOperand, U(1), X(0),
            .. store, .. join,
            GetTupleElement, Y(1), U(1), projected,
            PutList, construction, projected, X(0),
            .. store,
            UpdateRecord, A(2), U(3), Y(1), X(0), ExtendedListOperand, U(2),
            U(3), ExtendedTypedRegisterOperand, construction, U(0),
            .. observe, Deallocate, U(2), Return, EndCode
        ];
    }

    private static byte[]? Rewrite(byte[] bytes)
    {
        ReadOnlyMemory<byte> code = bytes;
        object header = New(
            "BeamCodeHeader",
            0,
            (int)UpdateRecord,
            4,
            1,
            code
        );
        var stack = new List<(int Start, int End, int Slot)>();
        Action<int, int, int> recordStack = (start, end, slot) => stack.Add((start, end, slot));
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            3,
            0,
            null,
            recordStack
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "RewriteTerminalConstructionScratch",
            code,
            body,
            stack
        );
    }
}
