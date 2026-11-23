using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamGuardedMaskAccumulatorEntryChecks
{
    internal const string TestName = "compiler/beam/guarded-mask-accumulator-entry";
    private const string Fixture = "beam_guarded_mask_accumulator_probe.erl";
    private const string IncorrectSchedule = "The guarded mask entry did not preserve its register-only branch schedule.";
    private const string IncorrectResult = "The guarded mask entry did not preserve its result or arithmetic failure.";
    private const string Source = """
        -module(beam_guarded_mask_accumulator_probe).
        -export([entry/1, with_alternative/1]).
        entry(V) when V =< 31 ->
            {[255 band V], 1};
        entry(V) ->
            counted(V bsr 5, [V band 31], 1).
        counted(0, Acc, Count) ->
            {Acc, Count};
        counted(V, Acc, Count) ->
            counted(V bsr 5, [V band 31 bor 32 | Acc], Count + 1).
        with_alternative(V) when V > 31 ->
            {[255 band V], 1};
        with_alternative(V) ->
            counted(V bsr 5, [V band 31], 1).
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        CheckInstructions(image);
        BeamModule module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term one = Term.Tuple(Term.List(Term.I(31)), Term.I(1));
            Term two = Term.Tuple(Term.List(Term.I(33), Term.I(0)), Term.I(2));
            if (!one.Equals(await module.Invoke(context, "entry", Term.I(31)))
                || !two.Equals(await module.Invoke(context, "entry", Term.I(32)))
                || !Term.Tuple(Term.List(Term.I(32)), Term.I(1)).Equals(await module.Invoke(context, "with_alternative", Term.I(32))))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            await ExpectArithmeticFailure(Term.A("not_a_number"));
            await ExpectArithmeticFailure(new FloatTerm(2.5));
            await ExpectArithmeticFailure(new FloatTerm(31.5));

            return Nil.Value;

            async Task ExpectArithmeticFailure(Term term)
            {
                try
                {
                    _ = await module.Invoke(context, "entry", term);
                }
                catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                    && error.Reason.Equals(Term.A(ErlangErrorReasons.BadArithmetic)))
                {
                    return;
                }

                throw new InvalidOperationException(IncorrectResult);
            }
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
            item => Opcode(item) == FunctionInfo && atoms[(int)Value(Children(item, "Operands")[1])] == "entry"
        );
        object[] body = instructions.Skip(start + 1).TakeWhile(item => Opcode(item) != FunctionInfo).ToArray();
        var steps = body.Where(item => Opcode(item) == GcBifTwo).Select(item => Children(item, "Operands")).ToArray();
        if (body.Any(item => Opcode(item) is Allocate or AllocateHeap or Deallocate)
            || steps.Length != 3 || !steps.Select(step => Value(step[1])).SequenceEqual([1, 1, 2])
            || !steps.Select(step => Value(step[^1])).SequenceEqual([0, 1, 0])
            || body.Count(item => Opcode(item) == Swap) != 1 || body.Count(item => Opcode(item) == TailLocalCall) != 1
            || Property(steps[0][3], "TypeIndex") is not int shortType
            || (long)Property(types[shortType], "Maximum") != 31
            || Property(steps[2][3], "TypeIndex") is not int longType
            || (long)Property(types[longType], "Minimum") != 31)
        {
            throw new InvalidOperationException(IncorrectSchedule);
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");

    private static object[] Children(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
