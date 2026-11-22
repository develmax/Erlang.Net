using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamListCallRetryLayoutChecks
{
    internal const string TestName = "compiler/beam/places-owned-list-retries-after-checked-calls";
    private const string LayoutError = "Expected independent frame saves and the owned list retry in SSA order.";

    internal static void Run()
    {
        byte[] original = Body(reordered: false, retrySecond: X(2));
        byte[] expected = Body(reordered: true, retrySecond: X(2));
        byte[] distinctRecursionArgument = Body(reordered: false, retrySecond: X(1));
        if (Rewrite(original) is not { } actual || !expected.SequenceEqual(actual)
            || Rewrite(expected) is not null || Rewrite(distinctRecursionArgument) is not null)
        {
            throw new InvalidOperationException(LayoutError);
        }

        byte[] shared = ClosedListDecision(split: false);
        byte[] separate = ClosedListDecision(split: true);
        if (RewriteFrameSplit(shared) is not { } rewritten || !rewritten.AsSpan().SequenceEqual(separate)
            || RewriteFrameSplit(rewritten) is not null
            || RewriteFrameSplit(ClosedListDecision(split: false, swappedRetry: true)) is not null
            || RewriteFrameSplit(ClosedListDecision(split: false, extraFallback: true)) is not null)
        {
            throw new InvalidOperationException("Only disjoint cons decisions may allocate their own call roots.");
        }

        byte[] cursor = ClosedListDecision(split: false, hasPrefix: true);
        byte[] stillRead = ClosedListDecision(split: false, hasPrefix: true, fallbackReadsInput: true);
        byte[] overwritten = ClosedListDecision(split: false, hasPrefix: true, overwritesCapture: true);
        if (RewriteFrameSplit(cursor) is not { } scheduled
            || !scheduled.AsSpan().SequenceEqual(ClosedListDecision(split: true, hasPrefix: true))
            || RewriteFrameSplit(stillRead) is not { } preserved
            || !preserved.AsSpan().SequenceEqual(ClosedListDecision(
                split: true,
                hasPrefix: true,
                fallbackReadsInput: true
            ))
            || RewriteFrameSplit(overwritten) is not { } retainedOrder
            || !retainedOrder.AsSpan().SequenceEqual(ClosedListDecision(
                split: true,
                hasPrefix: true,
                overwritesCapture: true
            )))
        {
            throw new InvalidOperationException("The checked cons cursor must preserve X0 on observable failure edges.");
        }

        byte[] update = ClosedListDecision(split: false, hasPrefix: true, updatesListField: true);
        if (RewriteFrameSplit(update) is not { } typed
            || !typed.AsSpan().SequenceEqual(ClosedListDecision(
                split: true,
                hasPrefix: true,
                updatesListField: true,
                typedListField: true
            ))
            || RewriteFrameSplit(update, reverseImport: false) is not { } unknown
            || !unknown.AsSpan().SequenceEqual(ClosedListDecision(split: true, hasPrefix: true, updatesListField: true)))
        {
            throw new InvalidOperationException("An owned record update must type only the proven list producer.");
        }
    }

    private static byte[] ClosedListDecision(
        bool split,
        bool swappedRetry = false,
        bool extraFallback = false,
        bool hasPrefix = false,
        bool fallbackReadsInput = false,
        bool overwritesCapture = false,
        bool updatesListField = false,
        bool typedListField = false
    )
    {
        bool scheduled = split && hasPrefix && !fallbackReadsInput;
        bool ordered = scheduled && !overwritesCapture;
        byte tupleRecord = Y(ordered ? 2 : 1);
        byte tupleField = Y(ordered ? 1 : 2);
        byte atomRecord = Y(ordered ? 1 : 0);
        byte atomField = Y(ordered ? 0 : 1);
        byte[] overwriteTuple = overwritesCapture ? [Move, X(0), tupleField] : [];
        byte[] overwriteAtom = overwritesCapture ? [Move, X(0), atomField] : [];
        byte[] overwriteShared = overwritesCapture ? [Move, X(0), Y(5)] : [];
        byte fieldSource = X(hasPrefix ? 3 : 1);
        byte cursor = X(scheduled ? 0 : 2);
        byte callerArgument = X(scheduled ? 2 : 0);
        byte[] prefix = hasPrefix
            ?
            [
                GetTupleElement, X(1), U(0), X(scheduled ? 0 : 3),
                IsNonemptyList, F(5), X(scheduled ? 0 : 3),
                GetList, X(scheduled ? 0 : 3), X(3), cursor,
                IsTuple, F(5), X(3), TestArity, F(5), X(3), U(2)
            ]
            : [];
        byte[] failure = hasPrefix
            ? [Label, U(5), Move, X(fallbackReadsInput ? 0 : 1), X(0), Return]
            : [];
        byte[] copyArgument = scheduled ? [Move, callerArgument, X(0)] : [];
        byte[] common =
        [
            .. Start(
                1,
                2,
                1,
                3
            ),
            IsTuple, F(1), X(1), TestArity, F(1), X(1), U(2)
        ];
        byte[] call = [Line, U(1), ExternalCall, U(1), U(0)];
        int extraHeap = updatesListField ? 3 : 0;
        byte[] updateValue = typedListField
            ? [ExtendedTypedRegisterOperand, X(0), U(2)]
            : [X(0)];

        byte[] ReplaceField(byte carrier) => updatesListField
            ? [UpdateRecord, A(4), U(2), carrier, X(0), ExtendedListOperand, U(2), U(2), .. updateValue]
            : [];
        if (split)
        {
            return
            [
                .. common, .. prefix,
                IsNonemptyList, F(4), cursor,
                GetTupleElement, fieldSource, U(0), callerArgument,
                GetTupleElement, fieldSource, U(1), X(3),
                GetList, cursor, X(4), cursor,
                IsTuple, F(3), X(4),
                IsTaggedTuple, F(4), ExtendedTypedRegisterOperand, X(4), U(1), U(2), A(3),
                ExactEqualTest, F(4), cursor, ExtendedLiteralOperand, U(0),
                Allocate, U(3), U(5), Move, X(4), Y(0),
                Move, X(ordered ? 3 : 1), Y(1), Move, X(ordered ? 1 : 3), Y(2),
                .. copyArgument, .. call, TestHeap, U(5 + extraHeap), U(1), .. overwriteTuple, .. ReplaceField(tupleRecord),
                PutTuple, X(0), ExtendedListOperand, U(4), Y(0), tupleField, tupleRecord, X(0),
                Deallocate, U(3), Return,
                Label, U(3), ExactEqualTest, F(4), X(4), A(2), IsNil, F(4), cursor,
                Allocate, U(2), U(4), Move, X(ordered ? 3 : 1), Y(0), Move, X(ordered ? 1 : 3), Y(1),
                .. copyArgument, .. call, TestHeap, U(4 + extraHeap), U(1), .. overwriteAtom, .. ReplaceField(atomRecord),
                PutTuple, X(0), ExtendedListOperand, U(3), atomField, atomRecord, X(0),
                Deallocate, U(2), Return,
                Label, U(4), Move, X(1), X(0), Return, .. failure, EndCode
            ];
        }

        byte[] checkedArgument = extraFallback ? [IsNil, F(4), Y(3)] : [];
        byte[] argument = [Move, Y(3), X(0), .. call];

        return
        [
            .. common, .. prefix,
            Allocate, U(6), U(hasPrefix ? 4 : 3), InitYRegisters, ExtendedListOperand, U(2), Y(0), Y(1),
            Move, X(1), Y(4), GetTupleElement, fieldSource, U(0), Y(3),
            GetTupleElement, fieldSource, U(1), Y(5), Move, X(2), Y(2),
            IsNonemptyList, F(4), Y(2), GetList, Y(2), Y(1), Y(0),
            ExactEqualTest, F(3), Y(1), A(2), IsNil, F(3), Y(0), .. checkedArgument,
            .. argument, TestHeap, U(4 + extraHeap), U(1), .. overwriteShared, .. ReplaceField(Y(4)),
            PutTuple, X(0), ExtendedListOperand, U(3), Y(5), Y(4), X(0), Deallocate, U(6), Return,
            Label, U(3), GetList, Y(2), Y(swappedRetry ? 0 : 1), Y(swappedRetry ? 1 : 0),
            IsTaggedTuple, F(4), Y(1), U(2), A(3),
            ExactEqualTest, F(4), Y(0), ExtendedLiteralOperand, U(0),
            .. argument, TestHeap, U(5 + extraHeap), U(1), .. overwriteShared, .. ReplaceField(Y(4)),
            PutTuple, X(0), ExtendedListOperand, U(4), Y(1), Y(5), Y(4), X(0), Deallocate, U(6), Return,
            Label, U(4), Move, Y(4), X(0), Deallocate, U(6), Return, .. failure, EndCode
        ];
    }

    private static byte[]? RewriteFrameSplit(byte[] bytes, bool reverseImport = true)
    {
        var compiler = new BeamModuleCompiler("beam_owned_cons_decision_probe");
        _ = Call(compiler, "Intern", "first");
        _ = Call(compiler, "Intern", "second");
        _ = Call(compiler, "Intern", "copy");
        _ = Call(
            compiler,
            "Import",
            reverseImport ? "lists" : "erlang",
            reverseImport ? "reverse" : "list_to_atom",
            1
        );
        using var operands = new MemoryStream();
        _ = Call(
            compiler,
            "WriteLiteralPoolOperand",
            operands,
            Term.List(Term.A("first"))
        );

        return (byte[]?)Call(
            compiler,
            "SplitOwnedConsDecisionFrames",
            new ReadOnlyMemory<byte>(bytes),
            6
        );
    }

    private static byte[] Body(bool reordered, byte retrySecond)
    {
        byte[] saves = reordered
            ? [Move, X(4), Y(0), Move, X(2), Y(1), Move, X(1), Y(2)]
            : [Move, X(1), Y(0), Move, X(2), Y(1), Move, X(4), Y(2)];
        byte[] empty = [Label, U(3), IsNil, F(1), X(0), Move, A(2), X(0), Return];
        byte[] retry = reordered
            ? [Label, U(4), Move, Y(1), X(2), Move, Y(2), X(1), Move, Y(0), X(0)]
            : [Label, U(4), Move, Y(0), X(1), Move, Y(1), retrySecond, Move, Y(2), X(0)];
        retry = [.. retry, TailLocalCallWithFrame, U(3), F(2), U(3)];

        return
        [
            .. Start(
                1,
                2,
                1,
                3
            ), IsNonemptyList, F(3), X(0),
            GetList, X(0), X(3), X(4), IsTuple, F(1), X(3),
            TestArity, F(1), X(3), U(2), Allocate, U(3), U(5), .. saves,
            GetTupleElement, X(3), U(0), X(0), Line, U(1), LocalCall, U(3), F(2),
            IsTuple, F(4), X(0), Deallocate, U(3), Return,
            .. (reordered ? retry : empty), .. (reordered ? empty : retry), EndCode
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
            2,
            0,
            null,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "RewriteListRetryAfterCheckedCall",
            code,
            body
        );
    }
}
