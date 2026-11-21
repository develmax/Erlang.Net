using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using Plan = Erlang.Compiler.BeamModuleCompiler.PositionedBinaryCasePlan;

namespace Erlang.Tests;

internal static class BeamPositionedBinaryCasePlanChecks
{
    internal const string TestName = "compiler/beam/plans-owned-positioned-case-selectors";
    private const string PlanMismatch = "Unexpected source ownership or discriminator plan.";
    private const string Source = """
        -module(positioned_binary_case_ownership).
        -export([select/1, wider_selector/1, overlap/1, joined_selector/1]).

        select(Input) ->
            case Input of
                <<0:1, Rest/bitstring>> ->
                    Pad = erlang:bit_size(Rest) band 3,
                    {Value, Suffix} =
                        case Rest of
                            <<_:Pad, 0:1, Count:5, Tail/bitstring>> when Count > 0 ->
                                {Count, Tail};
                            <<_:Pad, 1:1, 0:1, Count:6, Tail/bitstring>> when Count > 0 ->
                                {Count + 64, Tail};
                            <<_:Pad, 1:1, 1:1, Count:4, Tail/bitstring>> when Count > 0 ->
                                {Count + 128, Tail}
                        end,
                    {Value, Suffix};
                <<1:1, Other/bitstring>> ->
                    {other, Other}
            end.

        wider_selector(Input) ->
            case Input of
                <<2:2, Rest/bitstring>> ->
                    Pad = erlang:bit_size(Rest) band 1,
                    {Value, Suffix} =
                        case Rest of
                            <<_:Pad, 0:1, 0:1, Count:4, Tail/bitstring>> when Count > 0 ->
                                {Count, Tail};
                            <<_:Pad, 0:1, 1:1, Count:6, Tail/bitstring>> when Count > 0 ->
                                {Count + 64, Tail};
                            <<_:Pad, 1:1, Count:5, Tail/bitstring>> when Count > 0 ->
                                {Count + 128, Tail}
                        end,
                    {Value, Suffix};
                <<_:2, Other/bitstring>> ->
                    {other, Other}
            end.

        overlap(Input) ->
            case Input of
                <<0:1, Rest/bitstring>> ->
                    Pad = erlang:bit_size(Rest) band 3,
                    {Value, Suffix} =
                        case Rest of
                            <<_:Pad, 0:1, Count:5, Tail/bitstring>> when Count > 0 ->
                                {Count, Tail};
                            <<_:Pad, 0:1, Count:5, Tail/bitstring>> ->
                                {fallback, {Count, Tail}};
                            <<_:Pad, 1:1, Count:5, Tail/bitstring>> ->
                                {other, {Count, Tail}}
                        end,
                    {Value, Suffix};
                <<1:1, Other/bitstring>> ->
                    {other, Other}
            end.

        joined_selector(Input) ->
            {Kind, Rest} =
                case Input of
                    <<0:1, Tail/bitstring>> -> {zero, Tail};
                    <<1:1, Tail/bitstring>> -> {one, Tail}
                end,
            Pad = erlang:bit_size(Rest) band 3,
            {Value, Suffix} =
                case Rest of
                    <<_:Pad, 0:1, Count:5, TailAfter/bitstring>> when Count > 0 ->
                        {Count, TailAfter};
                    <<_:Pad, 1:1, 0:1, Count:6, TailAfter/bitstring>> when Count > 0 ->
                        {Count + 64, TailAfter};
                    <<_:Pad, 1:1, 1:1, Count:4, TailAfter/bitstring>> when Count > 0 ->
                        {Count + 128, TailAfter}
                end,
            {Kind, Value, Suffix}.
        """;

    internal static async Task Run()
    {
        ModuleDefinition module = new Parser(Source).ParseModule();
        AssertPrefixes(FindPlans(module, "select"), [(0, 1), (2, 2), (3, 2)]);
        AssertPrefixes(FindPlans(module, "wider_selector"), [(0, 2), (1, 2), (1, 1)]);
        RequireNone(module, "overlap");
        RequireNone(module, "joined_selector");

        string earlierPrefix = Source.Replace(
            "<<_:Pad, 1:1, 0:1, Count:6, Tail/bitstring>>",
            "<<_:Pad, 0:1, 0:1, Count:6, Tail/bitstring>>",
            StringComparison.Ordinal
        );
        RequireNone(new Parser(earlierPrefix).ParseModule(), "select");

        string escaping = Source.Replace("{Value, Suffix};", "{Value, Suffix, Rest};", StringComparison.Ordinal);
        RequireNone(new Parser(escaping).ParseModule(), "select");

        string separateSizeFunction = Source.Replace(
            "-module(positioned_binary_case_ownership).",
            "-module(positioned_binary_case_ownership).\n-compile({no_auto_import,[bit_size/1]}).",
            StringComparison.Ordinal
        ).Replace("erlang:bit_size(Rest)", "bit_size(Rest)", StringComparison.Ordinal)
            + "\nbit_size(_) -> 0.\n";
        RequireNone(new Parser(separateSizeFunction).ParseModule(), "select");

        await CheckPositionedCaseEmission();
    }

