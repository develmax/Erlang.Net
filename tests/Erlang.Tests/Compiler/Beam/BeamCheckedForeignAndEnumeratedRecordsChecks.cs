using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCheckedForeignAndEnumeratedRecordsChecks
{
    internal const string TestName = "compiler/beam/checks-foreign-pairs-and-dispatches-enumerated-fields-in-registers";
    private const string Fixture = "beam_checked_foreign_enum_records_probe.erl";
    private const string Peer = "beam_checked_foreign_enum_records_peer";
    private const string IncorrectInstructions = "The record encoder or enumerated decoder lost its compact registers.";
    private const string IncorrectValue = "The record encoder or enumerated decoder returned an unexpected result.";
    private const string MissingBadMatch = "The encoder accepted an unchecked foreign pair.";

    private const string Source = """
        -module(beam_checked_foreign_enum_records_probe).
        -export([encode/2, decode_enum/2, encode_enum/2, run/1]).

        encode(Value, Tags) ->
            {_,Left,Right} = Value,
            {LB,LL} = beam_checked_foreign_enum_records_peer:pair(Left,[]),
            {RB,RL} = beam_checked_foreign_enum_records_peer:pair(Right,[<<2>>]),
            Bytes = [LB,RB],
            Length = LL + RL,
            finish(Tags,Bytes,Length).

        run(L) -> loop(L,[],0).
        loop([], Bytes, Count) -> {lists:reverse(Bytes),Count};
        loop([H|T], Bytes, Count) ->
            {Part,Length} = beam_checked_foreign_enum_records_peer:pair(H, []),
            loop(T,[Part|Bytes],Count + Length).

        decode_enum(Input, Tags) ->
            case decode_number(Input,Tags) of
                0 -> missing;
                1 -> first;
                3 -> third;
                Choice -> exit({error,{asn1,{invalid_enum,Choice}}})
            end.

        encode_enum(Record, Tags) ->
            {_,Value,Optional} = Record,
            {VB,VL} = case Value of
                first -> pack(first,1,[]);
                third -> pack(third,3,[]);
                Kind -> exit({error,{asn1,{invalid_name,Kind}}})
            end,
            {OB,OL} = case Optional of
                absent -> {<<>>,0};
                _ -> option(Optional,[<<2>>])
            end,
            Bytes = [VB,OB],
            Length = VL + OL,
            finish(Tags,Bytes,Length).

        pack(Part, Count, _) -> {Part,Count}.
        option(Part, _) -> {Part,1}.

        decode_number(Input, _) -> byte_size(Input).
        finish(Tags, Bytes, Length) -> {Tags,Bytes,Length}.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        Check("encode", [3], 3);
        Check("loop", [3, 1], 1);
        Check("decode_enum", [0], 0);
        Check("encode_enum", [3], 1);

        await using var runtime = new ProcessRuntime();
        Term bad = Term.A("bad");
        runtime.Modules.Register(
            Peer,
            "pair",
            2,
            (_, args) => ValueTask.FromResult(args[0].Equals(bad) ? bad : Term.Tuple(args[0], Term.I(1)))
        );
        var module = BeamModule.Prepare(image, new BeamDependencySelection([Peer, "lists"]));
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term left = Term.A("a");
            Term right = Term.A("b");
            Term tag = Term.A("record");
            Term values = Term.List(left, right);
            Term encoded = await module.Invoke(
                context,
                "encode",
                Term.Tuple(tag, left, right),
                tag
            );
            Term accumulated = await module.Invoke(context, "run", values);
            Term decoded = await module.Invoke(
                context,
                "decode_enum",
                new BitString([1]),
                Nil.Value
            );
            Term selected = await module.Invoke(
                context,
                "encode_enum",
                Term.Tuple(tag, Term.A("first"), right),
                tag
            );
            if (!Term.Tuple(tag, values, Term.I(2)).Equals(encoded)
                || !Term.Tuple(tag, Term.List(Term.A("first"), right), Term.I(2)).Equals(selected)
                || !Term.Tuple(values, Term.I(2)).Equals(accumulated)
                || !Term.A("first").Equals(decoded))
            {
                throw new InvalidOperationException(IncorrectValue);
            }

            foreach (Term input in new[] { Term.Tuple(tag, bad, right), Term.Tuple(tag, left, bad) })
            {
                try
                {
                    await module.Invoke(
                        context,
                        "encode",
                        input,
                        tag
                    );
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A("badmatch"), bad)))
                {
                    continue;
                }

                throw new InvalidOperationException(MissingBadMatch);
            }

            return Nil.Value;
        });

        void Check(string function, int[] frames, int failures)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && Items(instruction, "Operands").ToArray() is [_, var atom, _]
                    && atoms[Value(atom)] == function
            );
            if (start < 0)
            {
                throw new InvalidOperationException(IncorrectInstructions);
            }

            object[] body = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
            int[] actualFrames = body.Where(instruction => Opcode(instruction) == Allocate)
                .Select(instruction => Value(Items(instruction, "Operands").First())).ToArray();
            if (!frames.SequenceEqual(actualFrames) || body.Count(instruction => Opcode(instruction) == BadMatch) != failures)
            {
                throw new InvalidOperationException(IncorrectInstructions);
            }
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
