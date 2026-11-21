using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamClosedMapTailContinuationChecks
{
    internal const string TestName = "compiler/beam/keeps-a-closed-mappers-input-in-an-outgoing-register";
    private const string Fixture = "beam_closed_map_tail_continuation_probe.erl";
    private const string IncorrectFrame = "A closed map callback still spilled the original list.";
    private const string IncorrectResults = "A closed or captured map callback changed its values.";

    private const string Source = """
        -module(beam_closed_map_tail_continuation_probe).
        -export([ordered/1, captured/1]).
        ordered(Input) ->
            Values = lists:map(fun(L) when is_list(L) -> list_to_binary(L);
                                  (B) -> B
                               end, Input),
            lists:sort(Values).
        captured(Input) ->
            Values = lists:map(fun(Value) -> {Value, length(Input)} end, Input),
            lists:sort(Values).
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        object[] ordered = Read("ordered");
        object[] allocations = ordered.Where(instruction => Opcode(instruction) == AllocateHeap).ToArray();
        int move = Array.FindIndex(ordered, instruction => Opcode(instruction) == Move
            && Items(instruction, "Operands").Select(Value).ToArray() is [0, 1]);
        int closure = Array.FindIndex(ordered, instruction => Opcode(instruction) == MakeFun);
        if (allocations is not [var allocation] || Value(Items(allocation, "Operands").First()) != 0
            || move < 0 || move >= closure || ordered.Any(instruction => Opcode(instruction) == InitYRegisters))
        {
            throw new InvalidOperationException(IncorrectFrame);
        }

        var module = BeamModule.Prepare(image, new BeamDependencySelection(["lists"]));
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term a = new BitString([1]);
            Term b = new BitString([2]);
            Term values = Term.List(b, Term.List(Term.I(1)));
            Term numbers = Term.List(Term.I(3), Term.I(1));
            if (!Term.List(a, b).Equals(await module.Invoke(context, "ordered", values))
                || !Nil.Value.Equals(await module.Invoke(context, "ordered", Nil.Value))
                || !Term.List(Term.Tuple(Term.I(1), Term.I(2)), Term.Tuple(Term.I(3), Term.I(2))).Equals(
                    await module.Invoke(context, "captured", numbers)))
            {
                throw new InvalidOperationException(IncorrectResults);
            }

            return Nil.Value;
        });

        object[] Read(string function)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && Items(instruction, "Operands").ToArray() is [_, var atom, var arity]
                    && atoms[Value(atom)] == function && Value(arity) == 1
            );
            if (start < 0)
            {
                throw new InvalidOperationException(IncorrectFrame);
            }

            return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
