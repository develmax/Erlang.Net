using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCountedPositionPayloadChecks
{
    internal const string TestName = "compiler/beam/constructs-counted-bit-positions-and-forwards-the-proven-payload";
    private const string Fixture = "beam_counted_position_payload_probe.erl";
    private const string Entry = "entry";
    private const string Build = "build";
    private const string Assemble = "assemble";
    private const string ClosedExports = "-export([entry/2]).";
    private const string OpenExports = "-export([entry/2, build/3]).";
    private const string PositivePacker = "pack(Bits) -> {1, unused, Bits}.";
    private const string ZeroPacker = "pack(Bits) -> {0, unused, Bits}.";
    private const string IncorrectValue = "The counted positions, payload, or malformed-input result changed.";
    private const string IncorrectLayout = "Only proven position and payload paths may select the counted lowering.";

    private const string Source = """
        -module(beam_counted_position_payload_probe).
        -export([entry/2]).

        entry([_ | _] = Input, Options) -> assemble(Input, Options, tag).

        assemble([First | Tail], Options, Tags) ->
            Positions = collect([First | Tail], Options, []),
            Size = lists:max(Positions) + 1,
            Bits = build(Size, Positions, 0),
            {Length, Unused, Payload} = pack(Bits),
            finish(Tags, [Unused | Payload], Length + 1).

        collect(Input, Options, _) -> lists:sort(Input ++ Options).
        pack(Bits) -> {1, unused, Bits}.
        finish(Tags, Payload, Length) -> {Tags, Payload, Length}.

        build(0, [], _) -> [];
        build(0, _, _) -> exit({error, repeated_position});
        build(Size, [Position | Tail], Position) ->
            [1 | build(Size - 1, Tail, Position + 1)];
        build(Size, [Head | Tail], Position) ->
            [0 | build(Size - 1, [Head | Tail], Position + 1)];
        build(Size, [], Position) ->
            [0 | build(Size - 1, [], Position + 1)].
        """;

    internal static async Task Run()
    {
        byte[] closedImage = CodeGeneration.CompileBeamModule(Source, Fixture);
        string openSource = Source.Replace(ClosedExports, OpenExports, StringComparison.Ordinal)
            .Replace(PositivePacker, ZeroPacker, StringComparison.Ordinal);
        byte[] openImage = CodeGeneration.CompileBeamModule(openSource, Fixture);
        var dependencies = new BeamDependencySelection(["lists"]);
        var closed = BeamModule.Prepare(closedImage, dependencies);
        var open = BeamModule.Prepare(openImage, dependencies);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term tag = Term.A("tag");
            Term unused = Term.A("unused");
            Term payload = Term.List(
                unused,
                Term.I(1),
                Term.I(0),
                Term.I(0),
                Term.I(1)
            );
            Term first = Term.List(Term.I(3));
            Term second = Term.List(Term.I(0));
            if (!Term.Tuple(tag, payload, Term.I(2)).Equals(await closed.Invoke(
                context,
                Entry,
                first,
                second
            ))
                || !Term.Tuple(tag, payload, Term.I(1)).Equals(await open.Invoke(
                    context,
                    Entry,
                    first,
                    second
                )))
            {
                throw new InvalidOperationException(IncorrectValue);
            }

            try
            {
                _ = await closed.Invoke(
                    context,
                    Entry,
                    first,
                    first
                );
                throw new InvalidOperationException(IncorrectValue);
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Exit
                && error.Reason.Equals(Term.Tuple(Term.A("error"), Term.A("repeated_position"))))
            {
            }

            try
            {
                _ = await open.Invoke(
                    context,
                    Build,
                    Term.I(2),
                    Term.A("not_a_list"),
                    Term.I(0)
                );
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.A(ErlangErrorReasons.FunctionClause)))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(IncorrectValue);
        });

        int[] frames = ReadBody(closedImage, Build).Where(instruction => Opcode(instruction) == Allocate)
            .Select(instruction => Value(Items(instruction, "Operands")[0])).ToArray();
        var closedEncoder = ReadBody(closedImage, Assemble);
        var openEncoder = ReadBody(openImage, Assemble);
        if (frames is not [0, 0, 0]
            || !closedEncoder.Any(instruction => Opcode(instruction) == TrimFrame)
            || openEncoder.Any(instruction => Opcode(instruction) == TrimFrame))
        {
            throw new InvalidOperationException(IncorrectLayout);
        }
    }

    private static object[] ReadBody(byte[] image, string function)
    {
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions");
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands") is [_, var name, _]
                && atoms[Value(name)] == function
        );
        if (start < 0)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
