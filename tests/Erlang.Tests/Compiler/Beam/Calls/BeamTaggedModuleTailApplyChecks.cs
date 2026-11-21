using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTaggedModuleTailApplyChecks
{
    internal const string TestName = "compiler/beam/allocates-only-for-guarded-module-tail-apply";
    private const string UnexpectedSchedule = "Expected the dynamic branch alone to own a zero-slot frame.";
    private const string UnexpectedValue = "Unexpected tagged module forwarding or fallback result.";
    private const string Source = """
        -module(tagged_module_tail_apply_fixture).
        -export([one/1, two/2, reordered/2]).

        one({dispatch, Module, _} = Value) when is_atom(Module) ->
            Module:tuple_to_list(Value);
        one(Value) ->
            maps:keys(Value).

        two(Value = {other, _, Module, _}, Map) when erlang:is_atom(Module) ->
            Module:map_get(Value, Map);
        two(Value, Map) ->
            maps:get(Value, Map).

        reordered({dispatch, Module, _} = Value, Index) when is_atom(Module) ->
            Module:element(Index, Value);
        reordered(Value, Index) ->
            maps:get(Value, Index).
        """;

    internal static async Task Run()
    {
        byte[] bytes = CodeGeneration.CompileBeamModule(Source, "tagged_module_tail_apply_fixture.erl");
        object[] instructions = ((IEnumerable)Property(Code(bytes), "Instructions")).Cast<object>().ToArray();
        int[] starts = Enumerable.Range(0, instructions.Length)
            .Where(index => Opcode(instructions[index]) == FunctionInfo).ToArray();
        if (starts.Length < 3)
        {
            throw new InvalidOperationException(UnexpectedSchedule);
        }

        for (int index = 0; index < 2; index++)
        {
            int arity = index + 1;
            object[] body = instructions[starts[index]..starts[index + 1]]
                .Where(instruction => Opcode(instruction) is not (FunctionInfo or Label or Line))
                .ToArray();
            byte[] opcodes = body.Select(Opcode).ToArray();
            if (!opcodes.SequenceEqual([
                IsTaggedTuple,
                GetTupleElement,
                IsAtom,
                Allocate,
                Move,
                TailDynamicApply,
                TailExternalCall
            ])
                || Operand(body[1], 2) != arity
                || Operand(body[3], 0) != 0 || Operand(body[3], 1) != arity + 1
                || Operand(body[5], 0) != arity || Operand(body[5], 1) != 0)
            {
                throw new InvalidOperationException(UnexpectedSchedule);
            }
        }

        var image = BeamModule.Prepare(bytes);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term one = Term.Tuple(Term.A("dispatch"), Term.A("erlang"), Term.I(7));
            Term two = Term.Tuple(
                Term.A("other"),
                Term.I(8),
                Term.A("erlang"),
                Term.I(9)
            );
            Term map = new MapTerm([new(two, Term.I(23))]);
            if (!Term.List(Term.A("dispatch"), Term.A("erlang"), Term.I(7)).Equals(
                    await image.Invoke(context, "one", one))
                || !Nil.Value.Equals(await image.Invoke(context, "one", new MapTerm([])))
                || !Term.I(23).Equals(await image.Invoke(
                    context,
                    "two",
                    two,
                    map
                ))
                || !Term.A("dispatch").Equals(await image.Invoke(
                    context,
                    "reordered",
                    one,
                    Term.I(1)
                )))
            {
                throw new InvalidOperationException(UnexpectedValue);
            }

            return Nil.Value;
        });
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Operand(object instruction, int index) =>
        (BigInteger)Property(((IEnumerable)Property(instruction, "Operands")).Cast<object>().ElementAt(index), "Value");
}
