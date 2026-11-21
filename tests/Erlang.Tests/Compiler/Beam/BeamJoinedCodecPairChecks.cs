using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamJoinedCodecPairChecks
{
    internal const string TestName = "compiler/beam/checks-joined-codec-pairs-and-reuses-counted-frame";
    private const string Fixture = "beam_joined_codec_pair_probe.erl";
    private const string Peer = "beam_joined_codec_pair_peer";
    private const string UnexpectedLayout = "The joined codec result did not retain its checked pair and compact count frame.";
    private const string UnexpectedResult = "The joined codec result changed its value or failure.";

    private const string Source = """
        -module(beam_joined_codec_pair_probe).
        -export([choose/2, collect/1, unpack/2]).

        choose(Value, Tag) ->
            {Payload,Count} = case element(1,Value) of
                local -> maybe_pair(element(2,Value), [<<1>>]);
                foreign -> beam_joined_codec_pair_peer:go(element(2,Value), [<<2>>]);
                Other -> exit({error,{asn1,{invalid_choice_type,Other}}})
            end,
            wrap(Tag,Payload,Count).

        unpack(Initial, Tags) ->
            Parsed = parse(Initial, Tags),
            case (case Parsed of [Only] -> Only; _ -> Parsed end) of
                {1, Value} -> {one, field(Value, [])};
                {2, Value} -> {two, beam_joined_codec_pair_peer:go(Value, [])};
                Other -> {extension, fallback(Other)}
            end.

        parse(Value, _) -> Value.
        field(Value, _) -> Value.
        fallback(Value) -> {unknown,Value}.

        collect(Values) -> loop(Values,[],0).
        loop([], Acc, Count) -> {finish(Acc),Count};
        loop([Head|Tail], Acc, Count) ->
            {Payload,Len} = length_pair(Head,[]),
            loop(Tail,[Payload|Acc],Count+Len).

        maybe_pair(Value,Tag) -> beam_joined_codec_pair_peer:go(Value,Tag).
        length_pair(Value,_) -> {Value,beam_joined_codec_pair_peer:measure(Value)}.
        wrap(Tag,Payload,Count) -> {Tag,Payload,Count}.
        finish(Values) -> Values.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        CheckFrames("choose", [1]);
        CheckFrames("loop", [3, 1]);
        int[] labels = FunctionBody("unpack").Where(instruction => Opcode(instruction) == Label)
            .Select(ReadFirstOperand).TakeLast(3).ToArray();
        if (labels.Length != 3 || labels[1] != labels[0] + 1 || labels[2] != labels[1] + 1)
        {
            throw new InvalidOperationException(UnexpectedLayout);
        }

        await using var runtime = new ProcessRuntime();
        Term malformed = Term.A("malformed");
        runtime.Modules.Register(
            Peer,
            "go",
            2,
            (_, arguments) => ValueTask.FromResult(
                arguments[0].Equals(malformed) ? malformed : Term.Tuple(arguments[0], Term.I(2))
            )
        );
        runtime.Modules.Register(
            Peer,
            "measure",
            1,
            (_, _) => ValueTask.FromResult<Term>(Term.I(1))
        );
        var module = BeamModule.Prepare(image, new BeamDependencySelection([Peer, "erlang"]));
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term first = Term.A("first");
            Term second = Term.A("second");
            Term tag = Term.A("tag");
            Term local = Term.A("local");
            Term foreign = Term.A("foreign");
            Term left = await module.Invoke(
                context,
                "choose",
                Term.Tuple(local, first),
                tag
            );
            Term right = await module.Invoke(
                context,
                "choose",
                Term.Tuple(foreign, second),
                tag
            );
            Term collected = await module.Invoke(context, "collect", Term.List(first, second));
            Term decoded = await module.Invoke(
                context,
                "unpack",
                Term.List(Term.Tuple(Term.I(1), first)),
                tag
            );
            Term extension = await module.Invoke(
                context,
                "unpack",
                Term.List(Term.Tuple(Term.I(3), second)),
                tag
            );
            if (!decoded.Equals(Term.Tuple(Term.A("one"), first))
                || !extension.Equals(Term.Tuple(Term.A("extension"), Term.Tuple(Term.A("unknown"), Term.Tuple(Term.I(3), second))))
                || !left.Equals(Term.Tuple(tag, first, Term.I(2)))
                || !right.Equals(Term.Tuple(tag, second, Term.I(2)))
                || !collected.Equals(Term.Tuple(Term.List(second, first), Term.I(2))))
            {
                throw new InvalidOperationException(UnexpectedResult);
            }

            try
            {
                await module.Invoke(
                    context,
                    "choose",
                    Term.Tuple(local, malformed),
                    tag
                );
                throw new InvalidOperationException(UnexpectedResult);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A("badmatch"), malformed)))
            {
            }

            return Nil.Value;
        });

        void CheckFrames(string name, int[] expected)
        {
            int[] frames = FunctionBody(name).Where(instruction => Opcode(instruction) == Allocate)
                .Select(ReadFirstOperand).ToArray();
            if (!frames.SequenceEqual(expected))
            {
                throw new InvalidOperationException(UnexpectedLayout);
            }
        }

        IEnumerable<object> FunctionBody(string name)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && Items(instruction, "Operands").ToArray() is [_, var atom, _]
                    && atoms[(int)(BigInteger)Property(atom, "Value")] == name
            );
            if (start < 0)
            {
                throw new InvalidOperationException(UnexpectedLayout);
            }

            return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo);
        }
    }

    private static int ReadFirstOperand(object instruction) =>
        (int)(BigInteger)Property(Items(instruction, "Operands").First(), "Value");

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
