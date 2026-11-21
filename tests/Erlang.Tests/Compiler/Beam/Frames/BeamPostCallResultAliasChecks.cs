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

        return Task.CompletedTask;
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
