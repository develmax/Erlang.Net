using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamFixedListTerminalCaseChecks
{
    internal const string TestName = "compiler/beam/shares-fixed-list-spine-before-terminal-arm-frames";
    private const string IncorrectLayout = "The shared list checks must precede independent terminal arm frames.";
    private const string IncorrectResult = "A fixed list arm changed its result, effect order, or case failure.";
    private const string Source = """
        -module(beam_fixed_list_terminal_case).
        -export([make/1, state/0]).

        make(Action) ->
            fun(Data) ->
                case Data of
                    [A, B] ->
                        _ = stamp(A),
                        pair(A, B, Action);
                    [A, B, C, D] ->
                        _ = stamp(A),
                        Previous = stamp({C, D}),
                        finish(A, B, Action, Previous);
                    [A, B, C] ->
                        pair(A, B, C)
                end
            end.

        stamp(Value) -> erlang:put(fixed_list_terminal_probe, Value).
        state() -> erlang:get(fixed_list_terminal_probe).
        pair(A, B, C) -> {A, B, C}.
        finish(A, B, C, D) -> {A, B, C, D}.
        """;

    internal static async Task Run()
    {
        byte[] bytes = CodeGeneration.CompileBeamModule(Source, "beam_fixed_list_terminal_case.erl");
        object code = Code(bytes);
        object[] instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        string[] atoms = ((IEnumerable)Property(Property(code, "Structure"), "Atoms")).Cast<string>().ToArray();
        int info = Array.FindIndex(
            instructions,
            operation => Opcode(operation) == FunctionInfo
            && atoms[(int)OperandValue(operation, 1)].StartsWith("-make/1-", StringComparison.Ordinal)
        );
        if (info < 0)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object[] body = instructions.Skip(info + 2).TakeWhile(operation => Opcode(operation) != FunctionInfo).ToArray();
        object[] frames = body.Where(operation => Opcode(operation) is Allocate or AllocateHeap).ToArray();
        if (!body.Take(8).Select(Opcode).SequenceEqual(new byte[]
            {
                IsNonemptyList, GetList, IsNonemptyList, GetList,
                IsNonemptyList, GetList, IsNonemptyList, GetList
            })
            || !frames.Select(frame => (int)OperandValue(frame, 0)).SequenceEqual(new[] { 5, 3 })
            || body.Count(operation => Opcode(operation) == GetList) != 4
            || body.Count(operation => Opcode(operation) == IsNil) != 3
            || !body.Any(operation => Opcode(operation) == TailLocalCall)
            || OperandValue(body.Single(operation => Opcode(operation) == CaseEnd), 0) != 0)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        var image = BeamModule.Prepare(bytes);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term a = Term.A("a");
            Term b = Term.A("b");
            Term c = Term.A("c");
            Term d = Term.A("d");
            Term action = Term.A("action");
            var function = (FunctionTerm)await image.Invoke(context, "make", action);
            if (!Term.Tuple(a, b, c).Equals(await function.Invoke(context, [Term.List(a, b, c)]))
                || !Term.A("undefined").Equals(await image.Invoke(context, "state"))
                || !Term.Tuple(
                    a,
                    b,
                    action,
                    a
                ).Equals(await function.Invoke(context, [Term.List(
                    a,
                    b,
                    c,
                    d
                )]))
                || !Term.Tuple(c, d).Equals(await image.Invoke(context, "state"))
                || !Term.Tuple(b, a, action).Equals(await function.Invoke(context, [Term.List(b, a)]))
                || !b.Equals(await image.Invoke(context, "state")))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            Term[] rejected = [Nil.Value, Term.List(a), new Cons(a, new Cons(b, c)), Term.List(
                a,
                b,
                c,
                d,
                action
            )];
            foreach (Term input in rejected)
            {
                try
                {
                    await function.Invoke(context, [input]);
                    throw new InvalidOperationException(IncorrectResult);
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A("case_clause"), input)))
                {
                }
            }

            if (!b.Equals(await image.Invoke(context, "state")))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            return Nil.Value;
        });
    }

    private static BigInteger OperandValue(object instruction, int position) =>
        (BigInteger)Property(((IEnumerable)Property(instruction, "Operands")).Cast<object>().ElementAt(position), "Value");

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");
}
