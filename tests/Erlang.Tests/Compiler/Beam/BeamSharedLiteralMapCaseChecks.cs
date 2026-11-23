using System.Collections;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamSharedLiteralMapCaseChecks
{
    internal const string TestName = "compiler/beam/shares-literal-map-case-projections";
    private const string Fixture = "beam_shared_literal_map_probe.erl";
    private const string WrongReads = "A literal map case must share the common keys and keep distinct keys branch-local.";
    private const string WrongResult = "The literal map case changed branch order or its original failure payload.";
    private const string Source = """
        -module(beam_shared_literal_map_probe).
        -export([choose/1, different/1, dynamic/2, bound/2]).

        choose(Map) ->
            Choice = case Map of
                #{alpha := A, beta := absent} -> {left, A};
                #{beta := B, alpha := absent} -> {right, B};
                #{alpha := A, beta := B} when is_integer(A), is_integer(B) -> {both, A, B}
            end,
            {chosen, Choice}.

        different(Map) ->
            case Map of
                #{alpha := A, beta := left} -> {left, A};
                #{alpha := A, gamma := G} -> {right, A, G}
            end.

        dynamic(Map, Key) ->
            case Map of
                #{Key := left, beta := B} -> {left, B};
                #{Key := K, beta := B} -> {right, K, B}
            end.

        bound(Map, Expected) ->
            case Map of
                #{alpha := Expected} -> equal;
                #{alpha := Other} -> {other, Other}
            end.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        const string disabledDirective = "-compile(no_ssa_opt).\n-export(";
        byte[] disabled = BeamAdjacentNumericResultChecks.Compile(
            Source.Replace("-export(", disabledDirective, StringComparison.Ordinal),
            disabled: true
        );
        if (LookupCount(image, "choose") != 1 || LookupCount(image, "bound") != 1
            || LookupCount(image, "different") != 3
            || LookupCount(image, "different", "alpha") != 1
            || LookupCount(image, "different", "beta") != 1
            || LookupCount(image, "different", "gamma") != 1
            || LookupCount(disabled, "choose") != 6 || LookupCount(disabled, "bound") != 2)
        {
            throw new InvalidOperationException(WrongReads);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term alpha = Term.A("alpha");
            Term beta = Term.A("beta");
            Term gamma = Term.A("gamma");
            Term absent = Term.A("absent");
            Term one = Term.I(1);
            Term two = Term.I(2);
            Term left = Term.A("left");
            Term right = Term.A("right");
            Term chosen = Term.A("chosen");
            var overlap = new MapTerm([new(alpha, absent), new(beta, absent)]);
            var second = new MapTerm([new(alpha, absent), new(beta, two)]);
            var both = new MapTerm([new(alpha, one), new(beta, two)]);
            var gammaOnly = new MapTerm([new(alpha, one), new(gamma, two)]);
            var all = new MapTerm([new(alpha, one), new(beta, left), new(gamma, two)]);
            var noCommonKey = new MapTerm([new(beta, left), new(gamma, two)]);
            var missing = new MapTerm([new(beta, two)]);
            var unhandled = new MapTerm([new(alpha, left), new(beta, right)]);
            if (!(await module.Invoke(context, "choose", overlap)).Equals(Term.Tuple(chosen, Term.Tuple(left, absent)))
                || !(await module.Invoke(context, "choose", second)).Equals(Term.Tuple(chosen, Term.Tuple(right, two)))
                || !(await module.Invoke(context, "choose", both)).Equals(Term.Tuple(chosen, Term.Tuple(Term.A("both"), one, two)))
                || !(await module.Invoke(context, "different", gammaOnly)).Equals(Term.Tuple(right, one, two))
                || !(await module.Invoke(context, "different", all)).Equals(Term.Tuple(left, one))
                || !(await module.Invoke(
                    context,
                    "dynamic",
                    both,
                    alpha
                )).Equals(Term.Tuple(right, one, two))
                || !(await module.Invoke(
                    context,
                    "bound",
                    both,
                    one
                )).Equals(Term.A("equal"))
                || !(await module.Invoke(
                    context,
                    "bound",
                    both,
                    two
                )).Equals(Term.Tuple(Term.A("other"), one)))
            {
                throw new InvalidOperationException(WrongResult);
            }

            foreach (Term failure in new Term[] { missing, unhandled, absent })
            {
                try
                {
                    await module.Invoke(context, "choose", failure);
                }
                catch (ErlangException ex) when (ex.ExceptionClass == ErlangExceptionClasses.Error
                    && ex.Reason.Equals(Term.Tuple(Term.A("case_clause"), failure)))
                {
                    continue;
                }

                throw new InvalidOperationException(WrongResult);
            }

            try
            {
                await module.Invoke(context, "different", noCommonKey);
            }
            catch (ErlangException ex) when (ex.ExceptionClass == ErlangExceptionClasses.Error
                && ex.Reason.Equals(Term.Tuple(Term.A("case_clause"), noCommonKey)))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(WrongResult);
        });
    }

    private static int LookupCount(byte[] image, string function, string? key = null)
    {
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)(System.Numerics.BigInteger)Property(
                    Items(instruction, "Operands").ElementAt(1),
                    "Value"
                )] == function
        );
        if (start < 0)
        {
            throw new InvalidOperationException(WrongReads);
        }

        var lookups = instructions.Skip(start + 1)
            .TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .Where(instruction => Opcode(instruction) == GetMapElements);

        return key is null
            ? lookups.Count()
            : lookups.SelectMany(instruction => Items(instruction, "Operands").Skip(3)
                    .Where((_, index) => index % 2 == 0))
                .Count(operand => Property(operand, "Kind") is "atom"
                    && atoms[(int)(System.Numerics.BigInteger)Property(operand, "Value")] == key);
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
