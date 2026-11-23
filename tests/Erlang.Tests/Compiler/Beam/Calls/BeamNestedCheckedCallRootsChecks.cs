using System.Reflection;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Tests;

internal static class BeamNestedCheckedCallRootsChecks
{
    internal const string TestName = "compiler/beam/keeps-raw-checked-call-payload-before-destructive-projections";

    private const int UnsignedOperandTag = 0;
    private const int IntegerOperandTag = 1;

    internal static Task Run()
    {
        var type = typeof(CodeGeneration).Assembly.GetType("Erlang.Compiler.BeamModuleCompiler", throwOnError: true)!;
        const BindingFlags members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        ConstructorInfo constructor = type.GetConstructors(members).Single();
        object?[] arguments = constructor.GetParameters()
            .Select(parameter => parameter.Position == 0 ? "nested_checked_roots" : parameter.DefaultValue)
            .ToArray();
        object compiler = constructor.Invoke(arguments);
        byte[] original = ReadFixture();
        byte[] expected = ReadFixture(saveBeforeChecks: true);
        if (Rewrite(original) is not byte[] rewritten || !rewritten.AsSpan().SequenceEqual(expected)
            || Rewrite(ReadFixture(usesSavedOnSuccess: true)) is not null
            || Rewrite(ReadFixture(usesScratchOnSuccess: true)) is not null
            || Rewrite(ReadFixture(extraColdPredecessor: true)) is not null)
        {
            throw new InvalidOperationException("Nested checks must retain the raw result only when both changed register paths are dead.");
        }

        return Task.CompletedTask;

        object? Rewrite(byte[] code) => BeamBinaryHostAccess.Call(
            compiler,
            "RetainNestedCheckedCallResult",
            (ReadOnlyMemory<byte>)code,
            5
        );
    }

    private static byte[] ReadFixture(
        bool saveBeforeChecks = false,
        bool usesSavedOnSuccess = false,
        bool usesScratchOnSuccess = false,
        bool extraColdPredecessor = false
    )
    {
        const int cold = 3;
        const int success = 4;
        const int scratch = 2;
        using var code = new MemoryStream();
        Instruction(Label, (UnsignedOperandTag, 1));
        Instruction(
            FunctionInfo,
            (AtomTag, 1),
            (AtomTag, 1),
            (UnsignedOperandTag, 1)
        );
        Instruction(Label, (UnsignedOperandTag, 2));
        Instruction(Allocate, (UnsignedOperandTag, 1), (UnsignedOperandTag, 1));
        if (extraColdPredecessor)
        {
            Instruction(
                ExactEqualTest,
                (LabelTag, cold),
                (XRegisterTag, 0),
                (IntegerOperandTag, 0)
            );
        }

        Instruction(ExternalCall, (UnsignedOperandTag, 1), (UnsignedOperandTag, 0));
        if (saveBeforeChecks)
        {
            Save();
        }

        int inner = saveBeforeChecks ? 0 : scratch;
        int payloadTag = saveBeforeChecks ? XRegisterTag : YRegisterTag;
        Tagged(register: 0, arity: 3);
        Projection(
            0,
            1,
            XRegisterTag,
            inner
        );
        Tagged(inner, arity: 2);
        Projection(
            inner,
            0,
            payloadTag,
            0
        );
        Instruction(
            ExactNotEqualTest,
            (LabelTag, cold),
            (payloadTag, 0),
            (IntegerOperandTag, 7)
        );
        Instruction(
            ExactEqualTest,
            (LabelTag, success),
            (payloadTag, 0),
            (IntegerOperandTag, 8)
        );
        Instruction(Label, (UnsignedOperandTag, cold));
        if (!saveBeforeChecks)
        {
            Save();
        }

        Instruction(TestHeap, (UnsignedOperandTag, 2), (UnsignedOperandTag, 0));
        Instruction(
            PutList,
            (YRegisterTag, 0),
            (AtomTag, 0),
            (XRegisterTag, 0)
        );
        Instruction(Deallocate, (UnsignedOperandTag, 1));
        Instruction(Return);
        Instruction(Label, (UnsignedOperandTag, success));
        if (usesSavedOnSuccess)
        {
            Instruction(Move, (YRegisterTag, 0), (XRegisterTag, 0));
        }
        else if (usesScratchOnSuccess)
        {
            Instruction(Move, (XRegisterTag, scratch), (XRegisterTag, 0));
        }
        else
        {
            Instruction(Move, (AtomTag, 1), (XRegisterTag, 0));
        }

        Instruction(Deallocate, (UnsignedOperandTag, 1));
        Instruction(Return);
        Instruction(EndCode);

        return code.ToArray();

        void Save() => Instruction(Move, (XRegisterTag, 0), (YRegisterTag, 0));

        void Tagged(int register, int arity) => Instruction(
            IsTaggedTuple,
            (LabelTag, cold),
            (XRegisterTag, register),
            (UnsignedOperandTag, arity),
            (AtomTag, 1)
        );

        void Projection(
            int source,
            int field,
            int targetTag,
            int destination
        ) => Instruction(
            GetTupleElement,
            (XRegisterTag, source),
            (UnsignedOperandTag, field),
            (targetTag, destination)
        );

        void Instruction(byte opcode, params (int Tag, int Value)[] operands)
        {
            code.WriteByte(opcode);
            foreach (var (tag, value) in operands)
            {
                BeamCompactWriter.Write(code, tag, value);
            }
        }
    }
}
