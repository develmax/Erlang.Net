using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamConsCallContinuationChecks
{
    internal const string TestName = "compiler/beam/keeps-a-checked-cons-tail-in-the-call-frame";
    private const string Module = "beam_cons_call_continuation";
    private const string WrongFrame = "A checked list continuation changed the frame or retained extra bindings.";
    private const string WrongValue = "A checked list continuation changed its result.";
    private const string WrongFailure = "A checked list continuation changed its function-clause failure.";

    private const string Source = """
        -module(beam_cons_call_continuation).
        -export([take/1, step/2]).

        take([A,B|More]) -> finish(combine(A,B), More);
        take([A]) -> A.

        step(Acc, [Head|Tail]) -> finish(combine(Acc,Head), Tail);
        step(Acc, []) -> Acc.

        combine(A,B) -> {A,B}.
        finish(Value,Tail) -> {Value,Tail}.
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Module + ".erl");
        object[] instructions = ((IEnumerable)Property(Code(beam), "Instructions")).Cast<object>().ToArray();
        byte[] opcodes = instructions.Select(instruction => (byte)Property(Property(instruction, "Definition"), "Opcode")).ToArray();
        int[][] frames = instructions.Where((_, index) => opcodes[index] == Allocate)
            .Select(instruction => ((IEnumerable)Property(instruction, "Operands")).Cast<object>()
                .Select(operand => (int)(BigInteger)Property(operand, "Value")).ToArray()).ToArray();
        if (frames.Length != 2 || !frames[0].SequenceEqual([1, 3]) || !frames[1].SequenceEqual([1, 2])
            || opcodes.Count(opcode => opcode == IsNonemptyList) != 3
            || opcodes.Count(opcode => opcode == GetList) != 3
            || opcodes.Count(opcode => opcode == TailLocalCallWithFrame) != 2)
        {
            throw new InvalidOperationException(WrongFrame);
        }

        var image = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term a = Term.A("a");
            Term b = Term.A("b");
            Term suffix = Term.A("suffix");
            Term expected = Term.Tuple(Term.Tuple(a, b), suffix);
            Term nested = new Cons(a, new Cons(b, suffix));
            if (!expected.Equals(await image.Invoke(context, "take", nested))
                || !a.Equals(await image.Invoke(context, "take", Term.List(a)))
                || !expected.Equals(await image.Invoke(
                    context,
                    "step",
                    a,
                    new Cons(b, suffix)
                ))
                || !a.Equals(await image.Invoke(
                    context,
                    "step",
                    a,
                    Nil.Value
                )))
            {
                throw new InvalidOperationException(WrongValue);
            }

            try
            {
                await image.Invoke(context, "take", new Cons(a, suffix));
                throw new InvalidOperationException(WrongFailure);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A("function_clause")))
            {
                return Nil.Value;
            }
        });
    }
}
