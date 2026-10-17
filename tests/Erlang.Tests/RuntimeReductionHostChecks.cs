using System.Numerics;

namespace Erlang.Tests;

internal static class RuntimeReductionHostChecks
{
    public static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var owner = runtime.Spawn(async context =>
        {
            try
            {
                TupleTerm initial = await Sample(context, RuntimeStatisticsNames.Reductions);
                TupleTerm exactInitial = await Sample(context, RuntimeStatisticsNames.ExactReductions);
                Require(Total(initial) == Delta(initial) && Total(exactInitial) == Delta(exactInitial));
                await Work(runtime, RuntimeReductionHostNames.FirstWork);
                TupleTerm afterRetirement = await Sample(context, RuntimeStatisticsNames.Reductions);
                Require(Total(afterRetirement) - Total(initial) == RuntimeReductionHostNames.FirstWork);
                Require(Delta(afterRetirement) == RuntimeReductionHostNames.FirstWork);
                TupleTerm repeated = await Sample(context, RuntimeStatisticsNames.Reductions);
                Require(Total(repeated) == Total(afterRetirement) && Delta(repeated) == 0);
                TupleTerm exactAfterApproximate = await Sample(context, RuntimeStatisticsNames.ExactReductions);
                Require(Delta(exactAfterApproximate) == RuntimeReductionHostNames.FirstWork);
                Require(Total(exactAfterApproximate) == Total(repeated));
                await Task.WhenAll(
                    Work(runtime, RuntimeReductionHostNames.FirstWork),
                    Work(runtime, RuntimeReductionHostNames.SecondWork)
                );
                TupleTerm concurrent = await Sample(context, RuntimeStatisticsNames.Reductions);
                Require(Delta(concurrent) == RuntimeReductionHostNames.FirstWork + RuntimeReductionHostNames.SecondWork);
                TupleTerm beforeBump = await Sample(context, RuntimeStatisticsNames.ExactReductions);
                await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    RuntimeReductionHostNames.Bump,
                    Term.I(RuntimeReductionHostNames.RequestedBump)
                );
                TupleTerm bumped = await Sample(context, RuntimeStatisticsNames.ExactReductions);
                Require(Total(bumped) - Total(beforeBump) == RuntimeReductionHostNames.CappedBump);
                Require(Delta(bumped) == RuntimeReductionHostNames.CappedBump);
                finished.TrySetResult();
            }
            catch (Exception failure)
            {
                finished.TrySetException(failure);
                throw;
            }

            return Nil.Value;
        });
        await finished.Task.WaitAsync(RuntimeReductionHostNames.Deadline);
        Require((await owner.Completion.WaitAsync(RuntimeReductionHostNames.Deadline)).Equals(Term.A(ProcessExitReasons.Normal)));
    }

    private static async Task Work(ProcessRuntime runtime, int amount)
    {
        var worker = runtime.Spawn(async context =>
        {
            for (int count = 0; count < amount; count++)
                await context.ReduceAsync();

            return Nil.Value;
        });
        Require((await worker.Completion.WaitAsync(RuntimeReductionHostNames.Deadline)).Equals(Term.A(ProcessExitReasons.Normal)));
    }

    private static async ValueTask<TupleTerm> Sample(ProcessContext context, string key)
        => (TupleTerm)await context.Runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            RuntimeStatisticsNames.Function,
            Term.A(key)
        );

    private static BigInteger Total(TupleTerm sample) => ((Integer)sample.Items[0]).Value;

    private static BigInteger Delta(TupleTerm sample) => ((Integer)sample.Items[1]).Value;

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(RuntimeReductionHostNames.Failure);
    }
}
