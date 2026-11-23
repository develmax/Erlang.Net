using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamOrderedPairTakeChecks
{
    internal const string TestName = "compiler/beam/keeps-ordered-take-pair-in-its-accumulator";
    private const string Fixture = "beam_ordered_pair_take.erl";
    private const string BadLayout = "Ordered take framed the recursive accumulator or repeated the pair checks.";
    private const string BadValue = "Ordered take changed a preserved pair, suffix or failed match.";

    private const string Source = """
        -module(beam_ordered_pair_take).
        -export([take/3, replaced/3]).

        take(Key, [{K,_}|_], _) when Key < K -> missing;
        take(Key, [{K,_}=Pair|Rest], Acc) when Key > K ->
            take(Key, Rest, [Pair|Acc]);
        take(_, [{_,Value}|Rest], Acc) -> {Value,combine(Acc,Rest)};
        take(_, [], _) -> missing.

        replaced(Key, [{K,_}|_], _) when Key < K -> missing;
        replaced(Key, [{K,_}|Rest], Acc) when Key > K ->
            replaced(Key, Rest, [{K,marker}|Acc]);
        replaced(_, [{_,Value}|Rest], Acc) -> {Value,combine(Acc,Rest)};
        replaced(_, [], _) -> missing.

        combine([], Rest) -> Rest;
        combine([Item|Tail], Rest) -> combine(Tail, [Item|Rest]).
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        object code = Code(image);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands") is [_, var name, var arity]
                && atoms[Value(name)] == "take" && Value(arity) == 3
        );
        if (start < 0)
        {
            throw new InvalidOperationException(BadLayout);
        }

        object[] body = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        int allocation = Array.FindIndex(body, instruction => Opcode(instruction) == Allocate);
        int recurrence = Array.FindIndex(body, instruction => Opcode(instruction) == TailLocalCall);
        int comparison = Array.FindIndex(body, instruction => Opcode(instruction) == IsGreaterOrEqual);
        if (recurrence < 0 || allocation <= recurrence || comparison < 2
            || !IsProjection(body[comparison - 2], field: 0, destination: 1)
            || !IsProjection(body[comparison - 1], field: 1, destination: 5)
            || body.Count(instruction => Opcode(instruction) == GetList) != 1
            || body.Count(instruction => Opcode(instruction) == IsTuple) != 1
            || Items(body[allocation], "Operands") is not [var frame, var live]
            || Value(frame) != 1 || Value(live) != 6)
        {
            throw new InvalidOperationException(BadLayout);
        }

        BeamModule module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term one = Term.I(1);
            Term two = Term.I(2);
            Term zero = Term.I(0);
            Term val = Term.A("value");
            Term old = Term.A("old");
            Term improper = Term.A("improper");
            Term first = Term.Tuple(one, old);
            Term target = Term.Tuple(new FloatTerm(2.0), val);
            Term last = Term.Tuple(Term.I(3), old);
            Term rows = Term.List(first, target, last);

            await Check(
                "take",
                two,
                rows,
                Term.Tuple(val, Term.List(first, last))
            );
            await Check(
                "take",
                zero,
                rows,
                Term.A("missing")
            );
            await Check(
                "take",
                two,
                Nil.Value,
                Term.A("missing")
            );
            await Check(
                "take",
                two,
                new Cons(target, improper),
                Term.Tuple(val, improper)
            );
            await Check(
                "replaced",
                two,
                rows,
                Term.Tuple(val, Term.List(Term.Tuple(one, Term.A("marker")), last))
            );
            await Reject(Term.List(improper));
            await Reject(new Cons(first, improper));

            return Nil.Value;

            async Task Check(
                string function,
                Term key,
                Term input,
                Term expected
            )
            {
                if (!expected.Equals(await module.Invoke(
                    context,
                    function,
                    key,
                    input,
                    Nil.Value
                )))
                {
                    throw new InvalidOperationException(BadValue);
                }
            }

            async Task Reject(Term input)
            {
                try
                {
                    await module.Invoke(
                        context,
                        "take",
                        two,
                        input,
                        Nil.Value
                    );
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.FunctionClause)))
                {
                    return;
                }

                throw new InvalidOperationException(BadValue);
            }
        });
    }

    private static bool IsProjection(object instruction, int field, int destination) =>
        Opcode(instruction) == GetTupleElement
        && Items(instruction, "Operands") is [var source, var index, var target]
        && Value(source) == 3 && Value(index) == field && Value(target) == destination;

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
