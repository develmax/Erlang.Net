using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamProjectedTerminalBranchRootChecks
{
    internal const string TestName = "compiler/beam/clears-dead-record-root-after-final-projected-argument";
    private const string ShapeError = "The terminal lookup kept its dead saved record root.";
    private const string Source = """
        -module(beam_projected_terminal_branch_root).
        -export([check/1]).
        -record(state, {data}).

        check(#state{}=State) -> checked(State).

        checked(State) ->
            case proplists:get_value(status, State#state.data) of
                {_,_,_} -> true;
                undefined ->
                    case proplists:get_value(response, State#state.data) of
                        undefined -> false;
                        _ -> true
                    end
            end.
        """;

    internal static void Run()
    {
        string retained = Source.Replace("undefined -> false;", "undefined -> {false,State};", StringComparison.Ordinal);
        if (retained == Source || !ClearsDeadArgumentRoot(Source) || ClearsDeadArgumentRoot(retained))
        {
            throw new InvalidOperationException(ShapeError);
        }
    }

    private static bool ClearsDeadArgumentRoot(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, "beam_projected_terminal_branch_root.erl"));
        var instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        for (int index = 0; index + 2 < instructions.Length; index++)
        {
            if (Opcode(instructions[index]) == GetTupleElement
                && Kind(Operands(instructions[index])[0]) == BeamBinaryHostNames.Y
                && Value(Operands(instructions[index])[^1]) == 1
                && Opcode(instructions[index + 1]) == InitYRegisters
                && Operands(instructions[index + 1]).Any(operand => Kind(operand) == BeamBinaryHostNames.Y && Value(operand) == 0)
                && Opcode(instructions[index + 2]) == Move)
            {
                return true;
            }
        }

        return false;
    }

    private static string Kind(object operand) => (string)Property(operand, "Kind");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object operation) => ((IEnumerable)Property(operation, "Operands")).Cast<object>().ToArray();

    private static byte Opcode(object operation) => (byte)Property(Property(operation, "Definition"), "Opcode");
}
