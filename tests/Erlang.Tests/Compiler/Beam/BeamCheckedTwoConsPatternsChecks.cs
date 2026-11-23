using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCheckedTwoConsPatternsChecks
{
    internal const string TestName = "compiler/beam/checks-a-two-cons-literal-before-repeated-fields";
    private const string WrongSchedule = "The checked two-cons pattern changed its projection order or fallback owners.";
    private const string WrongResult = "The checked two-cons pattern changed a match or fallback result.";
    private const string Source = """
        -module(beam_checked_two_cons_patterns).
        -export([rewrite/1]).
        rewrite([{lead,N},{token,N,7} | Rest]) ->
            [{merged,N} | rewrite(Rest)];
        rewrite([Head | Rest]) ->
            [Head | rewrite(Rest)];
        rewrite([]) ->
            [].
        """;

    internal static async Task Run()
    {
        byte[] bytes = CodeGeneration.CompileBeamModule(Source, "beam_checked_two_cons_patterns.erl");
        object code = Code(bytes);
        object[] operations = Items(code, "Instructions")
            .Where(instruction => Opcode(instruction) is not (Line or Label or FunctionInfo)).ToArray();
        int frame = Array.FindIndex(operations, instruction => Opcode(instruction) == Allocate);
        if (frame != 11 || Value(Operands(operations[frame])[1]) != 5
            || operations.Take(frame).Select(Opcode).ToArray() is not
            [
                IsNonemptyList, GetList, IsTaggedTuple, IsNonemptyList, GetList,
                IsTaggedTuple, GetTupleElement, ExactEqualTest, GetTupleElement,
                GetTupleElement, ExactEqualTest
            ])
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        object[] marker = Operands(operations[6]);
        object[] repeated = Operands(operations[8]);
        object[] original = Operands(operations[9]);
        if (Value(marker[0]) != 2 || Value(marker[1]) != 2 || Value(marker[2]) != 4
            || Value(repeated[0]) != 2 || Value(repeated[1]) != 1 || Value(repeated[2]) != 2
            || Value(original[0]) != 1 || Value(original[1]) != 1 || Value(original[2]) != 4)
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        var image = BeamModule.Prepare(bytes);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term head = Term.Tuple(Term.A("lead"), Term.I(3));
            Term matching = Term.Tuple(Term.A("token"), Term.I(3), Term.I(7));
            Term mismatched = Term.Tuple(Term.A("token"), new FloatTerm(3.0), Term.I(7));
            Term wrongLiteral = Term.Tuple(Term.A("token"), Term.I(3), Term.I(8));
            Term tooShort = Term.Tuple(Term.A("token"), Term.I(3));
            Term tail = Term.A("rest");
            Term positive = Term.List(head, matching, tail);
            Term expected = Term.List(Term.Tuple(Term.A("merged"), Term.I(3)), tail);
            if (!expected.Equals(await image.Invoke(context, "rewrite", positive)))
            {
                throw new InvalidOperationException(WrongResult);
            }

            foreach (Term rejected in new[] { Term.List(head), Term.List(head, mismatched),
                Term.List(head, wrongLiteral), Term.List(head, tooShort), Nil.Value })
            {
                if (!rejected.Equals(await image.Invoke(context, "rewrite", rejected)))
                {
                    throw new InvalidOperationException(WrongResult);
                }
            }

            return Nil.Value;
        });
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
