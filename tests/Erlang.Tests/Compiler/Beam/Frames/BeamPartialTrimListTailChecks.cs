using System.Reflection;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Tests;

internal static class BeamPartialTrimListTailChecks
{
    internal const string TestName = "compiler/beam/reuses-cons-tail-scratch-before-a-single-frame-trim";

    private const int UnsignedOperandTag = 0;

    internal static Task Run()
    {
        var type = typeof(CodeGeneration).Assembly.GetType("Erlang.Compiler.BeamModuleCompiler", throwOnError: true)!;
        const BindingFlags members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        ConstructorInfo constructor = type.GetConstructors(members).Single();
        object?[] arguments = constructor.GetParameters()
            .Select(parameter => parameter.Position == 0 ? "partial_trim_list_tail" : parameter.DefaultValue)
            .ToArray();
        object compiler = constructor.Invoke(arguments);
        if (Rewrite(Fixture(1)) is not byte[] removed || !removed.AsSpan().SequenceEqual(Fixture(1, coalesced: true))
            || Rewrite(Fixture(2)) is not byte[] shrunk || !shrunk.AsSpan().SequenceEqual(Fixture(2, coalesced: true))
            || Rewrite(Fixture(1, keepsScratch: true)) is not null
            || Rewrite(Fixture(1, crossesTrim: true)) is not null
            || Rewrite(Fixture(1, readsOldSourceOnFailure: true)) is not null)
        {
            throw new InvalidOperationException("Cons-tail coalescing must preserve retained slot numbering and reject observable scratch values.");
        }

        return Task.CompletedTask;

        object? Rewrite(byte[] code) => BeamBinaryHostAccess.Call(
            compiler,
            "CoalescePartialTrimListTails",
            (ReadOnlyMemory<byte>)code,
            6
        );
    }

    private static byte[] Fixture(
        int dropped,
        bool coalesced = false,
        bool keepsScratch = false,
        bool crossesTrim = false,
        bool readsOldSourceOnFailure = false
    )
    {
        const int rejected = 3;
        const int continuation = 4;
        const int cross = 5;
        const int kept = 2;
        const int unsigned = UnsignedOperandTag;
        int width = dropped + kept;
        int source = dropped + 1;
        int head = dropped;
        int scratch = coalesced ? source - 1 : 0;
        int offset = coalesced ? -1 : 0;
        using var code = new MemoryStream();
        Op(Label, (unsigned, 1));
        Op(
            FunctionInfo,
            (AtomTag, 1),
            (AtomTag, 1),
            (unsigned, 1)
        );
        Op(Label, (unsigned, 2));
        Op(Allocate, (unsigned, width + offset), (unsigned, 1));
        code.WriteByte(InitYRegisters);
        code.WriteByte(ExtendedListOperand);
        BeamCompactWriter.Write(code, unsigned, width + offset);
        for (int slot = 0; slot < width + offset; slot++)
        {
            BeamCompactWriter.Write(code, YRegisterTag, slot);
        }

        Op(Move, (XRegisterTag, 0), (YRegisterTag, source + offset));
        Op(IsNonemptyList, (LabelTag, rejected), (YRegisterTag, source + offset));
        Op(
            GetList,
            (YRegisterTag, source + offset),
            (YRegisterTag, head + offset),
            (YRegisterTag, scratch)
        );
        Op(IsNonemptyList, (LabelTag, rejected), (YRegisterTag, scratch));
        Op(GetTail, (YRegisterTag, scratch), (YRegisterTag, source + offset));
        if (keepsScratch)
        {
            Op(Move, (YRegisterTag, scratch), (XRegisterTag, 1));
        }

        if (crossesTrim)
        {
            Op(
                ExactEqualTest,
                (LabelTag, cross),
                (XRegisterTag, 0),
                (AtomTag, 1)
            );
        }

        Op(Jump, (LabelTag, continuation));
        Op(Label, (unsigned, rejected));
        if (readsOldSourceOnFailure)
        {
            Op(Move, (YRegisterTag, source + offset), (XRegisterTag, 0));
        }

        Op(BadMatch, (XRegisterTag, 0));
        Op(Label, (unsigned, continuation));
        if (dropped + offset > 0)
        {
            Op(TrimFrame, (unsigned, dropped + offset), (unsigned, kept));
        }

        Op(Move, (YRegisterTag, 1), (XRegisterTag, 0));
        Op(Deallocate, (unsigned, kept));
        Op(Return);
        if (crossesTrim)
        {
            Op(Label, (unsigned, cross));
            Op(BadMatch, (XRegisterTag, 0));
        }

        Op(EndCode);

        return code.ToArray();

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
