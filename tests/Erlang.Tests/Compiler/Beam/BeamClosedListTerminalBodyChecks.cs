using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamClosedListTerminalBodyChecks
{
    internal const string TestName = "compiler/beam/plans-terminal-constructors-in-checked-list-arms";
    internal const string CopiesTestName = "compiler/beam/delays-result-spills-after-checked-list-arguments";
    private const string CopiesShapeError = "The saved call missed the delayed result-copy and argument swap.";
    private const string ShapeError = "The terminal return kept a stack slot for its closed constructor value.";
    private const string LiteralOperand = "literal";
    private const string Module = "beam_closed_list_terminal_body";

    private const string Source = """
        -module(beam_closed_list_terminal_body).
        -export([choose/1]).
        choose([File,Config,Wait,Log,Address,Port]) ->
            service:debug(Log),
            Profile = service:profile(Config, default),
            Flags = {mode,0,1},
            Children = [service:configure(Address,Port,Profile), service:save(File,Config,Wait),
                        service:accept(Address,Port,Profile,undefined)],
            {ok,{Flags,Children}};
        choose([File,Config,Wait,Log,Address,Port,Listen]) ->
            service:debug(Log),
            Profile = service:profile(Config, default),
            Flags = {mode,0,1},
            Children = [service:configure(Address,Port,Profile), service:save(File,Config,Wait),
                        service:accept(Address,Port,Profile,Listen)],
            {ok,{Flags,Children}}.
        """;

    internal static void Run()
    {
        string repeated = Source.Replace("{Flags,Children}", "{Flags,Children,Children}", StringComparison.Ordinal);
        if (repeated == Source || !KeepsFlagsInTerminalConstructor(Source) || KeepsFlagsInTerminalConstructor(repeated))
        {
            throw new InvalidOperationException(ShapeError);
        }
    }

    internal static void RunCopies()
    {
        string local = Source.Replace("service:configure", "configure", StringComparison.Ordinal)
            + "\nconfigure(A,P,R) -> service:configure(A,P,R).\n";
        if (!UsesDelayedArgumentSwap(local) || UsesDelayedArgumentSwap(Source))
        {
            throw new InvalidOperationException(CopiesShapeError);
        }
    }

    private static bool UsesDelayedArgumentSwap(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, Module + ".erl"));
        object[] instructions = Children(code, "Instructions");
        for (int index = 3; index < instructions.Length; index++)
        {
            if (Opcode(instructions[index]) == Swap
                && Children(instructions[index], "Operands").Select(Value).SequenceEqual([0, 2])
                && Opcode(instructions[index - 3]) == Move
                && Value(Children(instructions[index - 3], "Operands")[1]) == 2
                && Opcode(instructions[index - 2]) == Move
                && Value(Children(instructions[index - 2], "Operands")[0]) == 0
                && Opcode(instructions[index - 1]) == Move
                && Value(Children(instructions[index - 1], "Operands")[1]) == 1)
            {
                return true;
            }
        }

        return false;
    }

    private static bool KeepsFlagsInTerminalConstructor(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, Module + ".erl"));
        object[] instructions = Children(code, "Instructions");

        return instructions.Count(instruction => Opcode(instruction) == PutTuple
            && Children(instruction, "Operands").Any(operand => (string)Property(operand, "Kind") == LiteralOperand)) == 2;
    }

    private static object[] Children(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");
}
