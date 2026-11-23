using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamEntryLiteralRematerializationChecks
{
    internal const string TestName = "compiler/beam/rematerializes-closed-entry-frame-reads";
    private const string WrongLayout = "The literal spill must only be removed when every frame read still sees its initial literal.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_entry_literal_probe");
        _ = Call(compiler, "Intern", "success");
        _ = Call(compiler, "LiteralOperand", Term.List(Term.I(7)));
        byte[] compound = [ExtendedLiteralOperand, U(0)];
        byte[] atom = [A(2)];
        foreach (byte[] value in new[] { compound, atom })
        {
            byte[] outgoing = [Move, .. value, X(0)];
            byte[] saved = [Move, .. value, Y(0)];
            if (Apply(Fixture(value)) is not { } rewritten
                || rewritten.AsSpan().IndexOf(outgoing) < 0
                || rewritten.AsSpan().IndexOf(saved) >= 0
                || Apply(rewritten) is not null || Apply(Fixture(value, overwrite: true)) is not null)
            {
                throw new InvalidOperationException(WrongLayout);
            }
        }

        foreach (byte[] value in new[] { compound, atom })
        {
            if (ApplyOwned(Owned(value)) is not { } rewritten
                || !rewritten.SequenceEqual(Owned(value, compacted: true))
                || ApplyOwned(rewritten) is not null || ApplyOwned(Owned(value, bypass: true)) is not null
                || ApplyOwned(Owned(value, cleared: true)) is not null || ApplyOwned(Owned(value, rebound: true)) is not null)
            {
                throw new InvalidOperationException(WrongLayout);
            }
        }

        foreach (byte[] value in new[] { compound, atom })
        {
            if (ApplyOwned(Constructed(value)) is not { } rewritten
                || !rewritten.SequenceEqual(Constructed(value, compacted: true))
                || ApplyOwned(rewritten) is not null
                || ApplyOwned(Constructed(value, typed: true)) is not null
                || ApplyOwned(Constructed(value, overwrites: true)) is not null)
            {
                throw new InvalidOperationException(WrongLayout);
            }
        }

        foreach (byte[] value in new[] { compound, atom })
        {
            if (ApplyTrimmed(Trimmed(value)) is not { } rewritten
                || !rewritten.SequenceEqual(Trimmed(value, compacted: true))
                || ApplyTrimmed(rewritten) is not null
                || ApplyTrimmed(Trimmed(value, inherited: true)) is not null
                || ApplyTrimmed(Trimmed(value, bypass: true)) is not null
                || ApplyTrimmed(Trimmed(value, typedRead: true)) is not null)
            {
                throw new InvalidOperationException(WrongLayout);
            }
        }

        return Task.CompletedTask;

        byte[]? ApplyTrimmed(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "RematerializePostTrimLiteralSlot",
            code,
            4
        );

        byte[]? ApplyOwned(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "RematerializeOwnedLiteralSlot",
            code,
            4
        );

        byte[]? Apply(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "RematerializeEntryLiteralFrames",
            code,
            4
        );
    }

    private static byte[] Owned(
        byte[] value,
        bool compacted = false,
        bool bypass = false,
        bool cleared = false,
        bool rebound = false
    )
    {
        byte saved = compacted ? Y(0) : Y(1);
        byte[] initial = compacted ? [] : [InitYRegisters, ExtendedListOperand, U(1), Y(0)];
        byte[] branch = bypass ? [IsAtom, F(3), X(0)] : [];
        byte[] store = compacted ? [] : [Move, .. value, Y(0)];
        byte[] clear = cleared ? [InitYRegisters, ExtendedListOperand, U(1), Y(0)] : [];
        byte[] extra = rebound ? [Move, A(1), Y(0)] : [];
        byte[] source = compacted ? value : [Y(0)];

        return
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            Allocate, U(compacted ? 1 : 2), U(1), .. initial, Move, X(0), saved,
            ExternalCall, U(1), U(0), .. branch, .. store, .. clear, .. extra,
            Label, U(3), Move, .. source, X(1), Move, saved, X(0),
            Line, U(1), ExternalCall, U(2), U(1), Deallocate, U(compacted ? 1 : 2), Return, EndCode
        ];
    }

    private static byte[] Constructed(
        byte[] value,
        bool compacted = false,
        bool typed = false,
        bool overwrites = false
    )
    {
        byte retained = compacted ? Y(0) : Y(1);
        byte[] prefix = compacted ? [] : [InitYRegisters, ExtendedListOperand, U(1), Y(0)];
        byte[] store = compacted ? [] : [Move, .. value, Y(0)];
        byte[] read = compacted ? value : [Y(0)];
        byte[] head = typed ? [ExtendedTypedRegisterOperand, Y(0), U(1)] : read;
        byte result = overwrites ? Y(0) : X(1);

        return
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            Allocate, U(compacted ? 1 : 2), U(1), .. prefix, Move, X(0), retained,
            Line, U(1), ExternalCall, U(1), U(0), .. store,
            TestHeap, U(7), U(1), PutList, .. head, X(0), result,
            PutTuple, X(0), ExtendedListOperand, U(4), .. read, result, retained, .. read,
            Deallocate, U(compacted ? 1 : 2), Return, EndCode
        ];
    }

    private static byte[] Trimmed(
        byte[] value,
        bool compacted = false,
        bool inherited = false,
        bool bypass = false,
        bool typedRead = false
    )
    {
        byte[] initial = compacted ? [] : [InitYRegisters, ExtendedListOperand, U(1), Y(2)];
        byte[] existing = inherited ? [Move, X(0), Y(2)] : [];
        byte[] definition = compacted ? [] : [Move, .. value, Y(1)];
        byte[] source = compacted ? value : typedRead ? [ExtendedTypedRegisterOperand, Y(1), U(1)] : [Y(1)];
        byte[] relocation = compacted ? [] : [Move, Y(0), Y(1), TrimFrame, U(1), U(1)];
        byte[] split = bypass ? [IsAtom, F(3), X(0)] : [];
        byte[] join = bypass ? [Label, U(3)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            Allocate, U(compacted ? 2 : 3), U(1), .. initial, Move, X(0), Y(0), Move, A(2), Y(1), .. existing,
            Move, Y(0), X(0), ExternalCall, U(1), U(0), TrimFrame, U(1), U(compacted ? 1 : 2),
            .. split, .. definition, .. join, Move, .. source, X(1), Move, Y(0), X(0),
            Line, U(1), ExternalCall, U(2), U(1), Move, .. source, X(1), .. relocation,
            Move, Y(0), X(0), ExternalCall, U(2), U(2), Deallocate, U(1), Return, EndCode
        ];
    }

    private static byte[] Fixture(byte[] value, bool overwrite = false)
    {
        byte[] write = overwrite ? [Move, X(0), Y(0)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                0
            ),
            Allocate, U(2), U(0), Move, .. value, Y(0),
            InitYRegisters, ExtendedListOperand, U(1), Y(1),
            Move, Y(0), X(0), ExternalCall, U(1), U(0), .. write,
            ExactEqualTest, F(3), X(0), A(2),
            Move, Y(0), X(0), Deallocate, U(2), Return,
            Label, U(3), Move, Y(0), X(0), Deallocate, U(2), Return,
            EndCode
        ];
    }
}
