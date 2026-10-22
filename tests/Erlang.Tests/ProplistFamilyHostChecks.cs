using Erlang.Compiler;
using Erlang.Differential;

namespace Erlang.Tests;

internal static class ProplistFamilyHostChecks
{
    internal static Task Direct() => Run(false);

    internal static Task Compiled() => Run(true);

    internal static bool Owns(string module, string function, int arity)
        => module == ProplistFamilyTestNames.Module && function != ProplistFamilyTestNames.GetValue
            && ProplistFamilyTestData.Cases.Any(row => row.Function == function && row.Arity == arity);

    internal static async Task Mfa(string function, int arity)
    {
        var row = ProplistFamilyTestData.Cases.First(item => item.Function == function && item.Arity == arity && item.Error is null);
        await using var runtime = new ProcessRuntime();
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        await owner.InvokeAsync(context => Check(
            row,
            runtime,
            context,
            null
        )).WaitAsync(ProplistFamilyTestNames.Deadline);
    }

    private static async Task Run(bool compiledLane)
    {
        using var compiled = compiledLane ? GeneratedModuleCompiler.Compile(
            ProplistFamilyTestData.Read(ProplistFamilyTestNames.ProbeResource), ProplistFamilyTestNames.ProbeFile) : null;
        await using var runtime = new ProcessRuntime();
        compiled?.Definition.Register(runtime.Modules);
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        var failures = new List<Exception>();
        foreach (var row in ProplistFamilyTestData.Cases)
        {
            try
            {
                await owner.InvokeAsync(context => Check(
                    row,
                    runtime,
                    context,
                    compiled?.Definition
                )).WaitAsync(ProplistFamilyTestNames.Deadline);
            }
            catch (Exception error)
            {
                failures.Add(new InvalidOperationException(row.Name, error));
            }
        }
        if (!compiledLane)
        {
            foreach (var signature in ProplistFamilyTestData.Cases.Select(row => (row.Function, row.Arity)).Distinct())
            {
                if (!runtime.Modules.Exports.Any(value => value.Module == ProplistFamilyTestNames.Module
                    && value.Function == signature.Function && value.Arity == signature.Arity))
                {
                    failures.Add(new InvalidOperationException(ProplistFamilyTestNames.MissingExport + signature));
                }
            }
            foreach (var group in ProplistFamilyTestData.Cases.GroupBy(row => row.Function))
            {
                foreach (int arity in new[] { 0, group.Max(row => row.Arity) + 1 })
                {
                    try
                    {
                        Term rejectedTerm = await owner.InvokeAsync(async context =>
                        {
                            try
                            {
                                await runtime.Modules.Call(
                                    context,
                                    ProplistFamilyTestNames.Module,
                                    group.Key,
                                    Enumerable.Repeat<Term>(Nil.Value, arity).ToArray()
                                );

                                return Term.A(ErlangBooleanAtoms.False);
                            }
                            catch (ErlangException error) when (error.Reason.Equals(Term.A(ProplistFamilyTestNames.Undefined)))
                            {
                                return Term.A(ErlangBooleanAtoms.True);
                            }
                        }).WaitAsync(ProplistFamilyTestNames.Deadline);
                        bool rejected = rejectedTerm.Equals(Term.A(ErlangBooleanAtoms.True));
                        if (!rejected)
                        {
                            failures.Add(new InvalidOperationException(ProplistFamilyTestNames.WrongArity + group.Key + arity));
                        }
                    }
                    catch (Exception error)
                    {
                        failures.Add(error);
                    }
                }
            }
        }
        if (failures.Count != 0)
        {
            throw new AggregateException(ProplistFamilyTestNames.Failure, failures);
        }
    }

    private static async Task<Term> Check(
        ProplistFamilyTestCase row,
        ProcessRuntime runtime,
        ProcessContext context,
        ModuleDefinition? compiled
    )
    {
        async Task<Term> Literal(string expression) => await Execution.EvaluateAsync(new Parser(expression).ParseExpression(), context);
        Term expected = await Literal(row.Error ?? row.Expected!);
        Term actual;
        try
        {
            if (compiled is null)
            {
                var arguments = new List<Term>();
                foreach (string argument in row.Arguments)
                {
                    arguments.Add(await Literal(argument));
                }
                actual = await runtime.Modules.Call(
                    context,
                    ProplistFamilyTestNames.Module,
                    row.Function,
                    arguments.ToArray()
                );
            }
            else
            {
                actual = await Execution.InvokeAsync(
                    compiled,
                    ProplistFamilyTestNames.CaseEntry,
                    context,
                    [Term.A(row.Name)]
                );
            }
        }
        catch (ErlangException error)
        {
            if (row.Error is not null && error.ExceptionClass == ProplistFamilyTestNames.ErrorClass && error.Reason.Equals(expected))
            {
                return Nil.Value;
            }
            throw;
        }
        bool equal = row.Error is null && actual.Equals(expected);
        if (!equal && row.Error is null && row.Policy == ProplistFamilyTestNames.Unordered)
        {
            Term[] left = Cons.Items(actual).ToArray();
            Term[] right = Cons.Items(expected).ToArray();
            equal = left.Length == right.Length && left.Distinct().Count() == left.Length && left.ToHashSet().SetEquals(right);
        }
        if (!equal)
        {
            throw new InvalidOperationException(ProplistFamilyTestNames.OutcomeFailure + row.Name + actual);
        }

        return Nil.Value;
    }
}
