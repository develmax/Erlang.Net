using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamClosedFieldSelectorChecks
{
    internal const string TestName = "compiler/beam/lays-out-independent-field-selector-arms-in-reverse-postorder";
    private const string Fixture = "beam_closed_field_selector_probe.erl";
    private const string IncorrectBranches = "The closed field selector did not emit the independent target order.";
    private const string IncorrectResults = "A closed atom selector changed branch results.";

    private const string Source = """
        -module(beam_closed_field_selector_probe).
        -export([lookup/3, fallback/3, convert/3, identity/3]).

        lookup(direct, Input, _) -> wrap(Input, [tag]);
        lookup(external, Input, [Column|Pairs]) -> lists:keyfind(Column, Input, Pairs).

        fallback(direct, Input, _) -> wrap(Input, [tag]);
        fallback(missing, _, _) -> exit({missing, field}).

        convert(key, Input, _) -> wrap(Input, [key]);
        convert(params, Input, _) -> alternate(Input, [params]);
        convert(private, Input, _) -> {Input,0}.

        identity(key, Input, _) ->
            Value = decode(Input),
            wrap(Value, [key]);
        identity(params, Input, _) ->
            Value = decode(Input),
            alternate(Value, [params]);
        identity(private, Input, _) ->
            Input.

        wrap(Input, Tag) -> {Input,Tag}.
        alternate(Input, Tag) -> {Tag,Input}.
        decode(Input) -> {decoded,Input}.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        Check("lookup", [IsNonemptyList, TailExternalCall, TailLocalCall]);
        Check("fallback", [TailExternalCall, TailLocalCall]);
        Check("convert", [Return, TailLocalCall, TailLocalCall]);
        Check("identity", [Return, TailLocalCallWithFrame, TailLocalCallWithFrame]);

        var module = BeamModule.Prepare(image, new BeamDependencySelection(["lists"]));
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term pair = Term.Tuple(Term.I(4), Term.A("value"));
            Term tags = Term.List(Term.A("tag"));
            Term contextList = Term.List(Term.I(4), pair);
            if (!pair.Equals(await module.Invoke(
                context,
                "lookup",
                Term.A("external"),
                Term.I(1),
                contextList
            ))
                || !Term.Tuple(Term.I(4), tags).Equals(await module.Invoke(
                    context,
                    "fallback",
                    Term.A("direct"),
                    Term.I(4),
                    Nil.Value
                ))
                || !Term.Tuple(Term.I(4), Term.I(0)).Equals(await module.Invoke(
                    context,
                    "convert",
                    Term.A("private"),
                    Term.I(4),
                    Nil.Value
                ))
                || !Term.I(4).Equals(await module.Invoke(
                    context,
                    "identity",
                    Term.A("private"),
                    Term.I(4),
                    Nil.Value
                )))
            {
                throw new InvalidOperationException(IncorrectResults);
            }

            return Nil.Value;
        });

        void Check(string name, byte[] exits)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands").ToArray() is [_, var atom, _] && atoms[Value(atom)] == name
            );
            if (start < 0)
            {
                throw new InvalidOperationException(IncorrectBranches);
            }

            byte[] body = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
                .Select(Opcode).ToArray();
            if (body.Count(opcode => opcode == SelectValue) != 1 || !body.Where(opcode => exits.Contains(opcode)).SequenceEqual(exits))
            {
                throw new InvalidOperationException(IncorrectBranches);
            }
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
