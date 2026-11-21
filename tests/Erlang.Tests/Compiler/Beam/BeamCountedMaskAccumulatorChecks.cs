using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCountedMaskAccumulatorChecks
{
    internal const string TestName = "compiler/beam/counted-mask-accumulator";
    private const string Fixture = "beam_counted_mask_accumulator_probe.erl";
    private const string IncorrectSchedule = "The counted mask accumulator changed the arithmetic schedule or register roots.";
    private const string IncorrectResult = "The counted mask accumulator changed its value or failure.";
    private const string Source = """
        -module(beam_counted_mask_accumulator_probe).
        -export([start/1, arbitrary/3]).
        start(Value) when Value =< 31 ->
            {[Value band 31],1};
        start(Value) ->
            counted(Value bsr 5, [Value band 31], 1).
        counted(0, Acc, Count) ->
            {Acc, Count};
        counted(Value, Acc, Count) ->
            counted(Value bsr 5, [Value band 31 bor 32 | Acc], Count + 1).
        arbitrary(Value, Acc, Count) ->
            open_counted(Value, Acc, Count).
        open_counted(0, Acc, Count) ->
            {Acc, Count};
        open_counted(Value, Acc, Count) ->
            open_counted(Value bsr 5, [Value band 31 bor 32 | Acc], Count + 1).
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        CheckInstructions(image);
        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term accumulated = Term.List(Term.I(33), Term.I(32), Term.I(0));
            Term initial = Term.List(Term.I(7));
            Term arbitrary = Term.A("any_counter");
            if (!Term.Tuple(accumulated, Term.I(3)).Equals(await module.Invoke(context, "start", Term.I(1024)))
                || !Term.Tuple(initial, arbitrary).Equals(await module.Invoke(
                    context,
                    "arbitrary",
                    Term.I(0),
                    initial,
                    arbitrary
                )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            try
            {
                _ = await module.Invoke(
                    context,
                    "arbitrary",
                    Term.I(1),
                    initial,
                    arbitrary
                );
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.A(ErlangErrorReasons.BadArithmetic)))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(IncorrectResult);
        });
    }

    private static void CheckInstructions(byte[] image)
    {
        object code = Code(image);
        object[] instructions = Children(code, "Instructions");
        object[] types = Children(Property(code, "Types"), "Entries");
        string[] atoms = Children(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int start = Array.FindIndex(
            instructions,
            item => Opcode(item) == FunctionInfo && atoms[(int)Value(Children(item, "Operands")[1])] == "counted"
        );
        object[] body = instructions.Skip(start + 1).TakeWhile(item => Opcode(item) != FunctionInfo).ToArray();
        var steps = body.Where(item => Opcode(item) == GcBifTwo).Select(item => Children(item, "Operands")).ToArray();
        object? cons = body.FirstOrDefault(item => Opcode(item) == PutList);
        if (body.Any(item => Opcode(item) is Allocate or AllocateHeap or Deallocate)
            || steps.Length != 4 || !steps.Select(step => Value(step[1])).SequenceEqual([3, 4, 4, 4])
            || !steps.Select(step => Value(step[^1])).SequenceEqual([3, 0, 0, 2])
            || cons is null || !Children(cons, "Operands").Select(Value).SequenceEqual([0, 1, 1])
            || steps[2][3] is not { } masked || Property(masked, "TypeIndex") is not int typeIndex
            || (long)Property(types[typeIndex], "Minimum") != 0
            || (long)Property(types[typeIndex], "Maximum") != 31)
        {
            throw new InvalidOperationException(IncorrectSchedule);
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");

    private static object[] Children(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
