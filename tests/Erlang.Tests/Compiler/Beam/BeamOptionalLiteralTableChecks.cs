using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamOptionalLiteralTableChecks
{
    internal const string TestName = "compiler/beam/keeps-literal-arguments-in-optional-record-calls";
    private const string Fixture = "beam_optional_literal_table_probe.erl";
    private const string Decode = "decode";
    private const string LiteralTable = "[{zero, 0}, {eight, 8}]";
    private const string IncorrectValue = "An optional call changed its payload, literal arguments, or fallback.";
    private const string IncorrectLayout = "Only the literal optional call should use the compact field join.";

    private const string Source = """
        -module(beam_optional_literal_table_probe).
        -export([decode/2]).

        decode(Input, Tags) ->
            Fields = prepare(Input, Tags),
            {First, Rest} = case Fields of
                [{1, Value1} | Tail1] -> {read_first(Value1, []), Tail1};
                _ -> {missing_first, Fields}
            end,
            {Second, Extra} = case Rest of
                [{2, Value2} | Tail2] -> {read_second(Value2, [{zero, 0}, {eight, 8}], [21]), Tail2};
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
        read_second(Value, Table, Tags) -> {Value, Table, Tags}.
        """;

    internal static async Task Run()
    {
        byte[] literal = CodeGeneration.CompileBeamModule(Source, Fixture);
        byte[] runtimeTable = CodeGeneration.CompileBeamModule(
            Source.Replace(LiteralTable, "Tags", StringComparison.Ordinal), Fixture
        );
        var literalModule = BeamModule.Prepare(literal);
        var dynamicModule = BeamModule.Prepare(runtimeTable);
        Term table = Term.List(Term.Tuple(Term.A("zero"), Term.I(0)), Term.Tuple(Term.A("eight"), Term.I(8)));
        Term lateTable = Term.List(Term.Tuple(Term.A("late"), Term.I(5)));
        Term payload = new BitString([0, 128]);
        Term positive = Term.List(Term.Tuple(Term.I(1), Term.A("first")), Term.Tuple(Term.I(2), payload));
        Term expected = Term.Tuple(Term.A("observed"), Term.A("first"), Term.Tuple(payload, table, Term.List(Term.I(21))));
        Term dynamicExpected = Term.Tuple(
            Term.A("observed"),
            Term.A("first"),
            Term.Tuple(payload, lateTable, Term.List(Term.I(21)))
        );
        Term absent = Term.Tuple(Term.A("observed"), Term.A("missing_first"), Term.A("missing_second"));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            if (!expected.Equals(await literalModule.Invoke(
                context,
                Decode,
                positive,
                Nil.Value
            ))
                || !dynamicExpected.Equals(await dynamicModule.Invoke(
                    context,
                    Decode,
                    positive,
                    lateTable
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

        if (FrameSize(literal) != 3 || FrameSize(runtimeTable) <= 3)
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
