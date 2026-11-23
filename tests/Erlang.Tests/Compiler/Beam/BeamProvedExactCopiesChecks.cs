using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamProvedExactCopiesChecks
{
    internal const string TestName = "compiler/beam/elides-copies-after-a-proven-exact-match";
    private const string Fixture = "beam_proved_exact_copy_probe.erl";
    private const string RepeatedCopyRemains = "An exact guard leaves an unnecessary copy of the matched value.";
    private const string UnprovedCopyMissing = "A copy needed on the successful guard edge was removed.";

    private const string Source = """
        -module(beam_proved_exact_copy_probe).
        -export([fixed/2, changed/2, absolute/1, different/3]).
        fixed(Fun, State) ->
            case Fun(State) of
                State -> State;
                Updated -> fixed(Fun, Updated)
            end.

        changed(Fun, State) ->
            case Fun(State) of
                State -> {unchanged, State};
                Updated -> {changed, Updated}
            end.

        absolute(Value) ->
            case erlang:abs(Value) of
                Value -> Value;
                Other -> {changed, Other}
            end.

        different(Fun, State, Result) ->
            case Fun(State) of
                State -> Result;
                Updated -> {next, Updated}
            end.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = Items(code, "Instructions").ToArray();
        bool keepsOtherCopy = false;
        for (int index = 0; index + 1 < instructions.Length; index++)
        {
            object instruction = instructions[index];
            object next = instructions[index + 1];
            if (Opcode(instruction) == ExactEqualTest && Opcode(next) == Move
                && Items(instruction, "Operands").ToArray() is [_, var left, var right]
                && Items(next, "Operands").ToArray() is [var source, var destination]
                && SameOperand(left, destination))
            {
                if (SameOperand(right, source))
                {
                    throw new InvalidOperationException(RepeatedCopyRemains);
                }

                keepsOtherCopy = true;
            }
        }

        if (!keepsOtherCopy)
        {
            throw new InvalidOperationException(UnprovedCopyMissing);
        }

        return Task.CompletedTask;
    }

    private static bool SameOperand(object left, object right) =>
        (string)Property(left, "Kind") == (string)Property(right, "Kind")
        && (BigInteger)Property(left, "Value") == (BigInteger)Property(right, "Value");

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
