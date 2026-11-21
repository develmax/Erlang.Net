using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamMeasuredDecoderPairChecks
{
    internal const string TestName = "compiler/beam/resumes-a-measured-binary-only-in-a-closed-decoder";
    private const string Fixture = "beam_measured_decoder_pair_probe.erl";
    private const string Decode = "decode";
    private const string Octets = "octets";
    private const string Resume = "resume";
    private const string IncorrectValue = "Passing a binary context changed the decoded bits or the malformed-input error.";
    private const string IncorrectLayout = "The measured binary context must be used only when all calls enter through the paired decoder.";
    private const string PrivateDecoderExports = "-export([decode/3]).";
    private const string PublicDecoderExports = "-export([decode/3, octets/3]).";

    private const string Source = """
        -module(beam_measured_decoder_pair_probe).
        -export([decode/3]).

        decode(Data, Names, Tags) ->
            case collect(Data, Tags) of
                <<0>> -> [];
                <<Unused, Payload/binary>> ->
                    Bits = octets(byte_size(Payload), Unused, Payload),
                    finish(Bits, Names)
            end.

        collect(Data, _Tags) when is_binary(Data) -> Data.
        finish(Bits, Names) -> {Bits, Names}.

        octets(1, Unused, <<B7:1,B6:1,B5:1,B4:1,B3:1,B2:1,B1:1,B0:1,_/binary>>) ->
            lists:sublist([B7,B6,B5,B4,B3,B2,B1,B0], 8 - Unused);
        octets(Length, Unused, <<B7:1,B6:1,B5:1,B4:1,B3:1,B2:1,B1:1,B0:1,Rest/binary>>) ->
            [B7,B6,B5,B4,B3,B2,B1,B0 | octets(Length - 1, Unused, Rest)].
        """;

    internal static async Task Run()
    {
        byte[] closedImage = CodeGeneration.CompileBeamModule(Source, Fixture);
        byte[] openImage = CodeGeneration.CompileBeamModule(
            Source.Replace(PrivateDecoderExports, PublicDecoderExports, StringComparison.Ordinal),
            Fixture
        );
        var dependencies = new BeamDependencySelection(["lists"]);
        var closed = BeamModule.Prepare(closedImage, dependencies);
        var open = BeamModule.Prepare(openImage, dependencies);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term names = Term.A("names");
            Term tags = Term.A("tags");
            Term total = Term.List(
                Term.I(1),
                Term.I(0),
                Term.I(0),
                Term.I(0),
                Term.I(0),
                Term.I(0),
                Term.I(0),
                Term.I(0),
                Term.I(0),
                Term.I(1)
            );
            Term expected = Term.Tuple(total, names);
            foreach (var module in new[] { closed, open })
            {
                if (!Nil.Value.Equals(await module.Invoke(
                    context,
                    Decode,
                    new BitString([0]),
                    names,
                    tags
                ))
                    || !Term.Tuple(Term.List(Term.I(1)), names).Equals(
                        await module.Invoke(
                            context,
                            Decode,
                            new BitString([7, 128]),
                            names,
                            tags
                        ))
                    || !expected.Equals(await module.Invoke(
                        context,
                        Decode,
                        new BitString([6, 128, 64]),
                        names,
                        tags
                    )))
                {
                    throw new InvalidOperationException(IncorrectValue);
                }

                try
                {
                    _ = await module.Invoke(
                        context,
                        Decode,
                        new BitString([1]),
                        names,
                        tags
                    );
                    throw new InvalidOperationException(IncorrectValue);
                }
                catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                    && error.Reason.Equals(Term.A(ErlangErrorReasons.FunctionClause)))
                {
                }
            }

            if (!Term.List(Term.I(1)).Equals(await open.Invoke(
                context,
                Octets,
                Term.I(1),
                Term.I(7),
                new BitString([128])
            )))
            {
                throw new InvalidOperationException(IncorrectValue);
            }

            return Nil.Value;
        });

        if (!UsesResume(closedImage) || UsesResume(openImage))
        {
            throw new InvalidOperationException(IncorrectLayout);
        }
    }

    private static bool UsesResume(byte[] image)
    {
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions");
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands") is [_, var name, var arity]
                && atoms[Value(name)] == Octets && Value(arity) == 3
        );
        if (start < 0)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .Any(instruction => Opcode(instruction) == StartBinaryMatchKnown
                && Items(instruction, "Operands") is [var mode, ..] && atoms[Value(mode)] == Resume);
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
