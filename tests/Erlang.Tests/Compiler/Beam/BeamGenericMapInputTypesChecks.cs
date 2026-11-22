using System.Collections;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamGenericMapInputTypesChecks
{
    internal const string TestName = "compiler/beam/generic-map-patterns-annotate-proven-inputs";
    private const string WrongType = "Expected the map type on every proved generic map pattern operand.";
    private const string WrongProjection = "Expected each generic map lookup to preserve its individual destination.";
    private const string BadResult = "The map pattern returned an unexpected value.";
    private const int ExpectedMapType = 1 << 6;
    private const string Source = """
        -module(generic_map_input_probe).
        -export([pick/1, nested/1, dynamic/3, optional/1]).

        pick(#{alpha:=A,beta:=B}) -> {A,B};
        pick(_) -> missing.

        nested(#{outer := #{alpha := A}, extra := E}) -> {A,E};
        nested(_) -> missing.

        dynamic(Map,Key,Expected) ->
            case Map of
                #{Key:=Expected, extra:=V} -> V;
                _ -> missing
            end.

        optional(Map) -> maps:get(alpha,Map).
        """;

    internal static async Task Run()
    {
        const string path = "generic_map_input_probe.erl";
        byte[] image = CodeGeneration.CompileBeamModule(Source, path);
        CheckTypes(image, annotated: true);
        const string noSsaDirective = "-compile(no_ssa_opt).\n-export(";
        var disabled = BeamAdjacentNumericResultChecks.Compile(
            Source.Replace("-export(", noSsaDirective, StringComparison.Ordinal),
            disabled: true
        );
        CheckTypes(disabled, annotated: false);

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term alpha = Term.A("alpha");
            Term beta = Term.A("beta");
            Term missing = Term.A("missing");
            Term pair = new MapTerm([new(alpha, Term.I(1)), new(beta, Term.I(2))]);
            Term betaOnly = new MapTerm([new(beta, Term.I(2))]);
            Term inner = new MapTerm([new(alpha, Term.I(8))]);
            Term outer = new MapTerm([new(Term.A("outer"), inner), new(Term.A("extra"), Term.I(9))]);
            Term keys = new MapTerm([new(alpha, Term.I(3)), new(Term.A("extra"), Term.I(4))]);
            if (!(await module.Invoke(context, "pick", pair)).Equals(Term.Tuple(Term.I(1), Term.I(2)))
                || !(await module.Invoke(context, "nested", outer)).Equals(Term.Tuple(Term.I(8), Term.I(9)))
                || !(await module.Invoke(context, "pick", inner)).Equals(missing)
                || !(await module.Invoke(context, "pick", betaOnly)).Equals(missing)
                || !(await module.Invoke(context, "pick", alpha)).Equals(missing)
                || !(await module.Invoke(
                    context,
                    "dynamic",
                    keys,
                    alpha,
                    Term.I(3)
                )).Equals(Term.I(4))
                || !(await module.Invoke(
                    context,
                    "dynamic",
                    keys,
                    alpha,
                    Term.I(1)
                )).Equals(missing))
            {
                throw new InvalidOperationException(BadResult);
            }

            return Nil.Value;
        });
    }

    private static void CheckTypes(byte[] image, bool annotated)
    {
        object code = Code(image);
        object table = Call(Type("BeamTypeTableReader"), "Read", Property(code, "Structure"));
        object[] types = ((IEnumerable)Property(table, "Entries")).Cast<object>().ToArray();
        object[][] projections = ((IEnumerable)Property(code, "Instructions")).Cast<object>()
            .Where(instruction => (byte)Property(Property(instruction, "Definition"), "Opcode") == GetMapElements)
            .Select(instruction => ((IEnumerable)Property(instruction, "Operands")).Cast<object>().ToArray())
            .ToArray();
        if (projections.Length != 7 || projections.Any(operands => operands.Length != 5))
        {
            throw new InvalidOperationException(WrongProjection);
        }

        foreach (object[] operands in projections)
        {
            object input = operands[1];
            object? index = Property(input, "TypeIndex");
            bool valid = annotated
                ? index is int type && (int)Property(types[type], "TypeUnion") == ExpectedMapType
                : index is null;
            if (!valid)
            {
                throw new InvalidOperationException(WrongType);
            }
        }
    }
}
