using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamNestedSavedRecordChecks
{
    internal const string TestName = "compiler/beam/retains-checked-saved-records-through-local-flow";
    private const string Fixture = "beam_nested_saved_record_probe.erl";
    private const string WrongShape = "The checked record must retain its shape only on proven paths.";
    private const string WrongValue = "The nested record proof changed the field or the rejected record.";
    private const string Source = """
        -module(beam_nested_saved_record_probe).
        -export([read/2, read_siblings/2]).
        -record(entry, {payload}).

        read(Value, Flag) ->
            case keep(Value) of
                {ok, #entry{} = Record} ->
                    case choose(Flag) of
                        ready -> Record#entry.payload;
                        skipped -> skipped
                    end;
                Other -> Other
            end.

        read_siblings(Value, Flag) ->
            case keep(Value) of
                {ok, #entry{} = Record} ->
                    case choose(Flag) of
                        ready -> Record#entry.payload;
                        skipped -> skipped
                    end;
                {other, Record} -> Record#entry.payload;
                Other -> Other
            end.

        keep(Value) -> Value.
        choose(run) -> ready;
        choose(_) -> skipped.
        """;

    internal static async Task Run()
    {
        byte[] expected = PhysicalProgram(includeSecondCheck: false);
        byte[] actual = PhysicalProgram();
        if (Rewrite(actual) is not { } rewritten || !rewritten.AsSpan().SequenceEqual(expected)
            || Rewrite(PhysicalProgram(allowUncheckedEntry: true)) is not null
            || Rewrite(PhysicalProgram(overwriteRecord: true)) is not null
            || Rewrite(PhysicalProgram(otherTag: true)) is not null)
        {
            throw new InvalidOperationException(WrongShape);
        }

        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        if (CountTagTests(image, "read") != 1 || CountTagTests(image, "read_siblings") < 2)
        {
            throw new InvalidOperationException(WrongShape);
        }

        BeamModule module = BeamModule.Prepare(image, new BeamDependencySelection([]));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term payload = Term.I(21);
            Term record = Term.Tuple(Term.A("entry"), payload);
            Term input = Term.Tuple(Term.A("ok"), record);
            Term run = Term.A("run");
            Term invalid = Term.A("invalid");
            Term unmatched = Term.Tuple(Term.A("ok"), invalid);
            Term badRecord = Term.Tuple(Term.A("other"), invalid);
            if (!payload.Equals(await module.Invoke(
                context,
                "read",
                input,
                run
            ))
                || !Term.A("skipped").Equals(await module.Invoke(
                    context,
                    "read",
                    input,
                    Term.A("skip")
                ))
                || !unmatched.Equals(await module.Invoke(
                    context,
                    "read",
                    unmatched,
                    run
                ))
                || !payload.Equals(await module.Invoke(
                    context,
                    "read_siblings",
                    Term.Tuple(Term.A("other"), record),
                    run
                )))
            {
                throw new InvalidOperationException(WrongValue);
            }

            try
            {
                await module.Invoke(
                    context,
                    "read_siblings",
                    badRecord,
                    run
                );
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.Tuple(Term.A("badrecord"), invalid)))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(WrongValue);
        });
    }

    private static byte[] PhysicalProgram(
        bool includeSecondCheck = true,
        bool allowUncheckedEntry = false,
        bool overwriteRecord = false,
        bool otherTag = false
    )
    {
        const int join = 3;
        const int firstFailure = 4;
        const int secondFailure = 5;
        const int atomTag = 3;
        const int otherAtomTag = 4;
        byte[] entry = Start(
            1,
            2,
            2,
            2
        );
        byte[] write = overwriteRecord ? [Move, X(0), Y(0)] : [];
        byte[] laterCheck = includeSecondCheck
            ? [IsTaggedTuple, F(secondFailure), Y(0), U(2), A(otherTag ? otherAtomTag : atomTag)]
            : [];

        return
        [
            .. entry,
            Allocate, U(1), U(2), Move, X(0), Y(0),
            IsTaggedTuple, F(allowUncheckedEntry ? join : firstFailure), Y(0), U(2), A(atomTag),
            BifOne, F(join), U(0), X(1), X(1), Jump, F(join),
            Label, U(join), ExternalCall, U(1), U(0), .. write, .. laterCheck,
            GetTupleElement, Y(0), U(1), X(0), Deallocate, U(1), Return,
            Label, U(firstFailure), BadRecord, Y(0), Label, U(secondFailure), BadRecord, Y(0), EndCode
        ];
    }

    private static byte[]? Rewrite(byte[] instructions)
    {
        ReadOnlyMemory<byte> code = instructions;
        object header = New(
            "BeamCodeHeader",
            0,
            (int)BadRecord,
            6,
            1,
            code
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            5,
            0,
            null,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "RemoveRepeatedSavedRecordChecks",
            code,
            body
        );
    }

    private static int CountTagTests(byte[] image, string function)
    {
        object code = Code(image);
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int entry = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo && Name(Items(instruction, "Operands").ElementAt(1)) == function
        );
        if (entry < 0)
        {
            throw new InvalidOperationException(WrongShape);
        }

        return instructions.Skip(entry + 1)
            .TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .Count(instruction => Opcode(instruction) == IsTaggedTuple
                && Name(Items(instruction, "Operands").ElementAt(3)) == "entry");

        string Name(object operand) => atoms[(int)(BigInteger)Property(operand, "Value")];
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
