using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamCallConstructionProbe;

namespace Erlang.Tests;

internal static class BeamConsHeadTupleGroupChecks
{
    internal const string TestName = "compiler/beam/partitions-distinct-literal-cons-head-tags-before-owned-frames";
    private const string WrongDispatch = "Expected one checked cons and shared arity and tag decisions before the matched branch frame.";
    private const string WrongBehavior = "The tuple-head decision changed a nested match, guard, source priority, or rejected value.";
    private const string Source = """
        -module(cons_head_tuple_group).
        -export([step/2, one_arity/2, repeated/2]).

        step([{alpha, {value,N}} | Tail], Key) when is_integer(N) -> {erlang:put(Key,N), Tail};
        step([{beta=Tag,N} | Tail], _) -> {Tag,N,Tail};
        step([{long,N,M} | Tail], _) -> {N,M,Tail};
        step([Head | Tail], _) -> {fallback,Head,Tail};
        step([], _) -> empty.

        one_arity([{alpha,N} | Tail], Key) -> {erlang:put(Key,N), Tail};
        one_arity([{beta,N} | Tail], _) -> {beta,N,Tail};
        one_arity([Head | Tail], _) -> {fallback,Head,Tail};
        one_arity([], _) -> empty.

        repeated([{alpha,{value,N}} | Tail], _) when is_integer(N) -> {checked,N,Tail};
        repeated([{alpha,Value} | Tail], _) -> {second,Value,Tail};
        repeated([{beta,Value} | Tail], _) -> {beta,Value,Tail};
        repeated([Head | Tail], _) -> {fallback,Head,Tail};
        repeated([], _) -> empty.
        """;

    internal static async Task Run()
    {
        byte[] binary = CodeGeneration.CompileBeamModule(Source, "cons_head_tuple_group.erl");
        var code = new BeamCallConstructionProbe(binary);
        CheckDispatch(code.Body("step"), mixedArity: true);
        CheckDispatch(code.Body("one_arity"), mixedArity: false);
        if (code.Body("repeated").Any(instruction => Opcode(instruction) == SelectValue))
        {
            throw new InvalidOperationException(WrongDispatch);
        }

        var image = BeamModule.Prepare(binary);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Atom tail = Term.A("improper_tail");
            Atom key = Term.A("cons_head_tuple_group_key");
            Term checkedValue = Term.Tuple(Term.A("value"), Term.I(5));
            Term guardFailure = Term.Tuple(Term.A("value"), Term.A("not_integer"));
            Term badGuardHead = Term.Tuple(Term.A("alpha"), guardFailure);
            Term wrongNestedHead = Term.Tuple(Term.A("alpha"), Term.Tuple(Term.A("other"), Term.I(5)));
            Term matchingHead = Term.Tuple(Term.A("alpha"), checkedValue);
            Term betaHead = Term.Tuple(Term.A("beta"), Term.I(7));
            Term longHead = Term.Tuple(Term.A("long"), Term.I(4), Term.I(6));

            await Expect("step", new Cons(matchingHead, tail), Term.Tuple(Term.A("undefined"), tail));
            await Expect("step", new Cons(badGuardHead, tail), Term.Tuple(Term.A("fallback"), badGuardHead, tail));
            await Expect("step", new Cons(wrongNestedHead, tail), Term.Tuple(Term.A("fallback"), wrongNestedHead, tail));
            await Expect("step", new Cons(betaHead, tail), Term.Tuple(Term.A("beta"), Term.I(7), tail));
            await Expect("step", new Cons(longHead, tail), Term.Tuple(Term.I(4), Term.I(6), tail));
            await Expect("step", Nil.Value, Term.A("empty"));
            await Expect("one_arity", new Cons(betaHead, tail), Term.Tuple(Term.A("beta"), Term.I(7), tail));
            await Expect("repeated", new Cons(badGuardHead, tail), Term.Tuple(Term.A("second"), guardFailure, tail));
            try
            {
                await image.Invoke(
                    context,
                    "step",
                    tail,
                    key
                );
                throw new InvalidOperationException(WrongBehavior);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A("function_clause")))
            {
            }

            return Nil.Value;

            async Task Expect(string name, Term input, Term expected)
            {
                if (!(await image.Invoke(
                    context,
                    name,
                    input,
                    key
                )).Equals(expected))
                {
                    throw new InvalidOperationException(WrongBehavior);
                }
            }
        });
    }

    private static void CheckDispatch(IEnumerable<object> instructions, bool mixedArity)
    {
        object[] body = WithoutLines(instructions).Where(instruction => Opcode(instruction) != Label).ToArray();
        int shape = Array.FindIndex(body, instruction => Opcode(instruction) == IsTuple);
        int arity = Array.FindIndex(body, instruction =>
            Opcode(instruction) == (mixedArity ? SelectTupleArity : TestArity));
        int tags = Array.FindIndex(body, instruction => Opcode(instruction) == SelectValue);
        int frame = Array.FindIndex(body, instruction => Opcode(instruction) is Allocate or AllocateHeap);
        if (body.Length < 3 || Opcode(body[0]) != IsNonemptyList || Opcode(body[1]) != GetList || shape != 2
            || !IsX(Operand(body[1], 0), 0) || !IsX(Operand(body[1], 1), 2) || !IsX(Operand(body[1], 2), 0)
            || arity != shape + 1 || tags <= arity || frame <= tags)
        {
            throw new InvalidOperationException(WrongDispatch);
        }
    }
}
