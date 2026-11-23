using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamPreCallFrameAliasChecks
{
    internal const string TestName = "compiler/beam/removes-precall-frame-aliases-without-crossing-gc";
    internal const string CollectorTestName = "compiler/beam/retains-precall-aliases-at-one-collector";
    private const string Module = "beam_precall_frame_alias_fixture";
    private const string Pass = "EliminatePreCallFrameAliases";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler(Module);
        CheckCompacts(compiler, typedGuard: false);
        CheckCompacts(compiler, typedGuard: true);
        BeamPreCallClosureDestinationChecks.Run(compiler);
        if (Rewrite(compiler, Fixture(afterCall: [Move, Y(1), X(1), ExternalCall, U(2), U(0)])) is not { } multiple
            || !multiple.SequenceEqual(Fixture(
                compact: true,
                afterCall: [Move, Y(0), X(1), ExternalCall, U(2), U(0)])))
        {
            throw new InvalidOperationException(BeamPreCallFrameAliasDiagnostics.FrameAndGuard);
        }

        CheckUnchanged(
            compiler,
            Fixture(afterCall: [Move, Y(1), X(1), ExternalCall, U(2), U(0), Move, Y(0), X(1)]),
            BeamPreCallFrameAliasDiagnostics.AcrossCall
        );

        CheckUnchanged(
            compiler,
            Fixture(afterCall: [Move, Y(0), X(0)]),
            BeamPreCallFrameAliasDiagnostics.AcrossCall
        );
        CheckUnchanged(
            compiler,
            Fixture(interval: [TestHeap, U(2), U(0)]),
            BeamPreCallFrameAliasDiagnostics.Heap
        );
        CheckUnchanged(
            compiler,
            Fixture(interval: [Move, A(1), X(0)]),
            BeamPreCallFrameAliasDiagnostics.Clobber
        );
        CheckUnchanged(
            compiler,
            Fixture(interval: [InitYRegisters, ExtendedListOperand, U(1), Y(0)]),
            BeamPreCallFrameAliasDiagnostics.Reinitialized
        );
        CheckUnchanged(
            compiler,
            Fixture(sharedEntry: true),
            BeamPreCallFrameAliasDiagnostics.InteriorEntry
        );
        CheckUnchanged(
            compiler,
            Fixture(interval: [DebugLine, A(1), U(1), U(1), U(1)]),
            BeamPreCallFrameAliasDiagnostics.Debugger
        );

        return Task.CompletedTask;
    }

    internal static Task RunCollectors()
    {
        var compiler = new BeamModuleCompiler(Module);
        CheckCollectorCompacts(
            compiler,
            Fixture(interval: [TestHeap, U(2), U(0)]),
            Fixture(compact: true, interval: [TestHeap, U(2), U(1)])
        );
        CheckCollectorCompacts(
            compiler,
            Fixture(interval: [GcBifTwo, F(3), U(0), U(0), Y(1), A(1), X(1)]),
            Fixture(compact: true, interval: [GcBifTwo, F(3), U(1), U(0), Y(0), A(1), X(1)])
        );
        byte[] allocation =
        [
            TestHeap, (byte)(U(AllocationList) | ExtendedTag), U(AllocationKinds),
            U(AllocationWords), U(2), U(AllocationFloats), U(0), U(AllocationFuns), U(1)
        ];
        CheckCollectorCompacts(
            compiler,
            Fixture(interval: [.. allocation, U(0)]),
            Fixture(compact: true, interval: [.. allocation, U(1)])
        );
        CheckCollectorCompacts(compiler, DenseCollectorFixture(false), DenseCollectorFixture(true));
        CheckUnchanged(
            compiler,
            Fixture(interval: [TestHeap, U(2), U(0)], aliasRegister: 2),
            BeamPreCallFrameAliasDiagnostics.RootGap,
            allowCollectors: true
        );
        CheckUnchanged(
            compiler,
            Fixture(interval: [TestHeap, U(2), U(0), TestHeap, U(2), U(0)]),
            BeamPreCallFrameAliasDiagnostics.SecondCollector,
            allowCollectors: true
        );
        CheckUnchanged(
            compiler,
            Fixture(interval: [GcBifTwo, F(3), U(0), U(0), Y(1), A(1), X(0)]),
            BeamPreCallFrameAliasDiagnostics.Clobber,
            allowCollectors: true
        );

        return Task.CompletedTask;
    }

    private static void CheckCollectorCompacts(BeamModuleCompiler compiler, byte[] original, byte[] expected)
    {
        if (Rewrite(compiler, original, allowCollectors: true) is not { } rewritten
            || !rewritten.SequenceEqual(expected))
        {
            throw new InvalidOperationException(BeamPreCallFrameAliasDiagnostics.CollectorRoots);
        }
    }

    private static byte[] DenseCollectorFixture(bool compact)
    {
        int width = compact ? 1 : 3;
        byte retained = Y(compact ? 0 : 2);
        byte[] spills = compact ? [] : [Move, X(0), Y(0), Move, X(1), Y(1)];
        byte[] tupleArguments = compact ? [X(0), X(1)] : [Y(0), Y(1)];

        return
        [
            .. Start(
                1,
                2,
                1,
                3
            ),
            Allocate, U(width), U(3),
            Move, X(2), retained,
            .. spills,
            TestHeap, U(3), U(compact ? 2 : 0),
            PutTuple, X(0), ExtendedListOperand, U(2), .. tupleArguments,
            ExternalCall, U(1), U(0),
            Move, retained, X(0),
            Deallocate, U(width), Return,
            EndCode
        ];
    }

    private static void CheckCompacts(BeamModuleCompiler compiler, bool typedGuard)
    {
        var original = Fixture(typedGuard: typedGuard);
        var expected = Fixture(compact: true, typedGuard: typedGuard);
        if (Rewrite(compiler, original) is not { } rewritten || !rewritten.SequenceEqual(expected))
        {
            throw new InvalidOperationException(BeamPreCallFrameAliasDiagnostics.FrameAndGuard);
        }
    }

    private static void CheckUnchanged(
        BeamModuleCompiler compiler,
        byte[] original,
        string diagnostic,
        bool allowCollectors = false
    )
    {
        if (Rewrite(compiler, original, allowCollectors) is not null)
        {
            throw new InvalidOperationException(diagnostic);
        }
    }

    private static byte[]? Rewrite(BeamModuleCompiler compiler, ReadOnlyMemory<byte> code, bool allowCollectors = false) =>
        (byte[]?)Call(
            compiler,
            Pass,
            code,
            5,
            allowCollectors
        );

    private static byte[] Fixture(
        bool compact = false,
        bool typedGuard = false,
        byte[]? interval = null,
        byte[]? afterCall = null,
        bool sharedEntry = false,
        int aliasRegister = 0
    )
    {
        int width = compact ? 1 : 2;
        byte retained = Y(compact ? 0 : 1);
        byte test = compact ? X(aliasRegister) : Y(0);
        byte[] guardInput = typedGuard ? [ExtendedTypedRegisterOperand, test, U(1)] : [test];
        byte[] spill = compact ? [] : [Move, X(aliasRegister), Y(0)];
        byte[] reload = compact && aliasRegister == 0 ? [] : [Move, test, X(0)];
        int arity = Math.Max(2, aliasRegister + 1);
        byte[] bypass = sharedEntry
            ? [InitYRegisters, ExtendedListOperand, U(1), Y(0), IsAtom, F(4), X(1)]
            : [];
        byte[] sharedLabel = sharedEntry ? [Label, U(4)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                arity
            ),
            Allocate, U(width), U(arity),
            Move, X(1), retained,
            .. bypass,
            .. spill,
            .. sharedLabel,
            .. (interval ?? []),
            IsAtom, F(3), .. guardInput,
            .. reload,
            Line, U(1),
            ExternalCall, U(1), U(0),
            .. (afterCall ?? []),
            Move, retained, X(0),
            Deallocate, U(width), Return,
            Label, U(3), Move, retained, X(0),
            Deallocate, U(width), Return,
            EndCode
        ];
    }
}
