using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamOwnedCaseResultChecks
{
    internal const string TestName = "compiler/beam/places-owned-case-result-copies-on-success-edges";
    private const string Fixture = "owned_case_result_probe.erl";
    private const string MissingCopy = "The ordinary case edges must establish the single saved result before the join.";
    private const string WrongJoin = "The effect and return must keep their shared one-slot continuation.";
    private const string WrongObservation = "The result read by a nonterminal effect must still be prepared at its join.";

    internal const string Source = """
        -module(owned_case_result_probe).
        -export([owned/1, owned_simple/1, owned_nested/1, advance/1, sink/0, observed_join/1, evaluate_after/1, catch_sink/1, failing_case/1, failing_arm/1]).

        owned(Input) ->
            Carrier0 = erlang:hd(Input),
            Result = case Carrier0 of
                {ok, Inner} when Inner =:= 0 -> Carrier0;
                {ok, Inner} when Inner =:= 1 -> Carrier0;
                {error, Error} -> decorate(Error);
                Default -> {other, Default}
            end,
            ?MODULE:sink(),
            Result.

        owned_simple(Input) ->
            Result = case Input of
                {ok, Value} -> Value;
                Other -> decorate(Other)
            end,
            ?MODULE:sink(),
            Result.

        owned_nested(Input) ->
            Result = case Input of
                {ok, Inner} ->
                    case ?MODULE:advance(Inner) of
                        {done, Value} -> Value;
                        Other -> decorate(Other)
                    end;
                Other -> {outside, Other}
            end,
            ?MODULE:sink(),
            Result.

        advance(Value) -> {done, Value}.

        observed_join(Input) ->
            Result = case Input of
                {ok, Value} -> Value;
                Other -> decorate(Other)
            end,
            put(observed, Result),
            ?MODULE:sink(),
            Result.

        evaluate_after(Input) ->
            Result = case Input of
                {ok, Value} -> Value;
                Other -> decorate(Other)
            end,
            ?MODULE:sink(),
            decorate(Result).

        catch_sink(Input) ->
            Result = case Input of
                {ok, Value} -> Value;
                Other -> decorate(Other)
            end,
            try ?MODULE:sink() of
                _ -> Result
            catch
                error:ProbeError -> {caught, Result, ProbeError}
            end.

        failing_case(Input) ->
            Result = case Input of
                {ok, Value} -> Value;
                {more, Value} -> decorate(Value)
            end,
            ?MODULE:sink(),
            Result.

        failing_arm(Input) ->
            Result = case Input of
                {ok, Value} -> Value;
                Bad -> erlang:error({rejected, Bad})
            end,
            ?MODULE:sink(),
            Result.

        decorate(Value) -> {decorated, Value}.

        sink() ->
            put(calls, 1 + case get(calls) of undefined -> 0; N -> N end),
            case get(fail_sink) of
                true -> erlang:error(sink_failure);
                _ -> ok
            end.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        int[] starts = Enumerable.Range(0, instructions.Length)
            .Where(index => Opcode(instructions[index]) == FunctionInfo)
            .ToArray();
        object[] owned = instructions[starts[1]..starts[2]];
        int effect = Array.FindIndex(owned, instruction => Opcode(instruction) == ExternalCall);
        if (effect < 2 || Opcode(owned[effect - 1]) != Line || Opcode(owned[effect - 2]) != Label
            || !owned.Any(instruction => Opcode(instruction) == Allocate && Value(Operand(instruction, 0)) == 1)
            || !IsMove(owned[effect + 1], "y", "x"))
        {
            throw new InvalidOperationException(WrongJoin);
        }

        int copies = owned.Take(effect - 2).Count(instruction =>
            IsMove(instruction, "x", "y")
            || Opcode(instruction) == GetTupleElement && IsRegister(Operand(instruction, 2), "y"));
        if (copies != 2)
        {
            throw new InvalidOperationException(MissingCopy);
        }

        object[] observed = instructions[starts[4]..starts[5]];
        if (observed.Count(instruction => IsMove(instruction, "x", "y")) != 1)
        {
            throw new InvalidOperationException(WrongObservation);
        }

        return Task.CompletedTask;
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object Operand(object instruction, int index) =>
        ((IEnumerable)Property(instruction, "Operands")).Cast<object>().ElementAt(index);

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");

    private static bool IsRegister(object operand, string kind) =>
        (string)Property(operand, "Kind") == kind && Value(operand) == 0;

    private static bool IsMove(object instruction, string source, string destination) =>
        Opcode(instruction) == Move
        && IsRegister(Operand(instruction, 0), source)
        && IsRegister(Operand(instruction, 1), destination);
}
