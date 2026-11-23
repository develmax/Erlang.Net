using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamNestedBranchCopyScheduleChecks
{
    internal const string TestName = "compiler/beam/nested-branch-copy-schedule";
    private const string AliasSchedule = "The outer record alias belongs above its checked field alias.";
    private const string ArgumentSchedule = "A projected first argument leaves only the independent constant copies.";

    private const string Source = """
        -module(beam_nested_branch_copy_schedule).
        -export([dispatch/2, pair/3]).
        -record(sample,{measurement,stamp}).
        -record(session,{output,primary,secondary}).

        dispatch(first, Session=#session{primary=Primary=#sample{measurement=Reading}}) ->
            Stamp = erlang:abs(Reading),
            keeper:mark(Session#session.output, label, []),
            {saved,Session#session{primary=Primary#sample{stamp=Stamp}}};
        dispatch(second, Session=#session{secondary=Secondary=#sample{measurement=Reading}}) ->
            Stamp = keeper:lookup(Reading),
            {saved,Session#session{secondary=Secondary#sample{stamp=Stamp}}}.

        pair(first, Session=#session{}, Sample=#sample{measurement=Reading}) ->
            Stamp = keeper:lookup(Reading),
            {saved,Session#session{primary=Sample#sample{stamp=Stamp}}};
        pair(second, Session=#session{}, Sample=#sample{measurement=Reading}) ->
            Stamp = keeper:lookup(Reading),
            {saved,Session#session{secondary=Sample#sample{stamp=Stamp}}}.
        """;

    internal static void Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, "beam_nested_branch_copy_schedule.erl"));
        object[] instructions = Items(code, "Instructions");
        object function = Items(code, "Functions").Single(candidate => (string)Property(candidate, "Name") == "dispatch");
        int start = (int)Property(function, "FirstInstructionIndex");
        int end = (int)Property(function, "EndInstructionIndex");
        object[] body = instructions[start..end];
        int[] allocations = Enumerable.Range(0, body.Length).Where(index => Opcode(body[index]) == Allocate).ToArray();
        if (allocations.Length != 2 || allocations.Any(index => !HasNestedAliasCopies(index)))
        {
            throw new InvalidOperationException(AliasSchedule);
        }

        int format = Array.FindIndex(body, instruction => Opcode(instruction) == ExternalCall
            && Number(Items(instruction, "Operands")[0]) == 3);
        int lastArgument = format - 1;
        if (lastArgument >= 0 && Opcode(body[lastArgument]) == Line)
        {
            lastArgument--;
        }

        if (lastArgument < 2 || Opcode(body[lastArgument - 2]) != GetTupleElement
            || !IsCopy(
                body[lastArgument - 1],
                "nil",
                0,
                "x",
                2
            )
            || Opcode(body[lastArgument]) != Move
            || Number(Items(body[lastArgument], "Operands")[1]) != 1)
        {
            throw new InvalidOperationException(ArgumentSchedule);
        }

        bool HasNestedAliasCopies(int allocation)
        {
            int width = Number(Items(body[allocation], "Operands")[0]);
            int first = allocation + 1;
            if (Opcode(body[first]) == InitYRegisters)
            {
                first++;
            }

            return IsCopy(
                body[first],
                "x",
                2,
                "y",
                width - 2
            )
                && IsCopy(
                    body[first + 1],
                    "x",
                    1,
                    "y",
                    width - 1
                );
        }
    }

    private static bool IsCopy(
        object instruction,
        string source,
        int sourceValue,
        string target,
        int targetValue
    )
    {
        return Opcode(instruction) == Move && Items(instruction, "Operands") is [var from, var to]
            && (string)Property(from, "Kind") == source && Number(from) == sourceValue
            && (string)Property(to, "Kind") == target && Number(to) == targetValue;
    }

    private static object[] Items(object owner, string property) =>
        ((IEnumerable)Property(owner, property)).Cast<object>().ToArray();

    private static int Number(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");
}
