using System.Collections;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamNonterminalCaseCarrierChecks
{
    internal const string TestName = "compiler/beam/reuses-tagged-case-carrier-in-nested-results";
    private const string Fixture = "case_carrier_otp_probe.erl";
    private const string WrongConstructors = "A nested result rebuilt a selected tuple or lost a distinct error constructor.";

    internal const string Source = """
        -module(case_carrier_otp_probe).
        -export([run/2, get_input/2, step/2, record/1]).
        run(Route, Value) ->
            Result =
                case ?MODULE:get_input(Route, Value) of
                    {ok, Field} ->
                        case ?MODULE:step(Route, Field) of
                            first -> {ok, Field};
                            {second, _} -> {ok, Field};
                            Third -> {failed, Third}
                        end;
                    Other -> Other
                end,
            ?MODULE:record(Result),
            Result.
        get_input(rejected, Value) -> {rejected, Value};
        get_input(_, Value) -> {ok, Value}.
        step(first, _) -> first;
        step(second, V) -> {second, V};
        step(exception, V) -> erlang:error({step_failed,V});
        step(_, V) -> {failed_step, V}.
        record(Result) -> self() ! {recorded, Result}.
        """;

    private const string ScratchFixture = "case_payload_scratch_probe.erl";
    private const string ScratchMessage = "Saved case selector patterns must test closed payloads in X while preserving escaping bindings.";

    internal const string ScratchSource = """
        -module(case_payload_scratch_probe).
        -export([run/2, step/1, audit/1]).
        run(Selector, Keep) ->
            Result =
                case ?MODULE:step(Selector) of
                    {ok, {prompt, "login: "}, _} -> matched;
                    {ok, [{prompt, _First}, {prompt, _Second}], _} -> accepted;
                    {ok, [{capture, First}], _} -> ?MODULE:audit(First), First;
                    Error -> ?MODULE:audit(Error), {error, Error}
                end,
            ?MODULE:audit(Keep),
            Result.
        step(throw) -> erlang:error(selector_failed);
        step(Value) -> Value.
        audit(Value) -> self() ! {recorded, Value}.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        byte[] opcodes = ((IEnumerable)Property(code, "Instructions")).Cast<object>()
            .Select(instruction => (byte)Property(Property(instruction, "Definition"), "Opcode"))
            .ToArray();
        int first = Array.IndexOf(opcodes, FunctionInfo);
        int end = Array.IndexOf(opcodes, FunctionInfo, first + 1);
        byte[] result = opcodes[first..end];
        if (result.Count(opcode => opcode == PutTuple) != 1
            || !result.Contains(IsTaggedTuple)
            || result.Count(opcode => opcode == ExternalCall) < 3)
        {
            throw new InvalidOperationException(WrongConstructors);
        }

        CheckSavedSelectorPayloadScratch();

        return Task.CompletedTask;
    }

    private static void CheckSavedSelectorPayloadScratch()
    {
        object code = Code(CodeGeneration.CompileBeamModule(ScratchSource, ScratchFixture));
        object[] instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        int first = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo);
        int end = Array.FindIndex(instructions, first + 1, instruction => Opcode(instruction) == FunctionInfo);
        object[] run = instructions[first..end];
        if (run.Count(instruction => Opcode(instruction) == GetList
                && IsRegister(
                    instruction,
                    0,
                    "x",
                    0
                )
                && IsRegister(
                    instruction,
                    1,
                    "x",
                    1
                )
                && IsRegister(
                    instruction,
                    2,
                    "x",
                    0
                )) != 2
            || !Enumerable.Range(1, run.Length - 1).Any(index =>
                Opcode(run[index - 1]) == GetTupleElement && IsRegister(
                    run[index - 1],
                    2,
                    "x",
                    0
                )
                && Opcode(run[index]) == ExactEqualTest && IsRegister(
                    run[index],
                    1,
                    "x",
                    0
                ))
            || !run.Any(instruction => Opcode(instruction) == GetList && RegisterKind(instruction, 1) == "y"))
        {
            throw new InvalidOperationException(ScratchMessage);
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static bool IsRegister(
        object instruction,
        int index,
        string kind,
        int register
    )
    {
        object operand = Operand(instruction, index);

        return (string)Property(operand, "Kind") == kind
            && (System.Numerics.BigInteger)Property(operand, "Value") == register;
    }

    private static string RegisterKind(object instruction, int index) => (string)Property(Operand(instruction, index), "Kind");

    private static object Operand(object instruction, int index) =>
        ((IEnumerable)Property(instruction, "Operands")).Cast<object>().ElementAt(index);
}
