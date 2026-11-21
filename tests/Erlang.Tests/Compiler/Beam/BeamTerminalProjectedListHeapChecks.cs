using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTerminalProjectedListHeapChecks
{
    internal const string TestName = "compiler/beam/reserves-the-projected-list-return-heap-once";
    private const string Module = "beam_terminal_projected_list_heap";
    private const string ShapeError = "The tagged list return split its heap check or lost its live call result.";

    private const string Source = """
        -module(beam_terminal_projected_list_heap).
        -export([read/2, plain/2]).
        -record(item,{tail,path}).

        read(Path, #item{} = Entry) ->
            case file:read_file_info(Path) of
                {ok, Directory} when element(3, Directory) =:= directory ->
                    Status = erlang:atom_to_list(directory),
                    {answer,[{status,Status}|Entry#item.tail]};
                {ok, File} ->
                    {size,element(2,File),Entry#item.path};
                {error, Reason} ->
                    Status = erlang:atom_to_list(Reason),
                    {answer,[{status,Status}|Entry#item.tail]}
            end.

        plain(Path, Tail) ->
            Status = file:read_file_info(Path),
            {answer,[{status,Status}|Tail]}.
        """;

    internal static Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Module + ".erl");
        object code = Code(beam);
        object[] instructions = Children(code, "Instructions");
        string[] atoms = Children(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        if (HeapReturns(Body("read")) != 1 || HeapReturns(Body("plain")) != 0)
        {
            throw new InvalidOperationException(ShapeError);
        }

        return Task.CompletedTask;

        object[] Body(string name)
        {
            int start = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Children(instruction, "Operands")[1])] == name);

            return start < 0 ? [] : instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static int HeapReturns(object[] instructions)
    {
        int matches = 0;
        for (int index = 0; index + 6 < instructions.Length; index++)
        {
            if (Opcode(instructions[index]) == TestHeap
                && Children(instructions[index], "Operands").Select(Value).SequenceEqual([8, 1])
                && Opcode(instructions[index + 1]) == GetTupleElement
                && Children(instructions[index + 1], "Operands").Select(Value).Last() == 1
                && Opcode(instructions[index + 2]) == PutTuple && Opcode(instructions[index + 3]) == PutList
                && Children(instructions[index + 3], "Operands").Select(Value).SequenceEqual([0, 1, 0])
                && Opcode(instructions[index + 4]) == PutTuple && Opcode(instructions[index + 5]) == Deallocate
                && Opcode(instructions[index + 6]) == Return)
            {
                matches++;
            }
        }

        return matches;
    }

    private static object[] Children(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");
}
