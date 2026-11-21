using System.Reflection;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Tests;

internal static class BeamOverwrittenHandlerSlotChecks
{
    internal const string TestName = "compiler/beam/omits-copies-overwritten-by-handler-installation";
    private const int UnsignedOperandTag = 0;
    private const string WrongOverwrite = "Only a save immediately overwritten by installing its handler may be removed.";

    internal static Task Run()
    {
        var type = typeof(CodeGeneration).Assembly.GetType("Erlang.Compiler.BeamModuleCompiler", throwOnError: true)!;
        const BindingFlags members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        ConstructorInfo constructor = type.GetConstructors(members).Single();
        object?[] arguments = constructor.GetParameters()
            .Select(parameter => parameter.Position == 0 ? "overwritten_handler_slot" : parameter.DefaultValue).ToArray();
        object compiler = constructor.Invoke(arguments);
        foreach (byte install in new[] { CatchEnter, TryEnter })
        {
            if (Rewrite(Fixture(install)) is not byte[] result
                || !result.AsSpan().SequenceEqual(Fixture(install, save: false))
                || Rewrite(Fixture(install, otherSlot: true)) is not null
                || Rewrite(Fixture(install, separatedByLine: true)) is not null)
            {
                throw new InvalidOperationException(WrongOverwrite);
            }
        }

        return Task.CompletedTask;

        object? Rewrite(byte[] code) => BeamBinaryHostAccess.Call(
            compiler,
            "RemoveOverwrittenHandlerSlotCopies",
            (ReadOnlyMemory<byte>)code,
            4
        );
    }

    private static byte[] Fixture(
        byte install,
        bool save = true,
        bool otherSlot = false,
        bool separatedByLine = false
    )
    {
        const int handler = 0;
        const int landing = 3;
        using var code = new MemoryStream();
        Op(Label, (UnsignedOperandTag, 1));
        Op(
            FunctionInfo,
            (AtomTag, 1),
            (AtomTag, 1),
            (UnsignedOperandTag, 1)
        );
        Op(Label, (UnsignedOperandTag, 2));
        Op(Allocate, (UnsignedOperandTag, 2), (UnsignedOperandTag, 1));
        if (save)
        {
            Op(Move, (XRegisterTag, 0), (YRegisterTag, otherSlot ? 1 : handler));
        }

        if (separatedByLine)
        {
            Op(Line, (UnsignedOperandTag, 1));
        }

        Op(install, (YRegisterTag, handler), (LabelTag, landing));
        if (install == TryEnter)
        {
            Op(TryEnd, (YRegisterTag, handler));
            Op(Deallocate, (UnsignedOperandTag, 2));
            Op(Return);
        }

        Op(Label, (UnsignedOperandTag, landing));
        Op(install == CatchEnter ? CatchEnd : TryCase, (YRegisterTag, handler));
        Op(Deallocate, (UnsignedOperandTag, 2));
        Op(Return);
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
