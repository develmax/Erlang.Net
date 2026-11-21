using System.Collections;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCheckedListComparisonChecks
{
    internal const string TestName = "compiler/beam/types-both-operands-after-list-guards";
    private const string Module = "beam_checked_list_comparison";
    private const string Function = "equal";
    private const string WrongOperands = "The list comparisons did not retain both proven operand types.";
    private const string WrongValue = "The guarded list comparison changed its result.";
    private const string WrongFailure = "The guarded list comparison changed its function-clause failure.";

    private const string Source = """
        -module(beam_checked_list_comparison).
        -export([equal/2]).
        equal(Left, Right) when is_list(Left), is_list(Right) ->
            Left == Right.
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Module + ".erl");
        object[] instructions = ((IEnumerable)Property(Code(beam), "Instructions")).Cast<object>().ToArray();
        byte[] opcodes = instructions.Select(instruction => (byte)Property(Property(instruction, "Definition"), "Opcode")).ToArray();
        int compare = Array.IndexOf(opcodes, BifTwo);
        if (compare < 2 || opcodes[compare - 1] != IsList || opcodes[compare - 2] != IsList)
        {
            throw new InvalidOperationException(WrongOperands);
        }

        object[] operands = ((IEnumerable)Property(instructions[compare], "Operands")).Cast<object>().ToArray();
        if (Property(operands[2], "TypeIndex") is null || Property(operands[3], "TypeIndex") is null)
        {
            throw new InvalidOperationException(WrongOperands);
        }

        var image = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term one = Term.List(Term.I(1));
            Term two = Term.List(Term.I(2));
            if (!Term.A("true").Equals(await image.Invoke(
                context,
                Function,
                one,
                one
            )) || !Term.A("false").Equals(await image.Invoke(
                context,
                Function,
                one,
                two
            )))
            {
                throw new InvalidOperationException(WrongValue);
            }

            try
            {
                await image.Invoke(
                    context,
                    Function,
                    one,
                    Term.A("malformed")
                );
                throw new InvalidOperationException(WrongFailure);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A("function_clause")))
            {
                return Nil.Value;
            }
        });
    }
}
