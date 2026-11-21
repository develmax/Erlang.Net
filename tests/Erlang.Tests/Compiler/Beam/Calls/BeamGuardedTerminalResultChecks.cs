using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamGuardedTerminalResultChecks
{
    internal const string TestName = "compiler/beam/guarded-terminal-call-result";

    private const string Source = """
        -module(beam_guarded_terminal_result).
        -export([cancel/1,keep_reference/1,guard_other/2]).

        cancel({timer, Ref}) ->
            case erlang:cancel_timer(Ref) of
                Remaining when is_integer(Remaining) -> {cancelled, Remaining};
                _ -> {error, {expired, Ref}}
            end;
        cancel(infinity) ->
            ok;
        cancel(BadRef) ->
            {error, {bad_ref, BadRef}}.

        keep_reference({timer, Ref}) ->
            case erlang:cancel_timer(Ref) of
                Remaining when is_integer(Remaining) -> {cancelled, Ref};
                _ -> {error, {expired, Ref}}
            end.

        guard_other(Ref, Checked) ->
            case erlang:cancel_timer(Ref) of
                Result when is_integer(Checked) -> {cancelled, Result};
                _ -> {error, {expired, Ref}}
            end.
        """;

    internal static void Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, "beam_guarded_terminal_result.erl"));
        object[] instructions = Items(code, "Instructions");
        object[] functions = Items(code, "Functions");
        object[] accepted = Body("cancel");
        int predicate = Array.FindIndex(accepted, instruction => Opcode(instruction) == IsInteger);
        if (predicate < 1 || Opcode(accepted[predicate - 1]) != ExternalCall
            || !IsRegister(Items(accepted[predicate], "Operands")[1], "x", 0)
            || Opcode(accepted[predicate + 1]) != TestHeap
            || !Items(accepted[predicate + 1], "Operands").Select(Number).SequenceEqual([3, 1])
            || Opcode(accepted[predicate + 2]) != PutTuple
            || !IsRegister(Items(accepted[predicate + 2], "Operands")[^1], "x", 0))
        {
            throw new InvalidOperationException("The success edge must keep the tested call result rooted in X0.");
        }

        int rejectedLabel = Number(Items(accepted[predicate], "Operands")[0]);
        int rejected = Array.FindIndex(accepted, predicate + 1, instruction => Opcode(instruction) == Label
            && Number(Items(instruction, "Operands")[0]) == rejectedLabel);
        if (rejected < 0 || Opcode(accepted[rejected + 1]) != TestHeap
            || !Items(accepted[rejected + 1], "Operands").Select(Number).SequenceEqual([6, 0])
            || Opcode(accepted[rejected + 2]) != PutTuple
            || !IsRegister(Items(accepted[rejected + 2], "Operands")[^1], "y", 0))
        {
            throw new InvalidOperationException("The failure edge must keep the original reference in Y0.");
        }

        object[] retained = Body("keep_reference");
        int referenceTest = Array.FindIndex(retained, instruction => Opcode(instruction) == IsInteger);
        if (referenceTest < 0 || Opcode(retained[referenceTest + 1]) != TestHeap
            || !Items(retained[referenceTest + 1], "Operands").Select(Number).SequenceEqual([3, 0])
            || Opcode(retained[referenceTest + 2]) != PutTuple
            || !IsRegister(Items(retained[referenceTest + 2], "Operands")[^1], "y", 0))
        {
            throw new InvalidOperationException("A construction returning the original reference must keep its saved operand.");
        }

        object[] Body(string name)
        {
            object function = functions.Single(candidate => (string)Property(candidate, "Name") == name);
            int start = (int)Property(function, "FirstInstructionIndex");
            int end = (int)Property(function, "EndInstructionIndex");

            return instructions[start..end];
        }
    }

    private static bool IsRegister(object operand, string kind, int number) =>
        (string)Property(operand, "Kind") == kind && Number(operand) == number;

    private static int Number(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string property) =>
        ((IEnumerable)Property(owner, property)).Cast<object>().ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");
}
