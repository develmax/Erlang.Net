using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCounteredTupleConsAccumulatorChecks
{
    internal const string TestName = "compiler/beam/retains-a-checked-cons-head-through-field-conversion";
    private const string Module = "beam_countered_tuple_cons";
    private const string WrongPlan = "Expected early checked counting and one retained tuple across the field conversion.";
    private const string Source = """
        -module(beam_countered_tuple_cons).
        -export([collect/2, open/4]).
        collect(Tag, Input) -> collect(Tag, Input, 2, []).
        collect(_, [], _, Acc) -> Acc;
        collect(Tag, [{Key, _Prefix, Value, _} | Tail], Count, Acc) ->
            collect(Tag, Tail, Count+2, [{list_to_atom(Key), Value} | Acc]).
        open(_, [], _, Acc) -> Acc;
        open(Tag, [{Key, _Prefix, Value, _} | Tail], Count, Acc) ->
            open(Tag, Tail, Count+2, [{list_to_atom(Key), Value} | Acc]).
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Module + ".erl");
        object code = Code(image);
        if (!HasRetainedHead(code, "collect") || HasRetainedHead(code, "open"))
        {
            throw new InvalidOperationException(WrongPlan);
        }

        BeamModule module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term ignored = Term.A("ignored");
            Term one = Term.I(1);
            Term two = Term.I(2);
            Term a = Term.A("a");
            Term b = Term.A("b");
            Term first = Term.Tuple(
                Term.List(Term.I('a')),
                ignored,
                one,
                ignored
            );
            Term second = Term.Tuple(
                Term.List(Term.I('b')),
                ignored,
                two,
                ignored
            );
            Term expected = Term.List(Term.Tuple(b, two), Term.Tuple(a, one));
            if (!expected.Equals(await module.Invoke(
                context,
                "collect",
                ignored,
                Term.List(first, second)
            ))
                || !Nil.Value.Equals(await module.Invoke(
                    context,
                    "collect",
                    ignored,
                    Nil.Value
                )))
            {
                throw new InvalidOperationException(WrongPlan);
            }

            await RequireFailure(Term.List(Term.Tuple(a, one)), "function_clause");
            await RequireFailure(new Cons(first, b), "function_clause");
            await RequireFailure(Term.List(Term.Tuple(
                a,
                ignored,
                one,
                ignored
            )), "badarg");

            return Nil.Value;

            async Task RequireFailure(Term input, string reason)
            {
                try
                {
                    await module.Invoke(
                        context,
                        "collect",
                        ignored,
                        input
                    );
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.A(reason)))
                {
                    return;
                }

                throw new InvalidOperationException(WrongPlan);
            }
        });
    }

    private static bool HasRetainedHead(object code, string name)
    {
        object function = Items(code, "Functions").Single(item =>
            (string)Property(item, "Name") == name && (int)Property(item, "Arity") == 4);
        int start = (int)Property(function, "FirstInstructionIndex");
        int end = (int)Property(function, "EndInstructionIndex");
        object[] body = Items(code, "Instructions").Skip(start).Take(end - start).ToArray();
        int sum = Array.FindIndex(body, instruction => Opcode(instruction) == GcBifTwo);
        int allocation = Array.FindIndex(body, instruction => Opcode(instruction) == Allocate);
        int call = Array.FindIndex(body, instruction => Opcode(instruction) == ExternalCall);
        int heap = Array.FindIndex(body, instruction => Opcode(instruction) == TestHeap);

        return sum >= 0 && allocation > sum && call > allocation && heap == call + 1
            && Items(body[allocation], "Operands").Select(Value).ToArray() is [5, 6]
            && Items(body[heap], "Operands").Select(Value).ToArray() is [5, 1]
            && Opcode(body[heap + 1]) == GetTupleElement
            && Items(body[heap + 1], "Operands").ToArray() is [var tuple, var field, _]
            && (string)Property(tuple, "Kind") == "y" && Value(tuple) == 0 && Value(field) == 2;
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string property) => ((IEnumerable)Property(owner, property)).Cast<object>();
}
