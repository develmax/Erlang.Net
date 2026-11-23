using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamOrderedTupleConsLookupChecks
{
    internal const string TestName = "compiler/beam/shares-checked-pair-head-through-ordered-lookup";
    private const string Fixture = "beam_ordered_tuple_cons_lookup.erl";
    private const string BadLayout = "Ordered pair lookup did not share its checked entry or maintain its heap roots.";
    private const string BadValue = "Ordered pair lookup changed numeric equality, rejection, or a repeated binding.";

    private const string Source = """
        -module(beam_ordered_tuple_cons_lookup).
        -export([present/2, find/2, fetch/2, repeated/2]).

        present(Key, [{K,_}|_]) when Key < K -> false;
        present(Key, [{K,_}|Tail]) when Key > K -> present(Key, Tail);
        present(_Key, [{_K,_Value}|_]) -> true;
        present(_, []) -> false.

        find(Key, [{K,_}|_]) when Key < K -> missing;
        find(Key, [{K,_}|Tail]) when Key > K -> find(Key, Tail);
        find(_Key, [{_K,Value}|_]) -> {found,Value};
        find(_, []) -> missing.

        fetch(Key, [{K,_}|Tail]) when Key > K -> fetch(Key, Tail);
        fetch(Key, [{K,Value}|_]) when Key == K -> Value.

        repeated(Key, [{K,K}|_]) when Key < K -> missing;
        repeated(Key, [{K,_}|Tail]) when Key > K -> repeated(Key, Tail);
        repeated(_Key, [{_K,Value}|_]) -> {found,Value};
        repeated(_, []) -> missing.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        object code = Code(image);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        foreach (string function in new[] { "present", "find", "fetch" })
        {
            object[] body = ReadBody(function);
            if (body.Count(instruction => Opcode(instruction) == GetList) != 1
                || body.Count(instruction => Opcode(instruction) == IsTuple) != 1
                || body.Count(instruction => Opcode(instruction) == TestArity) != 1
                || body.Any(instruction => Opcode(instruction) is Allocate or AllocateHeap))
            {
                throw new InvalidOperationException(BadLayout);
            }
        }

        object heap = ReadBody("find").Single(instruction => Opcode(instruction) == TestHeap);
        if (Items(heap, "Operands") is not [var words, var live] || Value(words) != 3 || Value(live) != 3)
        {
            throw new InvalidOperationException(BadLayout);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term first = Term.Tuple(Term.I(1), Term.A("first"));
            Term value = Term.A("second");
            Term second = Term.Tuple(new FloatTerm(2.0), value);
            Term rows = Term.List(first, second);
            Term wrapped = Term.Tuple(Term.A("found"), value);
            await Check(
                "present",
                Term.I(2),
                rows,
                Term.A("true")
            );
            await Check(
                "present",
                Term.I(0),
                rows,
                Term.A("false")
            );
            await Check(
                "find",
                Term.I(2),
                rows,
                wrapped
            );
            await Check(
                "find",
                Term.I(3),
                rows,
                Term.A("missing")
            );
            await Check(
                "fetch",
                Term.I(2),
                rows,
                value
            );
            await Check(
                "repeated",
                Term.I(0),
                Term.List(second),
                wrapped
            );
            await Check(
                "find",
                Term.I(1),
                new Cons(first, Term.A("improper")),
                Term.Tuple(Term.A("found"), Term.A("first"))
            );
            await Reject("fetch", Term.I(0), rows);
            await Reject("find", Term.I(0), Term.List(Term.A("malformed")));
            await Reject("present", Term.I(3), new Cons(first, Term.A("improper")));

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
                    input
                )))
                {
                    throw new InvalidOperationException(BadValue);
                }
            }

            async Task Reject(string function, Term key, Term input)
            {
                try
                {
                    await module.Invoke(
                        context,
                        function,
                        key,
                        input
                    );
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.FunctionClause)))
                {
                    return;
                }

                throw new InvalidOperationException(BadValue);
            }
        });

        object[] ReadBody(string function)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && Items(instruction, "Operands") is [_, var name, var arity]
                    && atoms[Value(name)] == function && Value(arity) == 2
            );
            if (start < 0)
            {
                throw new InvalidOperationException(BadLayout);
            }

            return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