    private static async Task CheckPositionedCaseEmission()
    {
        byte[] bytes = CodeGeneration.CompileBeamModule(Source, "positioned_binary_case_ownership.erl");
        byte[] opcodes = ((IEnumerable)Property(Code(bytes), "Instructions")).Cast<object>()
            .Select(instruction => (byte)Property(Property(instruction, "Definition"), "Opcode")).ToArray();
        int[] starts = Enumerable.Range(0, opcodes.Length).Where(index => opcodes[index] == FunctionInfo).ToArray();
        if (starts.Length < 5)
        {
            throw new InvalidOperationException(PlanMismatch);
        }

        for (int index = 0; index < 2; index++)
        {
            byte[] body = opcodes[starts[index]..starts[index + 1]];
            int rollback = Array.IndexOf(body, SetBinaryPosition);
            if (body.Count(opcode => opcode == SkipBinaryBits) != 1 || !body.Contains(GetBinaryPosition)
                || rollback < 0 || !body.Skip(rollback + 1).Contains(CaseEnd))
            {
                throw new InvalidOperationException(PlanMismatch);
            }
        }

        if (opcodes[starts[2]..starts[4]].Contains(SetBinaryPosition))
        {
            throw new InvalidOperationException(PlanMismatch);
        }

        var image = BeamModule.Prepare(bytes);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term tail = new BitString([128], 2);
            Term expectation = Term.Tuple(Term.I(5), tail);
            Term shortInput = new BitString([98, 192], 11);
            Term zeroCount = new BitString([96, 64], 11);
            if (!expectation.Equals(await image.Invoke(context, "select", shortInput))
                || !expectation.Equals(await image.Invoke(context, "wider_selector", new BitString([162, 192], 11)))
                || !Term.Tuple(Term.I(69), new BitString([176], 4)).Equals(
                    await image.Invoke(context, "select", new BitString([112, 182], 15)))
                || !Term.Tuple(Term.I(133), tail).Equals(
                    await image.Invoke(context, "select", new BitString([122, 192], 11)))
                || !Term.Tuple(Term.A("fallback"), Term.Tuple(Term.I(0), tail)).Equals(
                    await image.Invoke(context, "overlap", zeroCount))
                || !Term.Tuple(Term.A("zero"), Term.I(5), tail).Equals(
                    await image.Invoke(context, "joined_selector", shortInput)))
            {
                throw new InvalidOperationException(PlanMismatch);
            }

            await Reject(zeroCount, new BitString([192, 128], 10));
            await Reject(new BitString([96], 7), new BitString([192], 6));
            await Reject(new BitString([]), new BitString([]));

            return Nil.Value;

            async Task Reject(Term input, Term suffix)
            {
                try
                {
                    await image.Invoke(context, "select", input);
                    throw new InvalidOperationException(PlanMismatch);
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A("case_clause"), suffix)))
                {
                }
            }
        });
    }

    private static Plan[] FindPlans(ModuleDefinition module, string name)
    {
        FunctionDefinition function = module.Functions.Single(item => item.Name == name && item.Arity == 1);

        return LocalFunctionEntries.ClauseExpressions(function.Clauses)
            .SelectMany(LocalFunctionEntries.Walk).OfType<Expr.Case>()
            .SelectMany(owner => owner.Clauses.Select(arm =>
                Plan.Read(
                    module,
                    function,
                    owner,
                    arm
                )))
            .OfType<Plan>().ToArray();
    }

    private static void AssertPrefixes(Plan[] plans, (BigInteger Bits, int Width)[] expected)
    {
        if (plans is not [var plan]
            || !plan.Arms.Select(arm => (arm.Prefix, arm.PrefixWidth)).SequenceEqual(expected)
            || !ReferenceEquals(plan.Result.Value, plan.Selector)
            || plan.Arms.Any(arm => arm.Remainder.Segments.Count != 2))
        {
            throw new InvalidOperationException(PlanMismatch);
        }
    }

    private static void RequireNone(ModuleDefinition module, string name)
    {
        if (FindPlans(module, name).Length != 0)
        {
            throw new InvalidOperationException(PlanMismatch);
        }
    }
}
