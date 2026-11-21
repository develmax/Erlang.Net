using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTerminalRecordOperandRootChecks
{
    internal const string TestName = "compiler/beam/terminal-record-update-exact-x-roots";
    private const string MissingRoots = "A terminal record update spilled two live operands instead of rooting them in X.";
    private const string LostContinuation = "Forwarding discarded a value that must remain live past the record update.";
    private const string Source = """
        -module(beam_terminal_record_operand_roots).
        -export([closed/2, retained/2]).
        -record(state,{normal,other}).
        -record(reading,{obj,value,timer,limit}).
        closed(tick, State=#state{normal=Reading=#reading{obj=Obj,value=Value,timer=Old,limit=Limit}}) ->
            Timer = if Value >= Limit -> service:finish(Obj), Old;
                       true -> service:next(Obj, Value)
                    end,
            {ok,State#state{normal=Reading#reading{value=Value+5,timer=Timer}}};
        closed(other, State=#state{}) ->
            State.
        retained(tick, State=#state{normal=Reading=#reading{obj=Obj,value=Value,timer=Old,limit=Limit}}) ->
            Timer = if Value >= Limit -> service:finish(Obj), Old;
                       true -> service:next(Obj, Value)
                    end,
            {ok,State#state{normal=Reading#reading{value=Value+5,timer=Timer}},Timer};
        retained(other, State=#state{}) ->
            State.
        """;

    internal static void Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, "beam_terminal_record_operand_roots.erl"));
        var instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        var functions = ((IEnumerable)Property(code, "Functions")).Cast<object>().ToArray();
        object[] resolved = Body("closed");
        int arithmetic = Array.FindIndex(resolved, instruction => Opcode(instruction) == GcBifTwo);
        if (arithmetic < 0 || ReadOperands(resolved[arithmetic])[1].Value != 1
            || ReadOperands(resolved[arithmetic])[^1] != ("x", 1)
            || Opcode(resolved[arithmetic + 1]) != TestHeap
            || ReadOperands(resolved[arithmetic + 1])[^1].Value != 2)
        {
            throw new InvalidOperationException(MissingRoots);
        }

        if (Body("retained").Where(instruction => Opcode(instruction) == GcBifTwo)
            .Any(instruction => ReadOperands(instruction)[^1] == ("x", 1)))
        {
            throw new InvalidOperationException(LostContinuation);
        }

        object[] Body(string name)
        {
            object function = functions.Single(candidate => (string)Property(candidate, "Name") == name);
            int start = (int)Property(function, "FirstInstructionIndex");
            int end = (int)Property(function, "EndInstructionIndex");

            return instructions[start..end];
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static (string Kind, int Value)[] ReadOperands(object instruction) =>
        ((IEnumerable)Property(instruction, "Operands")).Cast<object>()
            .Select(operand => ((string)Property(operand, "Kind"), (int)(BigInteger)Property(operand, "Value"))).ToArray();
}
