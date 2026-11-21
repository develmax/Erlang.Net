using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamOptionalAliasJoinChecks
{
    internal const string TestName = "compiler/beam/joins-multiple-alias-tags-in-an-optional-field";
    private const string Fixture = "beam_optional_alias_join_probe.erl";
    private const string Decode = "decode";
    private const string LiteralTag = "[11]";
    private const string IncorrectValue = "The optional tag dispatch lost the aliased tuple, its arguments, or its default.";
    private const string IncorrectLayout = "Only the proved alias dispatch should use the compact field join.";

    private const string Source = """
        -module(beam_optional_alias_join_probe).
        -export([decode/2]).

        decode(Input, Tags) ->
            Fields = prepare(Input, Tags),
            {First, Rest} = case Fields of
                [{1, Value} | Tail1] -> {read_first(Value, []), Tail1};
                _ -> {missing_first, Fields}
            end,
            {Second, Extra} = case Rest of
                [Whole = {4,_} | Tail2] -> {read_alias(Whole, [11]), Tail2};
                [Whole = {2,_} | Tail2] -> {read_alias(Whole, [22]), Tail2};
                _ -> {missing_second, Rest}
            end,
            case Extra of
                [] -> true;
                _ -> exit({error, {asn1, {unexpected, Extra}}})
            end,
            Result = {observed, First, Second},
            Result.

        prepare(Input, _) -> Input.
        read_first(Value, []) -> Value.
        read_alias(Value, Options) -> {Value,Options}.
        """;

    internal static async Task Run()
    {
        byte[] literal = CodeGeneration.CompileBeamModule(Source, Fixture);
        byte[] runtimeTag = CodeGeneration.CompileBeamModule(
            Source.Replace(LiteralTag, "Tags", StringComparison.Ordinal), Fixture
        );
        var literalModule = BeamModule.Prepare(literal);
        var dynamicModule = BeamModule.Prepare(runtimeTag);
        Term first = Term.Tuple(Term.I(1), Term.A("first"));
        Term highest = Term.Tuple(Term.I(4), Term.A("payload"));
        Term lowest = Term.Tuple(Term.I(2), Term.A("another"));
        Term options = Term.List(Term.I(9));
        Term absent = Term.Tuple(Term.A("observed"), Term.A("missing_first"), Term.A("missing_second"));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            if (!Observed(highest, Term.List(Term.I(11))).Equals(await literalModule.Invoke(
                context,
                Decode,
                Term.List(first, highest),
                Nil.Value
            ))
                || !Observed(lowest, Term.List(Term.I(22))).Equals(await literalModule.Invoke(
                    context,
                    Decode,
                    Term.List(first, lowest),
                    Nil.Value
                ))
                || !Observed(highest, options).Equals(await dynamicModule.Invoke(
                    context,
                    Decode,
                    Term.List(first, highest),
                    options
                ))
                || !absent.Equals(await literalModule.Invoke(
                    context,
                    Decode,
                    Nil.Value,
                    Nil.Value
                )))
            {
                throw new InvalidOperationException(IncorrectValue);
            }

            return Nil.Value;
        });

        if (FrameSize(literal) != 3 || FrameSize(runtimeTag) <= 3)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }
    }

    private static Term Observed(Term tuple, Term options) =>
        Term.Tuple(Term.A("observed"), Term.A("first"), Term.Tuple(tuple, options));

    private static int FrameSize(byte[] image)
    {
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions");
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands") is [_, var name, var arity]
                && atoms[Value(name)] == Decode && Value(arity) == 2
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
