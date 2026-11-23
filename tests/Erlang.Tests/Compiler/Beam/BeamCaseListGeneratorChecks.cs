using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCaseListGeneratorChecks
{
    internal const string TestName = "compiler/beam/lifts-case-list-elements-from-proven-lexical-scope";
    private const string Fixture = "beam_case_list_generator_probe.erl";
    private const string MissingHelper = "A supported case element did not produce its expected lexical helper.";
    private const string InvalidValue = "The lifted case helper changed a branch or captured match.";

    internal const string Source = """
        -module(beam_case_list_generator_probe).
        -export([route/2, classify/2, unwrap/1]).

        route(List, Receiver) ->
            [case lookup(Item, Receiver) of
                 {found, Value} -> {known, Value};
                 missing -> {missing, Item}
             end || Item <- List].

        classify(List, Expected) ->
            [case Item of
                 Expected -> {same, Item};
                 {value, Fresh} when is_integer(Fresh) -> {number, Fresh};
                 Other -> {other, Other}
             end || Item <- List].

        unwrap(List) ->
            [case Item of {ok, Value} -> Value end || Item <- List].

        lookup({input, Value}, Receiver) ->
            Receiver ! {seen, Value},
            {found, Value};
        lookup(Item, Receiver) ->
            Receiver ! {seen, Item},
            missing.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        var functions = Items(code, "Instructions").Where(instruction => Opcode(instruction) == BeamEmissionFormat.FunctionInfo)
            .Select(instruction => Items(instruction, "Operands").ToArray())
            .Select(operands => (atoms[Value(operands[1])], Value(operands[2]))).ToHashSet();
        if (!functions.Contains(("-route/2-lc$^0/1-0-", 2))
            || !functions.Contains(("-classify/2-lc$^0/1-0-", 2))
            || !functions.Contains(("-unwrap/1-lc$^0/1-0-", 1)))
        {
            throw new InvalidOperationException(MissingHelper);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term expected = Term.A("expected");
            Term first = Term.Tuple(Term.A("value"), Term.I(1));
            Term second = Term.Tuple(Term.A("value"), expected);
            Term classified = Term.List(
                Term.Tuple(Term.A("same"), expected),
                Term.Tuple(Term.A("number"), Term.I(1)),
                Term.Tuple(Term.A("other"), second)
            );
            if (!classified.Equals(await module.Invoke(context, "classify", [Term.List(expected, first, second), expected]))
                || !Term.List(first).Equals(await module.Invoke(context, "unwrap", [Term.List(Term.Tuple(Term.A("ok"), first))])))
            {
                throw new InvalidOperationException(InvalidValue);
            }

            return Nil.Value;
        });
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
