using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamClosedCompoundPatternChecks
{
    internal const string TestName = "compiler/beam/folds-closed-compound-patterns";
    private const string Fixture = "beam_closed_compound_pattern_probe.erl";
    private const string MissingClosedTests = "Expected one exact test for each closed compound pattern.";

    internal const string Source = """
        -module(beam_closed_compound_pattern_probe).
        -export([nested/1, list/1, tagged/1, overlap/1, disjoint_heads/1, overlapping_heads/1, select/1]).

        nested(Value) ->
            case ?MODULE:select(Value) of
                {outer,{inner,[4,{label,5}|done]},_} -> folded;
                {second,{inner,[4,{label,Open}|done]},_} -> {open,Open};
                Other -> {other,Other}
            end.

        list(Value) ->
            case ?MODULE:select(Value) of
                [1,{tag,2}] -> folded;
                Other -> {other,Other}
            end.

        tagged(Value) ->
            case ?MODULE:select(Value) of
                {label,3} -> folded;
                _ -> other
            end.

        overlap(Value) ->
            case ?MODULE:select(Value) of
                {label,3} -> folded;
                {label,Open} -> {open,Open};
                Other -> {other,Other}
            end.

        disjoint_heads(Value) ->
            case ?MODULE:select(Value) of
                [marker] -> marker;
                [{head,Data}, tail_only] -> {matched,Data};
                Other -> Other
            end.

        overlapping_heads(Value) ->
            case ?MODULE:select(Value) of
                [{head,Data}, retained_tail] -> {matched,Data};
                [{head,Data}, Open] -> {open,Data,Open};
                Other -> Other
            end.

        select(Value) -> Value.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static void Run()
    {
        object code = Code(CompileFixture());
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] nested = Body("nested");
        object[] list = Body("list");
        object[] tagged = Body("tagged");
        object[] overlap = Body("overlap");
        object[] disjointHeads = Body("disjoint_heads");
        object[] overlappingHeads = Body("overlapping_heads");
        var literals = Items(Property(code, "Structure"), "Literals").Cast<Term>().ToArray();
        if (!nested.Zip(nested.Skip(1)).Any(pair => IsNestedExactTest(pair.First, pair.Second))
            || !nested.Any(instruction => Opcode(instruction) == IsNonemptyList)
            || !list.Any(instruction => Opcode(instruction) == ExactEqualTest)
            || list.Any(instruction => Opcode(instruction) == IsNonemptyList)
            || !tagged.Any(instruction => Opcode(instruction) == ExactEqualTest)
            || tagged.Any(instruction => Opcode(instruction) == GetTupleElement)
            || !overlap.Any(instruction => Opcode(instruction) == GetTupleElement)
            || !disjointHeads.Any(instruction => Opcode(instruction) == GetList)
            || !literals.Contains(Term.List(Term.A("tail_only")))
            || !overlappingHeads.Any(instruction => Opcode(instruction) == IsNonemptyList)
            || literals.Contains(Term.List(Term.A("retained_tail"))))
        {
            throw new InvalidOperationException(MissingClosedTests);
        }

        object[] Body(string name)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Items(instruction, "Operands").ElementAt(1))] == name
            );

            return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static bool IsNestedExactTest(object projection, object test)
    {
        if (Opcode(projection) != GetTupleElement || Opcode(test) != ExactEqualTest)
        {
            return false;
        }

        object[] fields = Items(projection, "Operands").ToArray();
        object operand = Items(test, "Operands").ElementAt(1);

        return Value(fields[1]) == 1 && Value(fields[2]) == Value(operand)
            && Equals(Property(fields[2], "Kind"), Property(operand, "Kind"));
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
