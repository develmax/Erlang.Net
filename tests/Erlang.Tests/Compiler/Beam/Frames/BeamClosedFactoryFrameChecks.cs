using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamClosedFactoryFrameChecks
{
    internal const string TestName = "compiler/beam/closed-factory-empty-frame";
    private const string Fixture = "beam_closed_factory_frame_probe.erl";
    private const string WrongFrame = "The closure factory has incorrect frame ownership.";
    private const string WrongValue = "The closure factory produced the wrong result.";

    private const string Source = """
        -module(beam_closed_factory_frame_probe).
        -export([choose/1, pick/2, captured/1, capture/2, ordered/1, invoke_ordered/2]).
        choose(Id) when Id =:= {2,7,1} -> fun first/1;
        choose(Id) when Id =:= {2,7,2} -> fun second/1;
        choose(_) -> fun(Value) -> {other,Value} end.
        pick(Id, Value) ->
            Selected = choose(Id),
            Selected(Value).
        captured(Id) when Id =:= {2,7,1} -> fun(Value) -> {Id,Value} end;
        captured(_) -> fun first/1.
        capture(Id, Value) ->
            Selected = captured(Id),
            Selected(Value).
        ordered(Value) ->
            record_value(Value),
            fun first/1.
        invoke_ordered(Key, Other) ->
            Selected = ordered(Key),
            {get(factory_log),Selected(Other)}.
        record_value(Value) -> put(factory_log,Value).
        first(Value) -> {first,Value}.
        second(Value) -> {second,Value}.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] beam = CompileFixture();
        CheckInstructions(beam);
        var module = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term id = Term.Tuple(Term.I(2), Term.I(7), Term.I(1));
            Term second = Term.Tuple(Term.I(2), Term.I(7), Term.I(2));
            Term value = Term.A("value");
            Term missing = Term.A("missing");
            Term selected = Term.Tuple(Term.A("first"), value);
            if (!selected.Equals(await module.Invoke(
                context,
                "pick",
                id,
                value
            ))
                || !Term.Tuple(Term.A("second"), value).Equals(await module.Invoke(
                    context,
                    "pick",
                    second,
                    value
                ))
                || !Term.Tuple(Term.A("other"), value).Equals(await module.Invoke(
                    context,
                    "pick",
                    missing,
                    value
                ))
                || !Term.Tuple(id, value).Equals(await module.Invoke(
                    context,
                    "capture",
                    id,
                    value
                ))
                || !Term.Tuple(missing, selected).Equals(await module.Invoke(
                    context,
                    "invoke_ordered",
                    missing,
                    value
                )))
            {
                throw new InvalidOperationException(WrongValue);
            }

            return Nil.Value;
        });
    }

    private static void CheckInstructions(byte[] beam)
    {
        object code = Code(beam);
        object[] instructions = Children(code, "Instructions").ToArray();
        string[] atoms = Children(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] choose = Function("choose");
        object[] captured = Function("captured");
        object[] ordered = Function("ordered");
        if (choose.Any(instruction => Opcode(instruction) is Allocate or AllocateHeap or Deallocate)
            || choose.Count(instruction => Opcode(instruction) == MakeFun) != 3
            || choose.Where(instruction => Opcode(instruction) == TestHeap)
                .Any(instruction => Value(Children(instruction, "Operands").Last()) != 0)
            || !captured.Any(instruction => Opcode(instruction) == MakeFun && Children(instruction, "Operands").Count() > 3)
            || !ordered.Any(instruction => Opcode(instruction) is Allocate or AllocateHeap)
            || !ordered.Any(instruction => Opcode(instruction) is LocalCall or ExternalCall))
        {
            throw new InvalidOperationException(WrongFrame);
        }

        object[] Function(string name)
        {
            int start = Array.FindIndex(instructions, item => Opcode(item) == FunctionInfo
                && atoms[(int)Value(Children(item, "Operands").ElementAt(1))] == name);

            return instructions.Skip(start + 1).TakeWhile(item => Opcode(item) != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Children(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
