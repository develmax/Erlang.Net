using System.Reflection;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Tests;

internal static class BeamRepeatedBinaryContextLoadChecks
{
    internal const string TestName = "compiler/beam/keeps-live-binary-context-across-adjacent-field-reads";

    private const int UnsignedOperandTag = 0;
    private const int IntegerOperandTag = 1;

    internal static Task Run()
    {
        var type = typeof(CodeGeneration).Assembly.GetType("Erlang.Compiler.BeamModuleCompiler", throwOnError: true)!;
        const BindingFlags members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        ConstructorInfo constructor = type.GetConstructors(members).Single();
        object?[] arguments = constructor.GetParameters()
            .Select(parameter => parameter.Position == 0 ? "sequential_binary_context" : parameter.DefaultValue)
            .ToArray();
        object compiler = constructor.Invoke(arguments);
        if (Rewrite(Fixture()) is not byte[] shortened || !shortened.AsSpan().SequenceEqual(Fixture(reloaded: false))
            || Rewrite(Fixture(live: 0)) is not null
            || Rewrite(Fixture(destinationTag: XRegisterTag)) is not null
            || Rewrite(Fixture(destination: 1)) is not null
            || Rewrite(Fixture(labelBeforeReload: true)) is not null)
        {
            throw new InvalidOperationException("Binary context loads must respect GC roots, overwrites and branch entries.");
        }

        object? Rewrite(byte[] instructions) => BeamBinaryHostAccess.Call(
            compiler,
            "RemoveRepeatedBinaryContextLoads",
            (ReadOnlyMemory<byte>)instructions,
            5
        );

        return Task.CompletedTask;
    }

    private static byte[] Fixture(
        bool reloaded = true,
        int live = 1,
        int destinationTag = YRegisterTag,
        int destination = 0,
        bool labelBeforeReload = false
    )
    {
        const int failure = 3;
        const int unsigned = UnsignedOperandTag;
        using var code = new MemoryStream();
        Op(Label, (unsigned, 1));
        Op(
            FunctionInfo,
            (AtomTag, 1),
            (AtomTag, 1),
            (unsigned, 1)
        );
        Op(Label, (unsigned, 2));
        Op(Allocate, (unsigned, 2), (unsigned, 1));
        code.WriteByte(InitYRegisters);
        code.WriteByte(ExtendedListOperand);
        BeamCompactWriter.Write(code, unsigned, 2);
        BeamCompactWriter.Write(code, YRegisterTag, 0);
        BeamCompactWriter.Write(code, YRegisterTag, 1);
        Op(
            StartBinaryMatch,
            (LabelTag, failure),
            (XRegisterTag, 0),
            (unsigned, 1),
            (XRegisterTag, 0)
        );
        Op(Move, (XRegisterTag, 0), (YRegisterTag, 1));
        Op(Move, (YRegisterTag, 1), (XRegisterTag, 0));
        Extract(GetBinaryInteger, width: 64);
        if (labelBeforeReload)
        {
            Op(Label, (unsigned, 4));
        }

        if (reloaded)
        {
            Op(Move, (YRegisterTag, 1), (XRegisterTag, 0));
        }

        if (destinationTag == YRegisterTag && destination == 0 && live > 0 && !labelBeforeReload)
        {
            Extract(GetBinarySegment, width: 8);
            if (reloaded)
            {
                Op(Move, (YRegisterTag, 1), (XRegisterTag, 0));
            }
        }

        Op(Move, (YRegisterTag, 0), (XRegisterTag, 0));
        Op(Deallocate, (unsigned, 2));
        Op(Return);
        Op(Label, (unsigned, failure));
        Op(Move, (AtomTag, 1), (XRegisterTag, 0));
        Op(Deallocate, (unsigned, 2));
        Op(Return);
        Op(EndCode);

        return code.ToArray();

        void Extract(byte opcode, int width) => Op(
            opcode,
            (LabelTag, failure),
            (XRegisterTag, 0),
            (unsigned, live),
            (IntegerOperandTag, width),
            (unsigned, 1),
            (unsigned, 0),
            (destinationTag, destination)
        );

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
