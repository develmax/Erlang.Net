using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamBoundedListPredicateChecks
{
    internal const string TestName = "compiler/beam/reuses-tail-register-for-bounded-list-predicate";
    private const string Fixture = "beam_bounded_list_predicate_probe.erl";
    private const string WrongLayout = "The bounded predicate must retain the list tail and carry the lower integer bound into the upper comparison.";
    private const string WrongValue = "The bounded predicate changed its list or numeric guard result.";

    private const string Source = """
        -module(beam_bounded_list_predicate_probe).
        -export([within/1]).
        within([]) -> true;
        within([Head|Tail]) when is_integer(Head), Head >= 2, Head =< 9 -> within(Tail);
        within(_) -> false.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        CheckLayout(image);
        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term yes = Term.A(ErlangBooleanAtoms.True);
            Term no = Term.A(ErlangBooleanAtoms.False);
            Term lo = Term.I(2);
            Term hi = Term.I(9);
            Term[] accepted = [Nil.Value, Term.List(lo, hi, lo)];
            Term[] rejected =
            [
                Term.A("bad"),
                Term.List(Term.I(1)),
                Term.List(Term.I(10)),
                Term.List(new FloatTerm(2.0)),
                new Cons(lo, hi),
                Term.List(lo, Term.List(hi))
            ];
            foreach (Term input in accepted)
            {
                if (!yes.Equals(await module.Invoke(context, "within", input)))
                {
                    throw new InvalidOperationException(WrongValue);
                }
            }

            foreach (Term input in rejected)
            {
                if (!no.Equals(await module.Invoke(context, "within", input)))
                {
                    throw new InvalidOperationException(WrongValue);
                }
            }

            return Nil.Value;
        });
    }

    private static void CheckLayout(byte[] image)
    {
        object code = Code(image);
        object[] instructions = Items(code, "Instructions").ToArray();
        object[] body = instructions.SkipWhile(instruction => Opcode(instruction) != FunctionInfo).Skip(1)
            .TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        int cons = Array.FindIndex(body, instruction => Opcode(instruction) == GetList);
        object[] comparisons = body.Where(instruction => Opcode(instruction) == IsGreaterOrEqual).ToArray();
        if (cons < 0 || Operands(body[cons]) is not [var source, var head, var tail]
            || !IsX(source, 0) || !IsX(head, 1) || !IsX(tail, 0)
            || comparisons is not [var lower, var upper]
            || Operands(lower) is not [var lowerFailure, var lowerTyped, var lowerLimit]
            || Operands(upper) is not [var upperFailure, var upperLimit, var upperTyped]
            || Value(lowerFailure) != Value(upperFailure)
            || !IsX(lowerTyped, 1) || !IsX(upperTyped, 1)
            || Property(lowerTyped, "TypeIndex") is not int lowerType
            || Property(upperTyped, "TypeIndex") is not int upperType || lowerType == upperType
            || Value(lowerLimit) != 2 || Value(upperLimit) != 9
            || body.Any(instruction => Opcode(instruction) is Move && Operands(instruction) is [var from, var to]
                && IsX(from, 2) && IsX(to, 0)))
        {
            throw new InvalidOperationException(WrongLayout);
        }
    }

    private static bool IsX(object operand, int register) => Property(operand, "Kind") is "x" && Value(operand) == register;

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
