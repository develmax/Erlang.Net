using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamDefaultOnlySequenceEncoderChecks
{
    internal const string TestName = "compiler/beam/stages-default-encoder-pairs-and-projects-by-lifetime";
    private const string Fixture = "beam_default_only_sequence_encoder_probe.erl";
    private const string IncorrectLayout = "The composed encoder projected unused fields or repeated a proven tuple check.";
    private const string IncorrectValue = "The encoder changed the emitted parts, sum or original argument error.";

    private const string Source = """
        -module(beam_default_only_sequence_encoder_probe).
        -export([encode/2]).
        encode(Value, Tags) ->
            {_, A, B, C} = Value,
            {P1, L1} = case omitted(A) of
                true -> {[], 0};
                false -> one(A, first)
            end,
            {P2, L2} = case omitted(B) of
                true -> {[], 0};
                false -> one(B, second)
            end,
            {P3, L3} = case omitted(C) of
                true -> {[], 0};
                false -> one(C, third)
            end,
            Parts = [P1, P2, P3],
            Total = L1 + L2 + L3,
            finish(Tags, Parts, Total).
        omitted(default) -> true;
        omitted(_) -> false.
        one(Value, Field) -> {[Field, Value], 2}.
        finish(Tags, Parts, Total) -> {Tags, Parts, Total}.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands").ToArray() is [_, var atom, var arity]
                && atoms[Value(atom)] == "encode" && Value(arity) == 2
        );
        if (start < 0)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object[] body = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        int[] labels = body.Where(instruction => Opcode(instruction) == Label)
            .Select(instruction => Value(Items(instruction, "Operands").Single())).ToArray();
        object[] allocations = body.Where(instruction => Opcode(instruction) == Allocate).ToArray();
        if (allocations is not [var allocation] || Items(allocation, "Operands").Select(Value).ToArray() is not [4, 2]
            || body.Count(instruction => Opcode(instruction) == TestArity) != 1
            || body.Count(instruction => Opcode(instruction) == GcBifTwo) != 2
            || !labels.SequenceEqual(labels.Order()))
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term root = Term.A("record");
            Term omit = Term.A("default");
            Term tags = Term.A("tags");
            Term a = Term.A("a");
            Term b = Term.A("b");
            Term c = Term.A("c");
            Term p1 = Term.List(Term.A("first"), a);
            Term p2 = Term.List(Term.A("second"), b);
            Term p3 = Term.List(Term.A("third"), c);
            if (!Term.Tuple(tags, Term.List(Nil.Value, Nil.Value, Nil.Value), Term.I(0)).Equals(
                    await module.Invoke(
                        context,
                        "encode",
                        Term.Tuple(
                            root,
                            omit,
                            omit,
                            omit
                        ),
                        tags
                    ))
                || !Term.Tuple(tags, Term.List(p1, Nil.Value, p3), Term.I(4)).Equals(
                    await module.Invoke(
                        context,
                        "encode",
                        Term.Tuple(
                            root,
                            a,
                            omit,
                            c
                        ),
                        tags
                    ))
                || !Term.Tuple(tags, Term.List(p1, p2, p3), Term.I(6)).Equals(
                    await module.Invoke(
                        context,
                        "encode",
                        Term.Tuple(
                            root,
                            a,
                            b,
                            c
                        ),
                        tags
                    )))
            {
                throw new InvalidOperationException(IncorrectValue);
            }

            try
            {
                _ = await module.Invoke(
                    context,
                    "encode",
                    root,
                    tags
                );
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.Tuple(Term.A("badmatch"), root)))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(IncorrectValue);
        });
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
