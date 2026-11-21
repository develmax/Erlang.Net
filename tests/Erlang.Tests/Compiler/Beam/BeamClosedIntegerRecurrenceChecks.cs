using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamClosedIntegerRecurrenceChecks
{
    internal const string TestName = "compiler/beam/types-only-closed-integer-recurrence-operands";
    private const string Fixture = "beam_closed_integer_recurrence.erl";
    private const string WrongSchedule = "The recursive operand type did not follow the closed integer entry proof.";
    private const string WrongResult = "The recursive operand type changed an Erlang result or exception.";

    private const string Source = """
        -module(beam_closed_integer_recurrence).
        -export([run/1]).
        run(Input) -> loop(Input, 3).
        loop(Input, 0) -> {complete,Input};
        loop(Input, Count) ->
            case read(Input) of
                stop -> {early,Input};
                _ ->
                    touch(Input),
                    loop(Input, Count - 1)
            end.
        read(Value) -> Value.
        touch(Value) -> erlang:put(witness, Value).
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        byte[] open = CodeGeneration.CompileBeamModule(
            Source.Replace("[run/1]", "[run/1,loop/2]", StringComparison.Ordinal),
            Fixture
        );
        byte[] escaping = CodeGeneration.CompileBeamModule(
            Source.Replace("[run/1]", "[run/1,escape/0]", StringComparison.Ordinal) + "\nescape() -> fun loop/2.\n",
            Fixture
        );
        byte[] floatSeed = CodeGeneration.CompileBeamModule(
            Source.Replace("loop(Input, 3)", "loop(Input, 3.0)", StringComparison.Ordinal),
            Fixture
        );
        byte[] floatStep = CodeGeneration.CompileBeamModule(
            Source.Replace("Count - 1", "Count - 1.0", StringComparison.Ordinal),
            Fixture
        );
        byte[] increment = CodeGeneration.CompileBeamModule(
            Source.Replace("Count - 1", "Count + 1", StringComparison.Ordinal),
            Fixture
        );
        var closed = BeamModule.Prepare(image);
        var exposed = BeamModule.Prepare(open);
        var selection = new RuntimeLibrarySelection([RuntimeModuleSelection.Managed("erlang")]);
        await using var runtime = await ProcessRuntime.CreateAsync(selection, RuntimeNativePrimitives.InternalScalars413, CancellationToken.None);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            if (!Term.Tuple(Term.A("complete"), Term.A("step")).Equals(await closed.Invoke(context, "run", Term.A("step")))
                || !Term.Tuple(Term.A("early"), Term.A("stop")).Equals(await closed.Invoke(context, "run", Term.A("stop"))))
            {
                throw new InvalidOperationException(WrongResult);
            }

            try
            {
                await exposed.Invoke(
                    context,
                    "loop",
                    Term.A("step"),
                    Term.A("invalid")
                );
                throw new InvalidOperationException(WrongResult);
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.A("badarith")))
            {
            }

            return Nil.Value;
        });

        if (!IsTypedUpdate(image, "loop") || IsTypedUpdate(open, "loop")
            || IsTypedUpdate(escaping, "loop") || IsTypedUpdate(floatSeed, "loop") || IsTypedUpdate(floatStep, "loop")
            || IsTypedUpdate(increment, "loop"))
        {
            throw new InvalidOperationException(WrongSchedule);
        }
    }

    private static bool IsTypedUpdate(byte[] image, string name)
    {
        var module = BeamModule.Prepare(image);
        object code = typeof(BeamModule).GetField(BeamBinaryHostNames.CodeField, System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.NonPublic)!.GetValue(module)!;
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions");
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands") is [_, var nameAtom, _]
                && atoms[(int)(BigInteger)Property(nameAtom, "Value")] == name
        );
        if (start < 0)
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        object[] updates = instructions.Skip(start + 1).TakeWhile(item => Opcode(item) != FunctionInfo)
            .Where(item => Opcode(item) == GcBifTwo).ToArray();
        if (updates.Length != 1)
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        object operand = Items(updates[0], "Operands")[3];

        return operand.GetType().GetProperty("TypeIndex")!.GetValue(operand) is not null;
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
