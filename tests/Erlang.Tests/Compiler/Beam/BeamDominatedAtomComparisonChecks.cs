using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamDominatedAtomComparisonChecks
{
    internal const string TestName = "compiler/beam/types-atom-comparisons-on-checked-fallthrough";
    private const string Fixture = "beam_dominated_atom_comparison_probe.erl";
    private const string WrongLayout = "Atom refinement must be limited to the checked operand on the successful guard edge.";
    private const string WrongResult = "Atom comparison typing changed guard rejection or the successful value.";
    private const string Source = """
        -module(beam_dominated_atom_comparison_probe).
        -export([table/2, other/2, bare/2]).

        table(Tab, Key) when is_atom(Tab), Tab /= reserved ->
            {accepted, Tab, Key};
        table(Tab, Key) ->
            {rejected, Tab, Key}.

        other(Tab, Key) when is_atom(Tab), Key /= reserved ->
            {accepted, Tab, Key};
        other(Tab, Key) ->
            {rejected, Tab, Key}.

        bare(Tab, Key) when Tab /= reserved ->
            {accepted, Tab, Key};
        bare(Tab, Key) ->
            {rejected, Tab, Key}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        object code = Code(image);
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        if (TypedComparisons("table") != 1
            || TypedComparisons("other") != 0
            || TypedComparisons("bare") != 0)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        BeamModule module = BeamModule.Prepare(image, new BeamDependencySelection([]));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term accepted = Term.A("accepted");
            Term rejected = Term.A("rejected");
            Term table = Term.A("table");
            Term reserved = Term.A("reserved");
            Term key = Term.I(4);

            if (!Term.Tuple(accepted, table, key).Equals(await module.Invoke(
                context,
                "table",
                table,
                key
            ))
                || !Term.Tuple(rejected, reserved, key).Equals(await module.Invoke(
                    context,
                    "table",
                    reserved,
                    key
                ))
                || !Term.Tuple(rejected, key, key).Equals(await module.Invoke(
                    context,
                    "table",
                    key,
                    key
                ))
                || !Term.Tuple(accepted, table, key).Equals(await module.Invoke(
                    context,
                    "other",
                    table,
                    key
                ))
                || !Term.Tuple(rejected, table, reserved).Equals(await module.Invoke(
                    context,
                    "other",
                    table,
                    reserved
                ))
                || !Term.Tuple(accepted, key, key).Equals(await module.Invoke(
                    context,
                    "bare",
                    key,
                    key
                )))
            {
                throw new InvalidOperationException(WrongResult);
            }

            return Nil.Value;
        });

        int TypedComparisons(string function)
        {
            int start = Array.FindIndex(
                instructions,
                operation => Opcode(operation) == FunctionInfo
                    && atoms[(int)(BigInteger)Property(Items(operation, "Operands").ElementAt(1), "Value")] == function
            );
            if (start < 0)
            {
                throw new InvalidOperationException(WrongLayout);
            }

            return instructions.Skip(start + 1).TakeWhile(operation => Opcode(operation) != FunctionInfo)
                .Where(operation => Opcode(operation) is ExactNotEqualTest or ExactEqualTest)
                .Select(operation => Items(operation, "Operands").ElementAt(1))
                .Count(source => source.GetType().GetProperty("TypeIndex")!.GetValue(source) is int);
        }
    }

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
