using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamPostCallResultAliasChecks
{
    internal const string TestName = "compiler/beam/retains-the-sole-postcall-result-in-x0";
    private const string Module = "postcall_result_alias_fixture";
    private const string Pass = "EliminatePostCallResultAliases";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler(Module);
        CheckRewrite(
            compiler,
            Fixture(),
            Fixture(compact: true, roots: 1),
            BeamPostCallResultAliasDiagnostics.SoleResult
        );
        CheckRewrite(
            compiler,
            Fixture(roots: 1, typed: true),
            Fixture(compact: true, roots: 1, typed: true),
            BeamPostCallResultAliasDiagnostics.TypedRead
        );

        CheckDeclined(
            compiler,
            Fixture(beforeHeap: [Move, A(1), X(0)]),
            BeamPostCallResultAliasDiagnostics.Clobber
        );
        CheckDeclined(
            compiler,
            Fixture(beforeHeap: [InitYRegisters, ExtendedListOperand, U(1), Y(0)]),
            BeamPostCallResultAliasDiagnostics.Replacement
        );
        CheckDeclined(
            compiler,
            Fixture(beforeHeap: [ExternalCall, U(0), U(1)]),
            BeamPostCallResultAliasDiagnostics.SecondCall
        );
        CheckRewrite(
            compiler,
            Fixture(retainedOtherRoot: true),
            Fixture(compact: true, roots: 1, retainedOtherRoot: true),
            BeamPostCallResultAliasDiagnostics.OtherRoot
        );
        CheckRewrite(
            compiler,
            OwnedBifFixture(),
            OwnedBifFixture(compact: true),
            BeamPostCallResultAliasDiagnostics.OwnedBif
        );
        CheckDeclined(
            compiler,
            OwnedBifFixture(onlyReload: true),
            BeamPostCallResultAliasDiagnostics.PassThrough
        );
        CheckDeclined(
            compiler,
            OwnedBifFixture(clobber: true),
            BeamPostCallResultAliasDiagnostics.SecondCall
        );

        CheckRewrite(
            compiler,
            ListBranchFixture(),
            ListBranchFixture(compact: true),
            BeamPostCallResultAliasDiagnostics.ConstructedArguments
        );
        CheckRewrite(
            compiler,
            ListBranchFixture(secondItem: true),
            ListBranchFixture(compact: true, secondItem: true),
            BeamPostCallResultAliasDiagnostics.ConstructedArguments
        );
        CheckDeclined(
            compiler,
            ListBranchFixture(differentReason: true),
            BeamPostCallResultAliasDiagnostics.DifferentReason
        );

        CheckRewrite(
            compiler,
            GuardedContinuation(),
            GuardedContinuation(compact: true),
            BeamPostCallResultAliasDiagnostics.GuardedContinuation
        );
        CheckDeclined(compiler, GuardedContinuation(clobber: true), BeamPostCallResultAliasDiagnostics.Clobber);
        CheckDeclined(compiler, GuardedContinuation(bypass: true), BeamPostCallResultAliasDiagnostics.GuardedContinuation);

        CheckConstructorResult(compiler);

        _ = Call(compiler, "Intern", "record_tag");
        _ = Call(compiler, "Intern", "copy");
        _ = Call(compiler, "Intern", "field_value");
        CheckRewrite(
            compiler,
            RecordUpdateResult(),
            RecordUpdateResult(compact: true),
            BeamPostCallResultAliasDiagnostics.CheckedRecordField
        );
        CheckDeclined(
            compiler,
            RecordUpdateResult(separateArityCheck: true),
            BeamPostCallResultAliasDiagnostics.CheckedRecordField
        );
        CheckDeclined(
            compiler,
            RecordUpdateResult(reusesFieldAfterUpdate: true),
            BeamPostCallResultAliasDiagnostics.Clobber
        );
        CheckRewrite(
            compiler,
            ConstructorArgument(),
            ConstructorArgument(compact: true),
            BeamPostCallResultAliasDiagnostics.OutgoingConstructor
        );
        CheckRewrite(
            compiler,
            ConstructorArgument(closure: true),
            ConstructorArgument(compact: true, closure: true),
            BeamPostCallResultAliasDiagnostics.OutgoingConstructor
        );
        CheckDeclined(
            compiler,
            ConstructorArgument(clobber: true),
            BeamPostCallResultAliasDiagnostics.Clobber
        );
        CheckDeclined(
            compiler,
            ConstructorArgument(interveningCall: true),
            BeamPostCallResultAliasDiagnostics.SecondCall
        );
        CheckDeclined(
            compiler,
            ConstructorArgument(ignoresArgument: true),
            BeamPostCallResultAliasDiagnostics.OutgoingConstructor
        );

        return Task.CompletedTask;
    }

    private static byte[] ConstructorArgument(
        bool compact = false,
        bool closure = false,
        bool clobber = false,
        bool interveningCall = false,
        bool ignoresArgument = false
    )
    {
        int width = compact ? 1 : 2;
        byte destination = X(clobber ? 0 : 1);
        byte[] initialized = compact ? [] : [InitYRegisters, ExtendedListOperand, U(1), Y(1)];
        byte[] saved = compact ? [] : [Move, X(0), Y(1)];
        byte[] reload = compact ? [] : [Move, Y(1), X(0)];
        byte[] call = interveningCall ? [ExternalCall, U(1), U(1)] : [];
        byte[] heap = closure
            ? [
                (byte)(U(AllocationList) | ExtendedTag), U(AllocationKinds),
                U(AllocationWords), U(0), U(AllocationFloats), U(0), U(AllocationFuns), U(1)
            ]
            : [U(ConsHeapWords)];
        byte[] construction = closure
            ? [MakeFun, U(0), destination, ExtendedListOperand, U(1), Y(0)]
            : [PutList, Y(0), A(0), destination];

        return
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            Allocate, U(width), U(1), .. initialized, Move, X(0), Y(0),
            Line, U(1), ExternalCall, U(0), U(0), .. saved,
            TestHeap, .. heap, U(compact ? 1 : 0), .. construction, .. call, .. reload,
            Line, U(2), TailExternalCallWithFrame, U(ignoresArgument ? 0 : 2), U(2), U(width),
            EndCode
        ];
    }

    private static byte[] RecordUpdateResult(
        bool compact = false,
        bool separateArityCheck = false,
        bool reusesFieldAfterUpdate = false
    )
    {
        int width = compact ? 1 : 2;
        byte[] initial = compact ? [] : [InitYRegisters, ExtendedListOperand, U(1), Y(1)];
        byte[] spill = compact ? [] : [Move, X(0), Y(1)];
        byte[] check = separateArityCheck
            ? [IsTuple, F(3), Y(0), TestArity, F(3), Y(0), U(3)]
            : [IsTaggedTuple, F(3), Y(0), U(3), A(2)];
        byte[] laterUse = reusesFieldAfterUpdate ? [Move, Y(1), X(0)] : [];
        byte field = compact ? X(0) : Y(1);

        return
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            Allocate, U(width), U(1), .. initial, Move, X(0), Y(0),
            Line, U(1), ExternalCall, U(0), U(0), .. spill,
            .. check, TestHeap, U(4), U(compact ? 1 : 0),
            UpdateRecord, A(3), U(3), Y(0), X(0), ExtendedListOperand, U(4),
            U(2), field, U(3), A(4),
            .. laterUse, Deallocate, U(width), Return,
            Label, U(3), BadRecord, Y(0), EndCode
        ];
    }

    private static void CheckConstructorResult(BeamModuleCompiler compiler)
    {
        byte[]? Rewrite(byte[] input) => (byte[]?)Call(
            compiler,
            "RetainPostCallConstructorInputs",
            (ReadOnlyMemory<byte>)input,
            4
        );

        if (Rewrite(ConstructorResult()) is not { } compact
            || !compact.SequenceEqual(ConstructorResult(compact: true))
            || Rewrite(ConstructorResult(reusesScratch: true)) is not { } reused
            || !reused.SequenceEqual(ConstructorResult(compact: true, reusesScratch: true))
            || Rewrite(ConstructorResult(occupiedScratch: true, reusesScratch: true)) is not null
            || Rewrite(ConstructorResult(occupiedScratch: true)) is not null
            || Rewrite(ConstructorResult(crossesCollector: true)) is not null
            || Rewrite(ConstructorResult(reloadsOldScratch: true)) is not null)
        {
            throw new InvalidOperationException(BeamPostCallResultAliasDiagnostics.ConstructorSibling);
        }
    }

    private static byte[] ConstructorResult(
        bool compact = false,
        bool occupiedScratch = false,
        bool crossesCollector = false,
        bool reloadsOldScratch = false,
        bool reusesScratch = false
    )
    {
        int width = compact ? 1 : 2;
        byte scratch = X(compact ? 1 : 0);
        byte result = compact ? X(0) : Y(1);
        byte destination = X(reusesScratch ? 1 : 2);
        byte otherArgument = X(reusesScratch ? 2 : 1);
        byte[] initialized = compact ? [] : [InitYRegisters, ExtendedListOperand, U(1), Y(1)];
        byte[] saved = compact ? [] : [Move, X(0), Y(1)];
        byte[] clobber = occupiedScratch ? [Move, Y(0), X(1)] : [];
        byte[] collector = crossesCollector ? [TestHeap, U(ConsHeapWords), U(1)] : [];
        byte[] lateLoad = reloadsOldScratch ? [Move, X(0), X(1)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            Allocate, U(width), U(1), .. initialized, Move, X(0), Y(0),
            ExternalCall, U(0), U(0), .. saved, TestHeap, U(2 * ConsHeapWords), U(compact ? 1 : 0),
            .. clobber, PutList, Y(0), A(0), scratch, .. collector,
            PutList, result, scratch, destination, .. lateLoad, Move, A(1), otherArgument, Move, Y(0), X(0),
            TailExternalCallWithFrame, U(3), U(0), U(width), EndCode
        ];
    }

    private static void CheckRewrite(
        BeamModuleCompiler compiler,
        byte[] input,
        byte[] expected,
        string reason
    )
    {
        if (Rewrite(compiler, input) is not { } actual || !actual.SequenceEqual(expected))
        {
            throw new InvalidOperationException(reason);
        }
    }

    private static void CheckDeclined(BeamModuleCompiler compiler, byte[] input, string reason)
    {
        if (Rewrite(compiler, input) is not null)
        {
            throw new InvalidOperationException(reason);
        }
    }

    private static byte[]? Rewrite(BeamModuleCompiler compiler, ReadOnlyMemory<byte> code) =>
        (byte[]?)Call(
            compiler,
            Pass,
            code,
            4
        );

    private static byte[] GuardedContinuation(bool compact = false, bool clobber = false, bool bypass = false)
    {
        int width = compact ? 1 : 2;
        byte[] initial = compact ? [] : [InitYRegisters, ExtendedListOperand, U(1), Y(1)];
        byte[] beforeCall = bypass ? [IsNil, F(3), X(0)] : [];
        byte[] overwrite = clobber ? [Move, A(1), X(0)] : [];
        byte[] spill = compact ? [] : [Move, X(0), Y(1)];

        return
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            Allocate, U(width), U(1), .. initial, Move, X(0), Y(0), .. beforeCall,
            ExternalCall, U(1), U(0), ExactEqualTest, F(3), X(0), A(1),
            Move, Y(0), X(0), Deallocate, U(width), Return,
            Label, U(3), .. overwrite, .. spill, TestHeap, U(ConsHeapWords), U(compact ? 1 : 0),
            PutList, compact ? X(0) : Y(1), Y(0), X(0), Deallocate, U(width), Return, EndCode
        ];
    }

    private static byte[] ListBranchFixture(
        bool compact = false,
        bool differentReason = false,
        bool secondItem = false
    )
    {
        int width = compact ? 1 : 2;
        byte result = compact ? X(0) : Y(1);
        byte list = X(compact ? 1 : 0);
        byte reason = differentReason ? A(0) : A(1);
        byte[] initialized = compact ? [] : [InitYRegisters, ExtendedListOperand, U(1), Y(1)];
        byte[] savedResult = compact ? [] : [Move, X(0), Y(1)];
        byte[] continuation = secondItem ? [PutList, Y(0), list, list] : [];
        byte[] storeList = compact ? [] : [Move, X(0), Y(0)];
        byte[] setupError = compact ? [] : [Move, Y(0), X(1), Move, reason, X(0)];
        byte[] reload = compact ? [] : [Move, Y(1), X(0)];

        return
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            Allocate, U(width), U(1), .. initialized,
            Move, X(0), Y(0),
            ExternalCall, U(0), U(0), .. savedResult,
            TestHeap, U(ConsHeapWords * (secondItem ? 2 : 1)), U(compact ? 1 : 0),
            PutList, Y(0), A(0), list, .. continuation, .. storeList,
            ExactEqualTest, F(3), result, A(1), .. setupError,
            Line, U(1), TailExternalCallWithFrame, U(2), U(0), U(width),
            Label, U(3), .. reload, Deallocate, U(width), Return, EndCode
        ];
    }

    private static byte[] OwnedBifFixture(bool compact = false, bool onlyReload = false, bool clobber = false)
    {
        int width = compact ? 1 : 2;
        byte[] init = compact ? [] : [InitYRegisters, ExtendedListOperand, U(1), Y(1)];
        byte[] spill = compact ? [] : [Move, X(0), Y(1)];
        byte[] between = clobber ? [ExternalCall, U(0), U(1)] : [];
        byte[] consume = onlyReload
            ? [Move, Y(1), X(0)]
            : [BifTwo, F(0), U(1), compact ? X(0) : Y(1), Y(0), X(0)];

        return
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            Allocate, U(width), U(1), .. init, Move, X(0), Y(0),
            ExternalCall, U(0), U(0), ExternalCall, U(1), U(1), .. spill, .. between,
            .. consume, IsNil, F(3), X(0), Deallocate, U(width), Return,
            Label, U(3), Move, Y(0), X(0), Deallocate, U(width), Return, EndCode
        ];
    }

    private static byte[] Fixture(
        bool compact = false,
        int roots = 0,
        bool typed = false,
        byte[]? beforeHeap = null,
        bool retainedOtherRoot = false
    )
    {
        int frameWidth = (retainedOtherRoot ? 1 : 0) + (compact ? 0 : 1);
        byte saved = compact ? X(0) : Y(0);
        byte[] rootInit = compact ? [] : [InitYRegisters, ExtendedListOperand, U(1), Y(0)];
        byte other = compact ? Y(0) : Y(1);
        byte[] otherSpill = retainedOtherRoot ? [Move, X(0), other] : [];
        byte[] resultSpill = compact ? [] : [Move, X(0), Y(0)];
        byte[] reload = compact ? [] : [Move, Y(0), X(0)];
        byte[] guardValue = typed ? [ExtendedTypedRegisterOperand, saved, U(1)] : [saved];
        byte lastElement = retainedOtherRoot ? other : A(1);

        return
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            Allocate, U(frameWidth), U(1),
            .. rootInit,
            .. otherSpill,
            Line, U(1), ExternalCall, U(0), U(0),
            .. resultSpill,
            IsNil, F(3), .. guardValue,
            .. reload,
            Deallocate, U(frameWidth), Return,
            Label, U(3),
            .. (beforeHeap ?? []),
            TestHeap, U(3), U(roots),
            PutTuple, X(0), ExtendedListOperand, U(2), saved, lastElement,
            Deallocate, U(frameWidth), Return, EndCode
        ];
    }
}
