using System.Reflection;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Tests;

internal static class BeamRetiredCheckedCallSlotChecks
{
    internal const string TestName = "compiler/beam/reuses-a-retired-slot-only-on-the-owned-checked-call-path";

    private const int UnsignedOperandTag = 0;
    private const int IntegerOperandTag = 1;

    internal static Task Run()
    {
        var type = typeof(CodeGeneration).Assembly.GetType("Erlang.Compiler.BeamModuleCompiler", throwOnError: true)!;
        const BindingFlags members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        ConstructorInfo constructor = type.GetConstructors(members).Single();
        object?[] arguments = constructor.GetParameters()
            .Select(parameter => parameter.Position == 0 ? "retired_checked_call_slot" : parameter.DefaultValue)
            .ToArray();
        object compiler = constructor.Invoke(arguments);
        if (Rewrite(Fixture()) is not byte[] reused || !reused.AsSpan().SequenceEqual(Fixture(earlySlot: 2))
            || Rewrite(Fixture(readsHighSlotOnColdPath: true)) is not byte[] lower
            || !lower.AsSpan().SequenceEqual(Fixture(earlySlot: 1, readsHighSlotOnColdPath: true))
            || Rewrite(Fixture(extraColdEntry: true)) is not null
            || Rewrite(Fixture(liveRawOnSuccess: true)) is not null
            || Rewrite(Fixture(liveOldSlotAtJoin: true)) is not null)
        {
            throw new InvalidOperationException("Checked call projection must reuse only a retired slot and preserve the sibling owner.");
        }

        return Task.CompletedTask;

        object? Rewrite(byte[] code) => BeamBinaryHostAccess.Call(
            compiler,
            "ReuseRetiredCheckedCallSlot",
            (ReadOnlyMemory<byte>)code,
            7
        );
    }

    private static byte[] Fixture(
        int? earlySlot = null,
        bool readsHighSlotOnColdPath = false,
        bool extraColdEntry = false,
        bool liveRawOnSuccess = false,
        bool liveOldSlotAtJoin = false
    )
    {
        const int rejected = 3;
        const int sibling = 4;
        const int join = 5;
        int projection = earlySlot.HasValue ? 0 : 2;
        int coldSlot = earlySlot ?? 0;
        using var code = new MemoryStream();
        Mark(1);
        Op(
            FunctionInfo,
            (AtomTag, 1),
            (AtomTag, 1),
            (UnsignedOperandTag, 1)
        );
        Mark(2);
        Op(Allocate, (UnsignedOperandTag, 3), (UnsignedOperandTag, 1));
        Op(Move, (XRegisterTag, 0), (YRegisterTag, 2));
        Op(Move, (AtomTag, 1), (YRegisterTag, 0));
        Op(Move, (AtomTag, 1), (YRegisterTag, 1));
        Op(
            ExactEqualTest,
            (LabelTag, sibling),
            (XRegisterTag, 0),
            (IntegerOperandTag, 1)
        );
        if (extraColdEntry)
        {
            Op(
                ExactNotEqualTest,
                (LabelTag, rejected),
                (XRegisterTag, 0),
                (IntegerOperandTag, 2)
            );
        }

        Op(ExternalCall, (UnsignedOperandTag, 1), (UnsignedOperandTag, 0));
        if (earlySlot.HasValue)
        {
            Save();
        }

        Op(
            IsTaggedTuple,
            (LabelTag, rejected),
            (XRegisterTag, 0),
            (UnsignedOperandTag, 3),
            (AtomTag, 1)
        );
        Op(
            GetTupleElement,
            (XRegisterTag, 0),
            (UnsignedOperandTag, 1),
            (XRegisterTag, projection)
        );
        Op(
            ExactEqualTest,
            (LabelTag, rejected),
            (XRegisterTag, projection),
            (IntegerOperandTag, 9)
        );
        if (!liveRawOnSuccess)
        {
            Op(Move, (YRegisterTag, 0), (XRegisterTag, 0));
        }

        Op(Jump, (LabelTag, join));
        Mark(rejected);
        if (!earlySlot.HasValue)
        {
            Save();
        }

        if (readsHighSlotOnColdPath)
        {
            Op(Move, (YRegisterTag, 2), (XRegisterTag, 2));
        }

        ErrorList(coldSlot);
        Op(ExternalCall, (UnsignedOperandTag, 1), (UnsignedOperandTag, 0));
        ErrorList(coldSlot);
        Op(Jump, (LabelTag, join));
        Mark(sibling);
        ErrorList(2);
        Op(Jump, (LabelTag, join));
        Mark(join);
        if (liveOldSlotAtJoin)
        {
            Op(Move, (YRegisterTag, 0), (XRegisterTag, 0));
        }

        Op(Deallocate, (UnsignedOperandTag, 3));
        Op(Return);
        Op(EndCode);

        return code.ToArray();

        void ErrorList(int slot)
        {
            Op(TestHeap, (UnsignedOperandTag, 2), (UnsignedOperandTag, 0));
            Op(
                PutList,
                (YRegisterTag, slot),
                (AtomTag, 0),
                (XRegisterTag, 0)
            );
        }

        void Mark(int label) => Op(Label, (UnsignedOperandTag, label));

        void Save() => Op(Move, (XRegisterTag, 0), (YRegisterTag, coldSlot));

        void Op(byte opcode, params (int Tag, int Value)[] operands)
        {
            code.WriteByte(opcode);
            foreach (var (tag, value) in operands)
            {
                BeamCompactWriter.Write(code, tag, value);
            }
        }
    }
}
