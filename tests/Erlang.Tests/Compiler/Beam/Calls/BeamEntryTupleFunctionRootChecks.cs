using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamEntryTupleFunctionRootChecks
{
    internal const string TestName = "compiler/beam/keeps-entry-fun-fields-out-of-the-surviving-frame";
    private const string WrongLayout = "Expected the checked tuple fields to be projected directly into the call registers.";
    private const string WrongValue = "The tuple invocation or its failure produced an unexpected result.";
    private const string Source = """
        -module(entry_tuple_fun_roots).
        -export([lookup/4, check/5, unary/0, zero/0]).

        lookup({Fun, Argument}, Table, Default, Seed) ->
            Key = Fun(Argument),
            lookup(Key, Table, Default, Seed);
        lookup(Key, Table, Default, Seed) when is_atom(Key) ->
            Prior = retain(Seed),
            Value = maps:get(Key, Table, Default),
            _ = restore(Prior),
            Value.

        check({Fun, Argument}, Table, Inner, Outer, Seed) ->
            Key = Fun(Argument),
            check(Key, Table, Inner, Outer, Seed);
        check(Key, Table, Inner, Outer, Seed) when is_atom(Key) ->
            Prior = retain(Seed),
            true = member(Key, Table, Inner, Outer),
            _ = restore(Prior),
            ok.

        member(Key, Table, Inner, Outer) -> lists:member(Key, [Inner, Outer | Table]).
        retain(Value) -> Value.
        restore(Value) -> put(previous, Value).
        unary() -> fun(Value) -> Value end.
        zero() -> fun() -> unexpected end.
        """;

    internal static async Task Run()
    {
        byte[] bytes = CodeGeneration.CompileBeamModule(Source, "entry_tuple_fun_roots.erl");
        object[] instructions = ((IEnumerable)Property(Code(bytes), "Instructions")).Cast<object>().ToArray();
        int[] entries = Enumerable.Range(0, instructions.Length)
            .Where(index => Opcode(instructions[index]) == FunctionInfo).ToArray();
        if (entries.Length < 3)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        CheckLayout(instructions[entries[0]..entries[1]], 4);
        CheckLayout(instructions[entries[1]..entries[2]], 5);

        var image = BeamModule.Prepare(bytes, new BeamDependencySelection(["maps"]));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term key = Term.A("key");
            Term fallback = Term.A("fallback");
            Term seed = Term.A("seed");
            Term value = Term.A("value");
            Term unary = await image.Invoke(context, "unary");
            Term zero = await image.Invoke(context, "zero");
            Term map = new MapTerm([new(key, value)]);
            if (!value.Equals(
                await image.Invoke(
                    context,
                    "lookup",
                    Term.Tuple(unary, key),
                    map,
                    fallback,
                    seed
                ))
                || !Term.A("ok").Equals(
                await image.Invoke(
                    context,
                    "check",
                    Term.Tuple(unary, key),
                    Nil.Value,
                    key,
                    fallback,
                    seed
                )))
            {
                throw new InvalidOperationException(WrongValue);
            }

            Term invalid = Term.A("not_a_fun");
            try
            {
                await image.Invoke(
                    context,
                    "lookup",
                    Term.Tuple(invalid, key),
                    map,
                    fallback,
                    seed
                );
                throw new InvalidOperationException(WrongValue);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A("badfun"), invalid)))
            {
            }

            try
            {
                await image.Invoke(
                    context,
                    "lookup",
                    Term.Tuple(zero, key),
                    map,
                    fallback,
                    seed
                );
                throw new InvalidOperationException(WrongValue);
            }
            catch (ErlangException error) when (error.Reason.Equals(
                Term.Tuple(Term.A(ErlangErrorReasons.BadArity), Term.Tuple(zero, Term.List(key)))))
            {
            }

            return Nil.Value;
        });
    }

    private static void CheckLayout(object[] body, int arity)
    {
        int allocate = Array.FindIndex(body, instruction => Opcode(instruction) == Allocate);
        int call = Array.FindIndex(body, instruction => Opcode(instruction) == CallFun);
        int tail = Array.FindIndex(body, instruction => Opcode(instruction) == TailLocalCallWithFrame);
        if (allocate < 0 || call <= allocate || tail <= call
            || Operand(body[allocate], 0) != arity - 1 || Operand(body[allocate], 1) != arity
            || Operand(body[tail], 2) != arity - 1)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        object[] projections = body[allocate..call].Where(instruction => Opcode(instruction) == GetTupleElement).ToArray();
        if (projections.Length != 2 || Operand(projections[0], 1) != 0 || Operand(projections[0], 2) != 1
            || Operand(projections[1], 1) != 1 || Operand(projections[1], 2) != 0)
        {
            throw new InvalidOperationException(WrongLayout);
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Operand(object instruction, int index) =>
        (BigInteger)Property(((IEnumerable)Property(instruction, "Operands")).Cast<object>().ElementAt(index), "Value");
}
