using Erlang.Compiler;
using Erlang.Differential;

namespace Erlang.Tests;

internal static class ProplistIteratorHostChecks
{
    internal static async Task Run()
    {
        var failures = new List<Exception>();
        foreach (var profile in new[]
        {
            (ProplistIteratorTestNames.Resource, ProplistIteratorTestNames.File, ProplistIteratorTestNames.Cases),
            (ProplistFlatIteratorTestNames.Resource, ProplistFlatIteratorTestNames.File, ProplistFlatIteratorTestNames.Cases)
        })
        {
            try
            {
                await RunProfile(profile.Resource, profile.File, profile.Cases);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        }
        if (failures.Count != 0)
        {
            throw new AggregateException(ProplistFamilyTestNames.Failure, failures);
        }
    }

    private static async Task RunProfile(string resource, string file, IReadOnlyList<(string Name, string Input, string? Expected)> cases)
    {
        using var compiled = GeneratedModuleCompiler.Compile(ProplistFamilyTestData.Read(resource), file);
        await using var runtime = new ProcessRuntime();
        compiled.Definition.Register(runtime.Modules);
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        var failures = new List<Exception>();
        var expectedRows = new List<Term>();
        foreach (var row in cases)
        {
            try
            {
                await owner.InvokeAsync(async context =>
                {
                    Term input = await Execution.EvaluateAsync(new Parser(row.Input).ParseExpression(), context);
                    Term expected = row.Expected is null ? Term.Tuple(Term.A(ProplistIteratorTestNames.BadMap), input)
                        : await Execution.EvaluateAsync(new Parser(row.Expected).ParseExpression(), context);
                    Term outcome = row.Expected is null
                        ? Term.Tuple(Term.A(ProplistIteratorTestNames.Error), Term.A(ProplistIteratorTestNames.Error), expected)
                        : Term.Tuple(Term.A(ProplistIteratorTestNames.Ok), expected);
                    expectedRows.Add(Term.Tuple(Term.A(row.Name), outcome));
                    try
                    {
                        Term result = await runtime.Modules.Call(
                            context,
                            ProplistFamilyTestNames.Module,
                            ProplistIteratorTestNames.FromMap,
                            input
                        );
                        if (row.Expected is null || !result.Equals(expected))
                        {
                            throw new InvalidOperationException(ProplistFamilyTestNames.OutcomeFailure + row.Name + result);
                        }
                    }
                    catch (ErlangException error)
                    {
                        if (row.Expected is not null || error.ExceptionClass != ProplistIteratorTestNames.Error || !error.Reason.Equals(expected))
                        {
                            throw;
                        }
                        Term location = Term.List(
                            Term.Tuple(Term.A(ProplistIteratorTestNames.SourceFile), Term.String(ProplistIteratorTestNames.MapsFile)),
                            Term.Tuple(Term.A(ProplistIteratorTestNames.SourceLine), Term.I(ProplistIteratorTestNames.MapsLine)),
                            Term.Tuple(
                                Term.A(ProplistIteratorTestNames.ErrorInfo),
                                new MapTerm(new Dictionary<Term, Term>
                                {
                                    [Term.A(ProplistIteratorTestNames.ErrorModule)] = Term.A(ProplistIteratorTestNames.ErrorFormatter)
                                })
                            )
                        );
                        Term first = Term.Tuple(
                            Term.A(ProplistIteratorTestNames.Maps),
                            Term.A(ProplistIteratorTestNames.ToList),
                            Term.List(input),
                            location
                        );
                        if (error.StackTraceTerm is not Cons frame || !frame.Head.Equals(first))
                        {
                            throw new InvalidOperationException(ProplistFamilyTestNames.OutcomeFailure + row.Name + error.StackTraceTerm);
                        }
                    }

                    return Nil.Value;
                }).WaitAsync(ProplistFamilyTestNames.Deadline);
            }
            catch (Exception error)
            {
                failures.Add(new InvalidOperationException(row.Name, error));
            }
        }
        try
        {
            Term rows = await owner.InvokeAsync(context => Execution.InvokeAsync(
                compiled.Definition,
                ProplistIteratorTestNames.Run,
                context,
                []
            ).AsTask())
                .WaitAsync(ProplistFamilyTestNames.Deadline);
            if (expectedRows.Count != cases.Count || !rows.Equals(Cons.From(expectedRows)))
            {
                throw new InvalidOperationException(ProplistFamilyTestNames.Failure + rows);
            }
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        if (failures.Count != 0)
        {
            throw new AggregateException(ProplistFamilyTestNames.Failure, failures);
        }
    }
}
