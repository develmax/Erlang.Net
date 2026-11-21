using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTerminalCallResultArgumentChecks
{
    internal const string TestName = "compiler/beam/keeps-call-results-in-terminal-argument-registers";
    internal const string FrameTestName = "compiler/beam/compacts-dead-caught-input-stack-slot";
    private const string FrameShapeError = "The protected call retained an extra entry argument slot.";
    internal const string TrimTestName = "compiler/beam/trims-owned-return-arm-after-closed-catch";
    private const string TrimShapeError = "The closed catch retained dead stack slots in the returning arm.";
    private const string Module = "beam_terminal_call_result_arguments";
    private const string ShapeError = "The terminal call kept a stack spill for its local result.";

    private const string Source = """
        -module(beam_terminal_call_result_arguments).
        -export([checked/3]).

        checked(Options = [{_,_}|_], Timeout, Debug) ->
            case catch service:validate(Options) of
                {ok, Checked} ->
                    Address = proplists:get_value(address, Checked),
                    Port = proplists:get_value(port, Checked),
                    Profile = proplists:get_value(profile, Checked, default),
                    Name = local_name(Address, Port, Profile),
                    service:start({local,Name}, service, [undefined,Checked,Timeout,Debug,Address,Port]);
                {error, Reason} ->
                    service:report(Reason),
                    {stop,Reason}
            end.

        local_name(A,B,C) -> service:name(fixture,A,B,C).
        """;

    internal static void Run()
    {
        string alternate = Source.Replace("{local,Name}, service,", "{local,Name}, Debug,", StringComparison.Ordinal);
        if (alternate == Source || !UsesDirectArguments(Source) || UsesDirectArguments(alternate))
        {
            throw new InvalidOperationException(ShapeError);
        }
    }

    internal static void RunFrame()
    {
        string liveInput = Source.Replace("service:report(Reason)", "service:report({Reason,Options})", StringComparison.Ordinal);
        if (liveInput == Source || !HasCompactedFrame(Source) || HasCompactedFrame(liveInput))
        {
            throw new InvalidOperationException(FrameShapeError);
        }
    }

    private static bool HasCompactedFrame(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, Module + ".erl"));
        object[] instructions = Children(code, "Instructions");
        var allocations = instructions.Where(instruction => Opcode(instruction) == Allocate).ToArray();
        var catches = instructions.Where(instruction => Opcode(instruction) == CatchEnter).ToArray();

        return allocations.Length == 1 && catches.Length == 1
            && Children(allocations[0], "Operands").Select(Value).SequenceEqual([5, 3])
            && Value(Children(catches[0], "Operands")[0]) == 4;
    }

    internal static void RunTrim()
    {
        string twoPayloads = Source.Replace("{stop,Reason}", "{stop,Reason,Timeout}", StringComparison.Ordinal);
        if (twoPayloads == Source || !UsesSingleSlotTrim(Source) || UsesSingleSlotTrim(twoPayloads))
        {
            throw new InvalidOperationException(TrimShapeError);
        }
    }

    private static bool UsesSingleSlotTrim(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, Module + ".erl"));
        object[] instructions = Children(code, "Instructions");
        int caught = Array.FindIndex(instructions, instruction => Opcode(instruction) == CatchEnd);
        for (int index = caught + 1; index + 1 < instructions.Length; index++)
        {
            if (Opcode(instructions[index]) == TrimFrame && Opcode(instructions[index + 1]) == Move
                && Children(instructions[index], "Operands").Select(Value).SequenceEqual([4, 1])
                && Children(instructions[index + 1], "Operands").Select(Value).SequenceEqual([0, 0]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool UsesDirectArguments(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, Module + ".erl"));
        object[] instructions = Children(code, "Instructions");
        for (int index = 0; index + 3 < instructions.Length; index++)
        {
            if (Opcode(instructions[index]) == LocalCall && Opcode(instructions[index + 1]) == TestHeap
                && Children(instructions[index + 1], "Operands").Select(Value).SequenceEqual([15, 1])
                && Opcode(instructions[index + 2]) == PutTuple
                && Opcode(instructions[index + 3]) == PutList
                && Children(instructions[index + 3], "Operands").Select(Value).Last() == 1)
            {
                return true;
            }
        }

        return false;
    }

    private static object[] Children(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");
}
