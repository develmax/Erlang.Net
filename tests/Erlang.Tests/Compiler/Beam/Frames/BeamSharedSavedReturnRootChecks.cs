using System.Reflection;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Tests;

internal static class BeamSharedSavedReturnRootChecks
{
    internal const string TestName = "compiler/beam/clears-dead-roots-before-a-shared-saved-return";
    private const int UnsignedOperandTag = 0;
    private const string WrongRoots = "A shared saved result did not preserve the final argument read or a live post-call root.";

    internal static Task Run()
    {
        var type = typeof(CodeGeneration).Assembly.GetType("Erlang.Compiler.BeamModuleCompiler", throwOnError: true)!;
        const BindingFlags members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        ConstructorInfo constructor = type.GetConstructors(members).Single();
        object?[] arguments = constructor.GetParameters()
            .Select(parameter => parameter.Position == 0 ? "shared_saved_return_roots" : parameter.DefaultValue).ToArray();
        object compiler = constructor.Invoke(arguments);
        if (RunRewrite(Fixture()) is not byte[] rewritten || !rewritten.AsSpan().SequenceEqual(Fixture(clear: true))
            || RunRewrite(Fixture(alternateReadsOldValue: true)) is not byte[] preserved
            || !preserved.AsSpan().SequenceEqual(Fixture(clear: true, alternateReadsOldValue: true))
            || RunRewrite(Fixture(retainsArgumentAtJoin: true)) is not null)
        {
            throw new InvalidOperationException(WrongRoots);
        }

        return Task.CompletedTask;

        object? RunRewrite(byte[] code) => BeamBinaryHostAccess.Call(
            compiler,
            "ClearRootsBeforeSharedSavedReturns",
            (ReadOnlyMemory<byte>)code,
            5
        );
    }

    private static byte[] Fixture(
        bool clear = false,
        bool alternateReadsOldValue = false,
        bool retainsArgumentAtJoin = false
    )
    {
        const int second = 3;
        const int join = 4;
        const int liveField = 1;
        const int savedResult = 0;
        using var code = new MemoryStream();
        Op(Label, (UnsignedOperandTag, 1));
        Op(
            FunctionInfo,
            (AtomTag, 1),
            (AtomTag, 1),
            (UnsignedOperandTag, 2)
        );
        Op(Label, (UnsignedOperandTag, 2));
        Op(Allocate, (UnsignedOperandTag, 2), (UnsignedOperandTag, 2));
        Init(savedResult);
        Op(Move, (XRegisterTag, 1), (YRegisterTag, liveField));
        Op(
            ExactEqualTest,
            (LabelTag, second),
            (XRegisterTag, 0),
            (AtomTag, 1)
        );
        Arm(clear);
        Op(Jump, (LabelTag, join));
        Op(Label, (UnsignedOperandTag, second));
        Arm(clear && !alternateReadsOldValue, alternateReadsOldValue);
        Op(Label, (UnsignedOperandTag, join));
        Op(ExternalCall, (UnsignedOperandTag, 0), (UnsignedOperandTag, 0));
        Op(Move, (YRegisterTag, retainsArgumentAtJoin ? liveField : savedResult), (XRegisterTag, 0));
        Op(Deallocate, (UnsignedOperandTag, 2));
        Op(Return);
        Op(EndCode);

        return code.ToArray();

        void Arm(bool clearArgument, bool preserve = false)
        {
            Op(Move, (YRegisterTag, liveField), (XRegisterTag, 0));
            if (clearArgument)
            {
                Init(liveField);
            }

            Op(ExternalCall, (UnsignedOperandTag, 1), (UnsignedOperandTag, 1));
            Op(Move, preserve ? (YRegisterTag, liveField) : (XRegisterTag, 0), (YRegisterTag, savedResult));
        }

        void Init(int slot)
        {
            code.WriteByte(InitYRegisters);
            code.WriteByte(ExtendedListOperand);
            BeamCompactWriter.Write(code, UnsignedOperandTag, 1);
            BeamCompactWriter.Write(code, YRegisterTag, slot);
        }

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
