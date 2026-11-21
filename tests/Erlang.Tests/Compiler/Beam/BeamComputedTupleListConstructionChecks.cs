using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamComputedTupleListConstructionChecks
{
    internal const string TestName = "compiler/beam/constructs-sibling-list-cells-after-scalar-operands";
    private const string Fixture = "beam_computed_tuple_list_probe.erl";
    private const string LateScalars = "The scalar fields must precede the shared constructor heap check.";
    private const string LostEffect = "A consumed tuple moved beyond the call that observes it.";

    private const string Source = """
        -module(beam_computed_tuple_list_probe).
        -export([run/0,control/0]).

        run() ->
            Seed = sample(),
            Key = narrow(Seed),
            Mixed = (255 band (Key bsl 3)) bor (rand:uniform(8) - 1),
            ets:insert(computed_tuple_list_table, [{first,Key},
                                                   {second,Key bsl 4},
                                                   {third,Mixed}]),
            Key.

        control() ->
            Seed = sample(),
            Key = narrow(Seed),
            Mixed = (255 band (Key bsl 3)) bor (rand:uniform(8) - 1),
            First = {first,Key},
            erlang:put(consumed, First),
            ets:insert(computed_tuple_list_table, [First,
                                                  {second,Key bsl 4},
                                                  {third,Mixed}]),
            Key.

        sample() -> erlang:put(sample, visited).

        narrow(Value) -> Value band 63.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] run = Body("run");
        int heapCheck = Array.FindIndex(run, instruction => Opcode(instruction) == TestHeap);
        if (heapCheck <= Array.FindLastIndex(run, instruction => Opcode(instruction) == GcBifTwo)
            || run.Count(instruction => Opcode(instruction) == TestHeap) != 1
            || Value(Operands(run[heapCheck])[0]) != 15 || Value(Operands(run[heapCheck])[1]) != 2
            || Value(Operands(run.Single(instruction => Opcode(instruction) == Allocate))[0]) != 2)
        {
            throw new InvalidOperationException(LateScalars);
        }

        object[] control = Body("control");
        int firstTuple = Array.FindIndex(control, instruction => Opcode(instruction) == PutTuple);
        if (firstTuple < 0 || firstTuple >= Array.FindLastIndex(control, instruction => Opcode(instruction) == GcBifTwo)
            || !control.Skip(firstTuple + 1).Any(instruction => Opcode(instruction) == ExternalCall))
        {
            throw new InvalidOperationException(LostEffect);
        }

        return Task.CompletedTask;

        object[] Body(string name)
        {
            int start = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)Value(Operands(instruction)[1])] == name);

            return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
