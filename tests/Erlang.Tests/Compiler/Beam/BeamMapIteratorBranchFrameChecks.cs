using System.Reflection;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamMapIteratorBranchFrameChecks
{
    internal const string TestName = "compiler/beam/owns-map-iterator-fallback-frame";
    private const string Probe = "beam_map_iterator_branch_frame_probe";
    private const string Parent = "choose";
    private const string Helper = "-choose/1-lc$^0/1-0-";
    private const string Tag = "matched";
    private const string Unsafe = "The iterator branch did not keep its original payload and error edge.";
    private const int SourceFrame = 2;
    private const int OwnedFrame = 1;
    private const int IteratorEntry = 3;
    private const int Success = 4;
    private const int HelperInfo = 5;
    private const int HelperEntry = 6;
    private const int IteratorLine = 1;
    private const int FromListLine = 2;

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler(Probe);
        int parent = (int)Call(compiler, "Intern", Parent)!;
        int helper = (int)Call(compiler, "Intern", Helper)!;
        int tag = (int)Call(compiler, "Intern", Tag)!;
        int reason = (int)Call(compiler, "Intern", ComprehensionErrorReasons.BadGenerator)!;
        int iterator = (int)Call(
            compiler,
            "Import",
            "erts_internal",
            "mc_iterator",
            1
        )!;
        int error = (int)Call(
            compiler,
            "Import",
            "erlang",
            "error",
            1
        )!;
        int fromList = (int)Call(
            compiler,
            "Import",
            "maps",
            "from_list",
            1
        )!;
        var provenance = (Dictionary<(string Name, int Arity), Atom?>)typeof(BeamModuleCompiler)
            .GetField("mapIteratorHelpers", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(compiler)!;
        provenance.Add((Helper, 1), null);
        byte[] before = Fixture();
        byte[] expected = Fixture(lowered: true);
        if (Rewrite(before) is not { } after || !after.SequenceEqual(expected)
            || Rewrite(after) is not null
            || Rewrite(Fixture(failureInput: 0)) is not null
            || Rewrite(Fixture(bypassIterator: true)) is not null
            || Rewrite(Fixture(mapBase: true)) is not { } mapped
            || !mapped.SequenceEqual(Fixture(lowered: true, mapBase: true)))
        {
            throw new InvalidOperationException(Unsafe);
        }

        var atomMap = Enumerable.Range(0, 15).ToArray();
        (atomMap[parent], atomMap[helper]) = (atomMap[helper], atomMap[parent]);
        (atomMap[tag], atomMap[reason]) = (atomMap[reason], atomMap[tag]);
        SetTable("emittedAtoms", atomMap);
        var importMap = new int[] { 2, 0, 1 };
        SetTable("emittedImports", importMap);
        (parent, helper, tag, reason) = (atomMap[parent], atomMap[helper], atomMap[tag], atomMap[reason]);
        (iterator, error, fromList) = (importMap[iterator], importMap[error], importMap[fromList]);
        byte[] reordered = Fixture();
        if (Rewrite(reordered) is not { } lowered || !lowered.SequenceEqual(Fixture(lowered: true)))
        {
            throw new InvalidOperationException(Unsafe);
        }

        provenance.Clear();
        if (Rewrite(reordered) is not null)
        {
            throw new InvalidOperationException(Unsafe);
        }

        return Task.CompletedTask;

        void SetTable(string field, int[] table) => typeof(BeamModuleCompiler)
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(compiler, table);

        byte[]? Rewrite(ReadOnlyMemory<byte> bytes) => (byte[]?)Call(
            compiler,
            "RewriteOwnedMapIteratorBranches",
            bytes,
            HelperEntry + 1
        );

        byte[] Fixture(
            bool lowered = false,
            int failureInput = 1,
            bool bypassIterator = false,
            bool mapBase = false
        )
        {
            byte[] head = lowered
                ? []
                : [Allocate, U(SourceFrame), U(1), InitYRegisters, ExtendedListOperand, U(1), Y(0), Move, X(0), Y(1)];
            byte carrier = lowered ? X(0) : Y(1);
            byte[] construction = mapBase
                ? [PutMapAssoc, F(0), carrier, X(0), U(lowered ? 1 : 0), ExtendedListOperand, U(2), A(tag), carrier]
                :
                [
                    TestHeap, U(3), U(lowered ? 1 : 0),
                    PutTuple, X(0), ExtendedListOperand, U(2), A(tag), carrier
                ];
            byte[] release = lowered ? [] : [Deallocate, U(SourceFrame)];
            byte[] branch = lowered ? [Allocate, U(OwnedFrame), U(1), Move, X(0), Y(0)] : [];
            byte[] success = lowered ? [TrimFrame, U(OwnedFrame), U(0)] : [Move, X(0), Y(0)];

            return
            [
                .. Start(
                    1,
                    2,
                    parent,
                    1
                ),
                .. head,
                mapBase ? IsMap : IsAtom, F(bypassIterator ? Success : IteratorEntry), carrier,
                .. construction,
                .. release, Return,
                Label, U(IteratorEntry), .. (lowered ? new byte[] { } : [Move, carrier, X(0)]), .. branch,
                Line, U(IteratorLine), ExternalCall, U(1), U(iterator),
                IsNil, F(Success), X(0), TestHeap, U(3), U(0),
                PutTuple, X(0), ExtendedListOperand, U(2), A(reason), Y(lowered ? 0 : failureInput),
                TailExternalCallWithFrame, U(1), U(error), U(lowered ? OwnedFrame : SourceFrame),
                Label, U(Success), .. success,
                LocalCall, U(1), F(HelperEntry),
                Line, U(FromListLine), TailExternalCallWithFrame, U(1), U(fromList), U(lowered ? 0 : SourceFrame),
                .. Start(
                    HelperInfo,
                    HelperEntry,
                    helper,
                    1
                ),
                Return, EndCode
            ];
        }
    }
}
