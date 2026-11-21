using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamRetainedCallConstructionChecks
{
    internal const string TestName = "compiler/beam/keeps-the-producer-in-x0-through-retained-construction";
    private const string Fixture = "retained_call_construction_probe.erl";
    private const string WrongFrame = "The producer result was moved out of X0 or an extra frame slot was kept.";
    private const string WrongOrder = "A second source effect was moved outside its position.";
    private const string Source = """
        -module(retained_call_construction_probe).
        -export([build/2, control/2]).
        build(First, Second) ->
            Name = producer(First),
            Descriptor = {service, start, [First, Second, default]},
            Flag = stable,
            {Name, Descriptor, Flag}.

        control(First, Second) ->
            Name = producer(First),
            Descriptor = {service, start, [First, side_effect(Second), default]},
            {Name, Descriptor, stable}.

        producer(failure) -> erlang:error(source_failed);
        producer(Value) ->
            erlang:put(produced, Value),
            {name, Value}.
        side_effect(Value) ->
            erlang:put(second_value, Value),
            Value.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        object[] build = Body("build");
        object allocation = build.Single(instruction => Opcode(instruction) == Allocate);
        object heap = build.Single(instruction => Opcode(instruction) == TestHeap);
        if (Operands(allocation) is not [var slots, var live] || Value(slots) != 2 || Value(live) != 2
            || build.Any(instruction => Opcode(instruction) == InitYRegisters)
            || Operands(heap) is not [_, var roots] || Value(roots) != 1)
        {
            throw new InvalidOperationException(WrongFrame);
        }

        object[] control = Body("control");
        int[] effects = control.Select((instruction, index) => (instruction, index))
            .Where(pair => Opcode(pair.instruction) == LocalCall).Select(pair => pair.index).ToArray();
        if (effects is not [var first, var second] || second <= first
            || Array.FindLastIndex(control, instruction => Opcode(instruction) == PutTuple) <= second)
        {
            throw new InvalidOperationException(WrongOrder);
        }

        return Task.CompletedTask;

        object[] Body(string function)
        {
            int start = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Operands(instruction)[1])] == function);

            return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object item) => (byte)Property(Property(item, "Definition"), "Opcode");

    private static object[] Operands(object item) => Items(item, "Operands").ToArray();

    private static int Value(object item) => (int)(BigInteger)Property(item, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
