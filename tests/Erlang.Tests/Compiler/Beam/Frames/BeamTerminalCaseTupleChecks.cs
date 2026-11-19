using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTerminalCaseTupleChecks
{
    internal const string TestName = "compiler/beam/duplicates-terminal-tuples-into-case-call-predecessors";
    private const string Module = "beam_terminal_case_tuple";
    private const string WrongDispatch = "Expected the checked tuple projection and select before the branch frames.";
    private const string WrongConstruction = "Expected an X0-rooted tuple after each returning call and a tail call for the raising arm.";
    private const string WrongResult = "The case must retain the original tuple tag and match an already bound result.";
    private const string MissingFailure = "Expected the selected case to raise the original exception.";

    private const string Source = """
        -module(beam_terminal_case_tuple).
        -export([bound/2,other_selector/3,bound_name/3,ordered/2]).
        bound({Tag, Value}, State) ->
            Result = case Tag of
                east -> east_value(Value, State);
                unavailable -> unavailable(Value, State);
                west -> west_value(Value, State)
            end,
            {Tag, Result}.
        other_selector({Tag, Value}, Selector, State) ->
            Result = case Selector of
                east -> east_value(Value, State);
                west -> west_value(Value, State)
            end,
            {Tag, Result}.
        bound_name(Tag, Data, Result) ->
            Result = case Tag of
                east -> east_value(Data, 2);
                west -> west_value(Data, 3)
            end,
            {Tag, Result}.
        ordered(Tag, Value) ->
            Result = case Tag of
                east -> erlang:put(order, Value);
                west -> erlang:put(order, other)
            end,
            {erlang:get(order), Result}.
        east_value(Value, State) -> {east_result, Value, State}.
        west_value(Value, State) -> {west_result, Value, State}.
        unavailable(_Value, _State) -> erlang:error(unavailable).
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Module + ".erl");
        object[] instructions = ((IEnumerable)Property(Code(beam), "Instructions")).Cast<object>()
            .SkipWhile(instruction => Opcode(instruction) != FunctionInfo).Skip(1)
            .TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .Where(instruction => Opcode(instruction) is not (Label or Line)).ToArray();
        byte[] opcodes = instructions.Select(Opcode).ToArray();
        ReadOnlySpan<byte> entry = [IsTuple, TestArity, GetTupleElement, GetTupleElement, SelectValue];
        ReadOnlySpan<byte> returning = [Allocate, LocalCall, TestHeap, PutTuple, Deallocate, Return];
        if (!opcodes.AsSpan().StartsWith(entry))
        {
            throw new InvalidOperationException(WrongDispatch);
        }

        int first = opcodes.AsSpan().IndexOf(returning);
        int second = first < 0 ? -1 : opcodes.AsSpan(first + returning.Length).IndexOf(returning);
        if (second < 0 || !opcodes.Contains(TailLocalCall) || !opcodes.Contains(CaseEnd))
        {
            throw new InvalidOperationException(WrongConstruction);
        }

        CheckReturningArm(first);
        CheckReturningArm(first + returning.Length + second);

        BeamModule image = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term input = Term.A("input");
            Term east = Term.A("east");
            Term state = Term.I(2);
            Term data = Term.Tuple(Term.A("kept"), input);
            Term expected = Term.Tuple(Term.A("east_result"), input, state);
            Term bound = await image.Invoke(
                context,
                "bound",
                Term.Tuple(east, input),
                state
            );
            Term other = await image.Invoke(
                context,
                "other_selector",
                data,
                east,
                state
            );
            Term matched = await image.Invoke(
                context,
                "bound_name",
                east,
                input,
                expected
            );
            Term ordered = await image.Invoke(
                context,
                "ordered",
                east,
                input
            );
            if (!bound.Equals(Term.Tuple(east, expected))
                || !other.Equals(Term.Tuple(Term.A("kept"), expected))
                || !matched.Equals(Term.Tuple(east, expected))
                || !ordered.Equals(Term.Tuple(input, Term.A("undefined"))))
            {
                throw new InvalidOperationException(WrongResult);
            }

            try
            {
                await image.Invoke(
                    context,
                    "bound_name",
                    east,
                    input,
                    Term.A("different")
                );
                throw new InvalidOperationException(MissingFailure);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A("badmatch"), expected)))
            {
            }

            Term unavailable = Term.A("unavailable");
            try
            {
                await image.Invoke(
                    context,
                    "bound",
                    Term.Tuple(unavailable, input),
                    state
                );
                throw new InvalidOperationException(MissingFailure);
            }
            catch (ErlangException error) when (error.Reason.Equals(unavailable))
            {
            }

            Term unknown = Term.A("unknown");
            try
            {
                await image.Invoke(
                    context,
                    "bound",
                    Term.Tuple(unknown, input),
                    state
                );
                throw new InvalidOperationException(MissingFailure);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A("case_clause"), unknown)))
            {
            }

            return Nil.Value;
        });

        void CheckReturningArm(int start)
        {
            if (Operand(instructions[start], 0) != 0 || Operand(instructions[start], 1) != 2
                || Operand(instructions[start + 2], 0) != 3 || Operand(instructions[start + 2], 1) != 1)
            {
                throw new InvalidOperationException(WrongConstruction);
            }
        }
    }

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Operand(object instruction, int index) =>
        (BigInteger)Property(((IEnumerable)Property(instruction, "Operands")).Cast<object>().ElementAt(index), "Value");
}
