using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamPairedTupleBitwiseChecks
{
    internal const string TestName = "compiler/beam/sinks-paired-tuple-fields-to-bitwise-operations";
    private const string WrongLayout = "The paired bitwise projections do not provide the expected live register prefix.";
    private const string WrongValue = "Paired bitwise fields changed an output or exception.";
    private const string Source = """
        -module(beam_paired_tuple_bitwise).
        -export([pair/2, permuted/2]).
        pair({A,B,C,D}, {E,F,G,H}) ->
            {A band E,
             B band F,
             C band G,
             D band H}.
        permuted({A,B}, {C,D}) -> {A band D, B band C}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, "beam_paired_tuple_bitwise.erl");
        object code = Code(image);
        object[] instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        string[] atoms = ((IEnumerable)Property(Property(code, "Structure"), "Atoms")).Cast<string>().ToArray();
        int info = Array.FindIndex(instructions, instruction =>
            Opcode(instruction) == FunctionInfo && atoms[Value(Operands(instruction)[1])] == "pair");
        if (info < 0)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        object[] body = instructions.Skip(info + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        byte[] prefix = body.Select(Opcode).Where(opcode => opcode != Line).ToArray();
        if (prefix is not [
                Label, IsTuple, TestArity, IsTuple, TestArity,
                GetTupleElement, GetTupleElement, GcBifTwo,
                GetTupleElement, GetTupleElement, GcBifTwo,
                GetTupleElement, GetTupleElement, GcBifTwo,
                GetTupleElement, GetTupleElement, GcBifTwo, TestHeap, PutTuple, Return,
                ..])
        {
            throw new InvalidOperationException(WrongLayout);
        }

        int[] live = body.Where(instruction => Opcode(instruction) == GcBifTwo)
            .Select(instruction => Value(Operands(instruction)[1])).ToArray();
        if (!live.SequenceEqual([4, 5, 6, 5]))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            var left = Term.Tuple(
                Term.I(15),
                Term.I(6),
                Term.I(10),
                Term.I(12)
            );
            var right = Term.Tuple(
                Term.I(6),
                Term.I(9),
                Term.I(5),
                Term.I(3)
            );
            var expected = Term.Tuple(
                Term.I(6),
                Term.I(0),
                Term.I(0),
                Term.I(0)
            );
            if (!expected.Equals(await module.Invoke(
                context,
                "pair",
                left,
                right
            )) || !Term.Tuple(Term.I(9), Term.I(6)).Equals(await module.Invoke(
                context,
                "permuted",
                Term.Tuple(Term.I(15), Term.I(6)),
                Term.Tuple(Term.I(6), Term.I(9))
            )))
            {
                throw new InvalidOperationException(WrongValue);
            }

            var invalid = Term.Tuple(
                Term.A("bad"),
                Term.I(6),
                Term.I(10),
                Term.I(12)
            );
            await AssertError(invalid, Term.I(0), ErlangErrorReasons.FunctionClause);
            await AssertError(invalid, right, ErlangErrorReasons.BadArithmetic);

            return Nil.Value;

            async Task AssertError(Term first, Term second, string reason)
            {
                try
                {
                    _ = await module.Invoke(
                        context,
                        "pair",
                        first,
                        second
                    );
                }
                catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                    && error.Reason.Equals(Term.A(reason)))
                {
                    return;
                }

                throw new InvalidOperationException(WrongValue);
            }
        });
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) =>
        ((IEnumerable)Property(instruction, "Operands")).Cast<object>().ToArray();
}
