using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamRetainedRecordEncoderChecks
{
    internal const string TestName = "compiler/beam/retains-input-and-proved-pairs-in-conditional-record-encoding";
    private const string Fixture = "beam_retained_record_encoder_probe.erl";
    private const string Entry = "encode";
    private const string KnownPair = "pack(Value, []) -> {Value, 1}.";
    private const string IncorrectValue = "Retained record encoding changed a field, an omitted value, or an error.";
    private const string IncorrectLayout = "Only proved pairs should use the compact retained record frame.";

    private const string Source = """
        -module(beam_retained_record_encoder_probe).
        -export([encode/2]).

        encode(Input, Tags) ->
            {_, First, Second, Third} = Input,
            {Bytes1, Len1} = pack(First, []),
            {Bytes2, Len2} = case skipped(Second) of
                true -> {[], 0};
                false -> pack(Second, [])
            end,
            {Bytes3, Len3} = case Third of
                missing -> {<<>>, 0};
                _ -> pack(Third, [])
            end,
            Pieces = [Bytes1, Bytes2, Bytes3],
            Length = Len1 + Len2 + Len3,
            finish(Tags, Pieces, Length).

        pack(Value, []) -> {Value, 1}.
        skipped(missing) -> true;
        skipped(_) -> false.
        finish(Tags, Parts, Length) -> {Tags, Parts, Length}.
        """;

    internal static async Task Run()
    {
        byte[] known = CodeGeneration.CompileBeamModule(Source, Fixture);
        byte[] unknown = CodeGeneration.CompileBeamModule(
            Source.Replace(KnownPair, "pack(Value, []) -> Value.", StringComparison.Ordinal), Fixture
        );
        var knownModule = BeamModule.Prepare(known);
        var unknownModule = BeamModule.Prepare(unknown);
        Term marker = Term.A("record");
        Term first = Term.A("one");
        Term second = Term.A("two");
        Term third = Term.A("three");
        Term missing = Term.A("missing");
        Term supplied = Term.Tuple(
            marker,
            first,
            second,
            third
        );
        Term omitted = Term.Tuple(
            marker,
            first,
            missing,
            missing
        );
        Term expected = Term.Tuple(Nil.Value, Term.List(first, second, third), Term.I(3));
        Term expectedOmission = Term.Tuple(Nil.Value, Term.List(first, Nil.Value, new BitString([])), Term.I(1));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            if (!expected.Equals(await knownModule.Invoke(
                context,
                Entry,
                supplied,
                Nil.Value
            ))
                || !expectedOmission.Equals(await knownModule.Invoke(
                    context,
                    Entry,
                    omitted,
                    Nil.Value
                )))
            {
                throw new InvalidOperationException(IncorrectValue);
            }

            try
            {
                await unknownModule.Invoke(
                    context,
                    Entry,
                    supplied,
                    Nil.Value
                );
                throw new InvalidOperationException(IncorrectValue);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A("badmatch"), first)))
            {
            }

            return Nil.Value;
        });

        if (FrameSize(known) != 4 || FrameSize(unknown) <= 4)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }
    }

    private static int FrameSize(byte[] image)
    {
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions");
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands") is [_, var name, var arity]
                && atoms[Value(name)] == Entry && Value(arity) == 2
        );
        if (start < 0)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object allocate = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .Single(instruction => Opcode(instruction) is Allocate or AllocateHeap);

        return Value(Items(allocate, "Operands")[0]);
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
