using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamNilEqualityCanonicalizationChecks
{
    internal const string TestName = "compiler/beam/canonicalizes-exact-and-loose-empty-list-tests";
    private const string Fixture = "beam_nil_equality_probe.erl";
    private const string MissingNil = "The empty-list guard must use a nil test in both operand orders and nested branches.";
    private const string ChangedNumeric = "Loose equality with a number must retain its numeric comparison.";

    private const string Source = """
        -module(beam_nil_equality_probe).
        -export([right/1, left/1, nested/1, number/1]).

        right(List) when List == [] -> empty;
        right(_) -> present.

        left(List) when [] =:= List -> empty;
        left(_) -> present.

        nested({done,{ok,Tokens},Rest}) ->
            case erlang:length(Tokens) of
                0 when Rest == [] -> complete;
                _ -> incomplete
            end;
        nested(_) -> bad_input.

        number(Value) when Value == 0 -> number;
        number(_) -> other.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        foreach (string name in new[] { "left", "right", "nested" })
        {
            if (!Body(name).Any(i => Opcode(i) == IsNil))
            {
                throw new InvalidOperationException(MissingNil);
            }
        }

        if (!Body("number").Any(i => Opcode(i) == NumericEqualTest))
        {
            throw new InvalidOperationException(ChangedNumeric);
        }

        return Task.CompletedTask;

        object[] Body(string name)
        {
            int start = Array.FindIndex(
                instructions,
                i => Opcode(i) == FunctionInfo
                && atoms[(int)(BigInteger)Property(Items(i, "Operands").ElementAt(1), "Value")] == name
            );

            return instructions.Skip(start + 1).TakeWhile(i => Opcode(i) != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
