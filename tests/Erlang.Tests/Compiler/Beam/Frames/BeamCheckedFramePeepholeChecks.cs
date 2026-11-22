using System.Reflection;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCheckedFramePeepholeChecks
{
    internal const string FallthroughTestName = "compiler/beam/shares-physical-fallthrough-return";
    internal const string HeapTestName = "compiler/beam/folds-heap-through-checked-entry-projection";

    private const string HeapPass = "CombineStackAndHeapAllocation";
    private const int DictionaryImport = 2;
    private const int OtherImport = 3;
    private const int AbsentImport = -1;

    internal static Task Fallthrough()
    {
        Expect(
            "ShareFallthroughTupleReturns",
            ReturnCode(false),
            ReturnCode(true),
            BeamCheckedFramePeepholeDiagnostics.SharedReturn
        );
        Expect(
            "ShareFallthroughTupleReturns",
            ReturnCode(false, lateFrame: 3),
            null,
            BeamCheckedFramePeepholeDiagnostics.DifferentFrame
        );
        Expect(
            "ShareFallthroughTupleReturns",
            ReturnCode(false, lateResult: 0),
            null,
            BeamCheckedFramePeepholeDiagnostics.DifferentSlot
        );
        Expect(
            "ShareFallthroughTupleReturns",
            ReturnCode(false, lateLine: true),
            null,
            BeamCheckedFramePeepholeDiagnostics.DifferentLine
        );

        return Task.CompletedTask;
    }

    internal static Task Heap()
    {
        Expect(
            HeapPass,
            HeapCode(false),
            HeapCode(true),
            BeamCheckedFramePeepholeDiagnostics.CheckedProjection,
            AbsentImport
        );
        Expect(
            HeapPass,
            HeapCode(false, callBarrier: true),
            null,
            BeamCheckedFramePeepholeDiagnostics.CallBarrier,
            AbsentImport
        );
        Expect(
            HeapPass,
            HeapCode(false, live: 2),
            null,
            BeamCheckedFramePeepholeDiagnostics.LaterRoots,
            AbsentImport
        );
        Expect(
            HeapPass,
            HeapCode(false, lineBarrier: true),
            null,
            BeamCheckedFramePeepholeDiagnostics.DifferentLine,
            AbsentImport
        );

        Expect(
            HeapPass,
            ProjectedFieldsHeapCode(false),
            ProjectedFieldsHeapCode(true),
            BeamCheckedFramePeepholeDiagnostics.CheckedProjection,
            AbsentImport
        );
        Expect(
            HeapPass,
            ProjectedFieldsHeapCode(false, duplicateDestination: true),
            null,
            BeamCheckedFramePeepholeDiagnostics.LaterRoots,
            AbsentImport
        );
        Expect(
            HeapPass,
            ProjectedFieldsHeapCode(false, lineBarrier: true),
            null,
            BeamCheckedFramePeepholeDiagnostics.DifferentLine,
            AbsentImport
        );

        Expect(
            HeapPass,
            ProjectedDictionaryHeapCode(false),
            ProjectedDictionaryHeapCode(true),
            BeamCheckedFramePeepholeDiagnostics.CheckedProjection,
            DictionaryImport
        );
        foreach (var barrier in new[]
        {
            ProjectedDictionaryHeapCode(false, import: OtherImport),
            ProjectedDictionaryHeapCode(false, field: 3),
            ProjectedDictionaryHeapCode(false, live: 2),
            ProjectedDictionaryHeapCode(false, lineBarrier: true)
        })
        {
            Expect(
                HeapPass,
                barrier,
                null,
                BeamCheckedFramePeepholeDiagnostics.DictionaryLookupBarrier,
                DictionaryImport
            );
        }

        Expect(
            HeapPass,
            DictionaryHeapCode(false),
            DictionaryHeapCode(true),
            BeamCheckedFramePeepholeDiagnostics.DictionaryLookup,
            DictionaryImport
        );

        foreach (var barrier in new[]
        {
            DictionaryHeapCode(false, import: OtherImport),
            DictionaryHeapCode(false, failure: 3),
            DictionaryHeapCode(false, live: 2),
            DictionaryHeapCode(false, lineBarrier: true),
            DictionaryHeapCode(false, registerKey: true),
            DictionaryHeapCode(false, updatesX: true)
        })
        {
            Expect(
                HeapPass,
                barrier,
                null,
                BeamCheckedFramePeepholeDiagnostics.DictionaryLookupBarrier,
                DictionaryImport
            );
        }

        return Task.CompletedTask;
    }

    private static byte[] ReturnCode(
        bool shared,
        int lateFrame = 2,
        int lateResult = 1,
        bool lateLine = false
    )
    {
        var code = ModulePrefix();
        code.AddRange([ExactEqualTest, F(4), X(0), A(3)]);
        SaveFrame(code, 2);
        code.AddRange([ExactNotEqualTest, F(3), X(0), A(3)]);
        TupleReturn(code, 2, 0);
        if (!shared)
        {
            code.AddRange([Label, U(3)]);
            TupleReturn(code, 2, 1);
        }

        code.AddRange([Label, U(4)]);
        SaveFrame(code, lateFrame);
        code.AddRange([ExactEqualTest, F(5), X(0), A(4)]);
        if (shared)
        {
            code.AddRange([Label, U(3)]);
        }

        if (lateLine)
        {
            code.AddRange([Line, U(1)]);
        }

        TupleReturn(code, lateFrame, lateResult);
        code.AddRange([Label, U(5)]);
        TupleReturn(code, lateFrame, 0);
        code.Add(EndCode);

        return code.ToArray();
    }

    private static byte[] HeapCode(
        bool combined,
        bool callBarrier = false,
        int live = 0,
        bool lineBarrier = false
    )
    {
        var code = ModulePrefix();
        code.AddRange([IsTuple, F(3), X(0)]);
        code.AddRange([SelectTupleArity, X(0), F(3), ExtendedListOperand, U(2), U(2), F(4)]);
        code.AddRange([Label, U(4)]);
        code.AddRange([GetTupleElement, X(0), U(0), X(2)]);
        code.AddRange([ExactEqualTest, F(3), X(2), A(3)]);
        code.AddRange(combined ? [AllocateHeap, U(2), U(3), U(2)] : [Allocate, U(2), U(2)]);
        code.AddRange([Move, X(1), Y(1)]);
        code.AddRange([GetTupleElement, X(0), U(1), Y(0)]);
        if (callBarrier)
        {
            code.AddRange([ExternalCall, U(0), U(0)]);
        }

        if (lineBarrier)
        {
            code.AddRange([Line, U(1)]);
        }

        if (!combined)
        {
            code.AddRange([TestHeap, U(3), U(live)]);
        }

        code.AddRange([PutTuple, X(0), ExtendedListOperand, U(2), Y(0), Y(1)]);
        code.AddRange([Deallocate, U(2), Return, Label, U(3), Return, EndCode]);

        return code.ToArray();
    }

    private static byte[] ProjectedFieldsHeapCode(
        bool combined,
        bool duplicateDestination = false,
        bool lineBarrier = false
    )
    {
        var code = ModulePrefix();
        code.AddRange([IsTaggedTuple, F(3), X(0), U(3), A(3)]);
        code.AddRange(combined ? [AllocateHeap, U(3), U(3), U(2)] : [Allocate, U(3), U(2)]);
        code.AddRange([Move, X(0), Y(2), GetTupleElement, Y(2), U(1), Y(0)]);
        code.AddRange([GetTupleElement, Y(2), U(2), Y(duplicateDestination ? 0 : 1)]);
        if (lineBarrier)
        {
            code.AddRange([Line, U(1)]);
        }

        if (!combined)
        {
            code.AddRange([TestHeap, U(3), U(0)]);
        }

        code.AddRange([PutTuple, X(0), ExtendedListOperand, U(2), Y(0), Y(duplicateDestination ? 2 : 1)]);
        code.AddRange([Deallocate, U(3), Return, Label, U(3), Return, EndCode]);

        return code.ToArray();
    }

    private static byte[] ProjectedDictionaryHeapCode(
        bool combined,
        int import = DictionaryImport,
        int field = 1,
        int live = 1,
        bool lineBarrier = false
    )
    {
        var code = ModulePrefix();
        code.AddRange([IsTaggedTuple, F(3), X(0), U(3), A(3)]);
        code.AddRange(combined ? [AllocateHeap, U(2), U(2), U(2)] : [Allocate, U(2), U(2)]);
        code.AddRange([Move, X(1), Y(1), GetTupleElement, X(0), U(field), Y(0)]);
        code.AddRange([BifOne, F(0), U(import), A(3), X(0)]);
        if (lineBarrier)
        {
            code.AddRange([Line, U(1)]);
        }

        if (!combined)
        {
            code.AddRange([TestHeap, U(2), U(live)]);
        }

        code.AddRange([PutList, Y(0), A(0), X(1), Deallocate, U(2), Return, Label, U(3), Return, EndCode]);

        return code.ToArray();
    }

    private static byte[] DictionaryHeapCode(
        bool combined,
        int import = DictionaryImport,
        int failure = 0,
        int live = 1,
        bool lineBarrier = false,
        bool registerKey = false,
        bool updatesX = false
    )
    {
        var code = ModulePrefix();
        code.AddRange(combined ? [AllocateHeap, U(2), U(2), U(2)] : [Allocate, U(2), U(2)]);
        code.AddRange([InitYRegisters, ExtendedListOperand, U(1), Y(0), Move, X(1), Y(1)]);
        if (updatesX)
        {
            code.AddRange([Move, X(0), X(1)]);
        }

        code.AddRange([BifOne, F(failure), U(import), registerKey ? X(0) : A(3), X(0)]);
        if (lineBarrier)
        {
            code.AddRange([Line, U(1)]);
        }

        if (!combined)
        {
            code.AddRange([TestHeap, U(2), U(live)]);
        }

        code.AddRange([PutList, Y(1), A(0), X(1), Deallocate, U(2), Return]);
        if (failure != 0)
        {
            code.AddRange([Label, U(failure), Deallocate, U(2), Return]);
        }

        code.Add(EndCode);

        return code.ToArray();
    }

    private static List<byte> ModulePrefix() => [Label, U(1), FunctionInfo, A(1), A(2), U(2), Label, U(2)];

    private static void SaveFrame(List<byte> code, int frame)
    {
        code.AddRange([Allocate, U(frame), U(2)]);
        code.AddRange([Move, X(0), Y(0), Move, X(1), Y(1)]);
        if (frame > 2)
        {
            code.AddRange([InitYRegisters, ExtendedListOperand, U(1), Y(2)]);
        }
    }

    private static void TupleReturn(List<byte> code, int frame, int slot)
    {
        code.AddRange([TestHeap, U(3), U(0)]);
        code.AddRange([PutTuple, X(0), ExtendedListOperand, U(2), A(5), Y(slot)]);
        code.AddRange([Deallocate, U(frame), Return]);
    }

    private static void Expect(
        string method,
        byte[] source,
        byte[]? expected,
        string diagnostics,
        params object[] arguments
    )
    {
        ReadOnlyMemory<byte> bytes = source;
        object header = New(
            "BeamCodeHeader",
            0,
            (int)InitYRegisters,
            6,
            1,
            bytes
        );
        object instructions = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            6,
            0,
            null,
            null
        );
        MethodInfo? pass = typeof(BeamModuleCompiler).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static);
        byte[]? actual = pass?.Invoke(null, [bytes, instructions, .. arguments]) as byte[];
        if (expected is null ? actual is not null : actual is null || !expected.AsSpan().SequenceEqual(actual))
        {
            throw new InvalidOperationException(diagnostics);
        }
    }

    private static byte U(int value) => Compact(value, 0);

    private static byte X(int value) => Compact(value, XRegisterTag);

    private static byte Y(int value) => Compact(value, YRegisterTag);

    private static byte A(int value) => Compact(value, AtomTag);

    private static byte F(int value) => Compact(value, LabelTag);

    private static byte Compact(int value, int tag) => (byte)((value << 4) | tag);
}
