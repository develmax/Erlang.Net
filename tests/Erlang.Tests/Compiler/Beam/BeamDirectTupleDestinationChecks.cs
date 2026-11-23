using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamDirectTupleDestinationChecks
{
    internal const string TestName = "compiler/beam/builds-tuple-in-proven-copy-destination";
    private const string Fixture = "beam_tuple_destination_probe.erl";
    private const string IncorrectDestination = "The tuple was not constructed directly in the proven argument register.";
    private const string IncorrectLiveCopy = "The tuple copy was removed even though both registers are live.";
    private const string IncorrectValue = "Tuple placement changed a result or its guard failure.";

    internal const string Source = """
        -module(beam_tuple_destination_probe).
        -export([guarded/3, negative/1]).

        guarded(Value, Options, Name) when is_list(Value), is_list(Options), is_atom(Name) ->
            deliver(Name,{tag,Value,Options}).
        negative(V) -> T={tag,V}, deliver(T,T).
        deliver(Name,Request) -> {Name,Request}.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        CheckConsBeforeIndependentTuple();
        byte[] image = CompileFixture();
        object code = Code(image);
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] positive = Body("guarded");
        int construction = Array.FindIndex(positive, instruction => Opcode(instruction) == PutTuple);
        if (construction < 0 || Value(Operands(positive[construction])[0]) != 1
            || Opcode(positive[construction + 1]) != Move
            || Value(Operands(positive[construction + 1])[0]) != 2)
        {
            throw new InvalidOperationException(IncorrectDestination);
        }

        object[] negative = Body("negative");
        construction = Array.FindIndex(negative, instruction => Opcode(instruction) == PutTuple);
        if (construction < 0 || Value(Operands(negative[construction])[0]) != 0
            || Opcode(negative[construction + 1]) != Move
            || !Operands(negative[construction + 1]).Select(Value).SequenceEqual([0, 1]))
        {
            throw new InvalidOperationException(IncorrectLiveCopy);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term name = Term.A("request");
            Term value = Term.List(Term.A("value"));
            Term options = Term.List(Term.A("option"));
            Term tag = Term.A("tag");
            Term request = Term.Tuple(tag, value, options);
            Term shared = Term.Tuple(tag, Term.I(42));
            if (!Term.Tuple(name, request).Equals(await module.Invoke(
                context,
                "guarded",
                value,
                options,
                name
            )) || !Term.Tuple(shared, shared).Equals(await module.Invoke(context, "negative", Term.I(42))))
            {
                throw new InvalidOperationException(IncorrectValue);
            }

            try
            {
                _ = await module.Invoke(
                    context,
                    "guarded",
                    Term.I(1),
                    options,
                    name
                );
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.A(ErlangErrorReasons.FunctionClause)))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(IncorrectValue);
        });

        object[] Body(string name)
        {
            int info = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && Operands(instruction) is [_, var atom, _] && atoms[Value(atom)] == name
            );

            return instructions.Skip(info + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static void CheckConsBeforeIndependentTuple()
    {
        byte[] positive = ConsFixture();
        if (Rewrite(positive) is not { } result || !result.AsSpan().SequenceEqual(ConsFixture(rewritten: true))
            || Rewrite(ConsFixture(readsOldCons: true)) is not null
            || Rewrite(ConsFixture(replacesOtherRegister: true)) is not null)
        {
            throw new InvalidOperationException(IncorrectDestination);
        }

        static byte[]? Rewrite(byte[] bytes)
        {
            ReadOnlyMemory<byte> code = bytes;
            object header = New(
                "BeamCodeHeader",
                0,
                MinimumHighestOpcode,
                3,
                1,
                code
            );
            object body = Call(
                Type("BeamInstructionReader"),
                "Read",
                header,
                3,
                0,
                null,
                null
            );
            System.Type edit = typeof(BeamModuleCompiler).GetNestedType("CodeEdit", System.Reflection.BindingFlags.NonPublic)!;
            object edits = Activator.CreateInstance(typeof(List<>).MakeGenericType(edit))!;
            _ = Call(
                typeof(BeamModuleCompiler),
                "CollectDirectConstructionDestinationEdits",
                code,
                body,
                edits
            );

            return (byte[]?)Call(
                typeof(BeamModuleCompiler),
                "ApplyDuplicatedFrameReadEdits",
                code,
                edits
            );
        }
    }

    private static byte[] ConsFixture(
        bool rewritten = false,
        bool readsOldCons = false,
        bool replacesOtherRegister = false
    )
    {
        byte[] save = rewritten ? [] : [Move, X(0), X(3)];
        byte[] tupleInput = readsOldCons ? [ExtendedTypedRegisterOperand, X(0), U(0)] : [A(2)];

        return
        [
            .. Start(
                1,
                2,
                1,
                3
            ),
            TestHeap, U(5), U(3), PutList, X(1), X(2), rewritten ? X(3) : X(0),
            .. save,
            PutTuple, replacesOtherRegister ? X(4) : X(0), ExtendedListOperand, U(2), X(3), .. tupleInput,
            Return, EndCode
        ];
    }

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
