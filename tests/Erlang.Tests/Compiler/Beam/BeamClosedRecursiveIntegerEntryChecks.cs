using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamClosedRecursiveIntegerEntryChecks
{
    internal const string TestName = "compiler/beam/retains-proven-recursive-integer-entry-kind";
    internal const string Fixture = "beam_recursive_integer_entry.erl";
    private const string MissingTypeProof = "Expected exactly one typed closed-recursive integer entry.";

    internal const string Source = """
        -module(beam_recursive_integer_entry).
        -export([run/1, run_open/2, observe/0]).

        run(Input) ->
            counter(Input, 2).

        run_open(Input, Count) ->
            open_counter(Input, Count).

        counter(Input, 0) ->
            {closed, Input};
        counter(Input, Count) ->
            step(),
            counter(Input, Count - 1).

        open_counter(Input, 0) ->
            {open, Input};
        open_counter(Input, Count) when is_integer(Count), Count > 0 ->
            step(),
            open_counter(Input, Count - 1);
        open_counter(_, Value) ->
            {rejected, Value}.

        step() ->
            case get(steps) of
                undefined -> put(steps, 1);
                Count -> put(steps, Count + 1)
            end,
            ok.

        observe() ->
            erase(steps),
            Closed = run(example),
            Open = run_open(example, 1),
            Floating = run_open(example, 0.5),
            {Closed, Open, Floating, get(steps)}.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        int typedZeroTests = 0;
        foreach (object instruction in ((IEnumerable)Property(code, "Instructions")).Cast<object>())
        {
            if ((byte)Property(Property(instruction, "Definition"), "Opcode") != ExactEqualTest)
            {
                continue;
            }

            object[] operands = ((IEnumerable)Property(instruction, "Operands")).Cast<object>().ToArray();
            if ((string)Property(operands[2], "Kind") == "integer"
                && (BigInteger)Property(operands[2], "Value") == BigInteger.Zero
                && Property(operands[1], "TypeIndex") is int)
            {
                typedZeroTests++;
            }
        }

        if (typedZeroTests != 1)
        {
            throw new InvalidOperationException(MissingTypeProof);
        }

        return Task.CompletedTask;
    }
}
