namespace Erlang.Tests;

public static class CpuTopologyStateHostChecks
{
    public static async Task Run()
    {
        Term input = Term.List(Term.Tuple(
            Term.A(CpuTopologyNames.Processor),
            Term.List(Term.Tuple(Term.A(CpuTopologyNames.Core), Term.Tuple(Term.A(CpuTopologyNames.Logical), Term.I(42))))
        ));
        Term expected = Term.List(Term.Tuple(
            Term.A(CpuTopologyNames.Cpu),
            Term.I(-1),
            Term.I(0),
            Term.I(-1),
            Term.I(0),
            Term.I(0),
            Term.I(42)
        ));
        Require(CpuTopologyConversion.ToInternal(input).Equals(expected));
        await using var first = new ProcessRuntime();
        await using var second = new ProcessRuntime();
        var prior = new System.Collections.Concurrent.ConcurrentBag<Term>();
        Exception? failure = null;
        var processes = Enumerable.Range(0, CpuTopologyStateHostParameters.Exchanges).Select(index => first.Spawn(async context =>
        {
            try
            {
                Term row = Term.List(Term.Tuple(
                    Term.A(CpuTopologyNames.Cpu),
                    Term.I(-1),
                    Term.I(index),
                    Term.I(-1),
                    Term.I(0),
                    Term.I(0),
                    Term.I(index)
                ));
                prior.Add(await first.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    CpuTopologyStateNames.SystemFlag,
                    Term.A(CpuTopologyStateNames.Internal),
                    row
                ));
            }
            catch (Exception exception)
            {
                failure = exception;
                throw;
            }

            return Nil.Value;
        })).ToArray();
        await Task.WhenAll(processes.Select(process => process.Completion)).WaitAsync(CpuTopologyStateHostParameters.Deadline);
        if (failure is not null)
            throw failure;
        Require(prior.Count == CpuTopologyStateHostParameters.Exchanges);
        Require(prior.Distinct().Count() == CpuTopologyStateHostParameters.Exchanges);
        Require(prior.Count(value => value.Equals(Term.A(ProcessRegistryAtoms.Undefined))) == 1);
        var isolated = second.Spawn(async context =>
        {
            try
            {
                Require((await second.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    CpuTopologyStateNames.SystemInfo,
                    Term.Tuple(Term.A(CpuTopologyStateNames.Internal), Term.A(CpuTopologyStateNames.Defined))
                )).Equals(Term.A(ProcessRegistryAtoms.Undefined)));
            }
            catch (Exception exception)
            {
                failure = exception;
                throw;
            }

            return Nil.Value;
        });
        await isolated.Completion.WaitAsync(CpuTopologyStateHostParameters.Deadline);
        if (failure is not null)
            throw failure;
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(CpuTopologyStateHostNames.Failure);
    }
}
