using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Tests;

internal static class BeamPartialTrimCallExchangeChecks
{
    internal const string TestName = "compiler/beam/exchanges-call-result-before-redundant-single-slot-trim";
    private const int UnsignedOperandTag = 0;
    private const string Failure = "The exchange must preserve outgoing arguments and every exit that still uses the original frame.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("partial_trim_call_exchange");
        if (Rewrite(Fixture()) is not byte[] actual || !actual.AsSpan().SequenceEqual(Fixture(compact: true))
            || Rewrite(Fixture(usesScratchBeforeCall: true)) is not null
            || Rewrite(Fixture(crossesCut: true)) is not null)
        {
            throw new InvalidOperationException(Failure);
        }

        return Task.CompletedTask;

        object? Rewrite(byte[] code) => BeamBinaryHostAccess.Call(
            compiler,
            "CoalescePartialTrimCallExchanges",
            (ReadOnlyMemory<byte>)code,
            5
        );
    }

    private static byte[] Fixture(
        bool compact = false,
        bool usesScratchBeforeCall = false,
        bool crossesCut = false
    )
    {
        const int returning = 3;
        const int afterTrim = 4;
        int width = compact ? 1 : 2;
        int saved = compact ? 0 : 1;
        using var code = new MemoryStream();
        Op(Label, (UnsignedOperandTag, 1));
        Op(
            FunctionInfo,
            (AtomTag, 1),
            (AtomTag, 1),
            (UnsignedOperandTag, 1)
        );
        Op(Label, (UnsignedOperandTag, 2));
        Op(Allocate, (UnsignedOperandTag, width), (UnsignedOperandTag, 1));
        if (!compact)
        {
            code.WriteByte(InitYRegisters);
            code.WriteByte(ExtendedListOperand);
            BeamCompactWriter.Write(code, UnsignedOperandTag, 1);
            BeamCompactWriter.Write(code, YRegisterTag, 0);
        }

        Op(Move, (XRegisterTag, 0), (YRegisterTag, saved));
        Op(
            ExactEqualTest,
            (LabelTag, crossesCut ? afterTrim : returning),
            (XRegisterTag, 0),
            (AtomTag, 1)
        );
        Op(
            TailExternalCallWithFrame,
            (UnsignedOperandTag, 1),
            (UnsignedOperandTag, 0),
            (UnsignedOperandTag, width)
        );
        Op(Label, (UnsignedOperandTag, returning));
        if (usesScratchBeforeCall)
        {
            Op(Move, (YRegisterTag, 0), (XRegisterTag, 0));
        }

        Op(ExternalCall, (UnsignedOperandTag, 1), (UnsignedOperandTag, 0));
        if (compact)
        {
            Op(Swap, (YRegisterTag, saved), (XRegisterTag, 0));
        }
        else
        {
            Op(Move, (XRegisterTag, 0), (YRegisterTag, 0));
            Op(Move, (YRegisterTag, saved), (XRegisterTag, 0));
            Op(Move, (YRegisterTag, 0), (YRegisterTag, saved));
            Op(TrimFrame, (UnsignedOperandTag, 1), (UnsignedOperandTag, 1));
        }

        Op(Label, (UnsignedOperandTag, afterTrim));
        Op(ExternalCall, (UnsignedOperandTag, 1), (UnsignedOperandTag, 1));
        Op(Move, (YRegisterTag, 0), (XRegisterTag, 0));
        Op(Deallocate, (UnsignedOperandTag, 1));
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
