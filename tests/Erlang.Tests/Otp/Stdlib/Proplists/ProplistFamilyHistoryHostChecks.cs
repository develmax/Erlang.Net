namespace Erlang.Tests;

internal static class ProplistFamilyHistoryHostChecks
{
    internal static Task Proper() => Traverse(ProplistFamilyHistoryNames.Unfold, false);

    internal static Task Unfold() => Traverse(ProplistFamilyHistoryNames.Unfold, true);

    internal static Task Compact() => Traverse(ProplistFamilyHistoryNames.Compact, true);

    internal static Task Delete() => Traverse(ProplistFamilyHistoryNames.Delete, true);

    private static async Task Traverse(string function, bool improper)
    {
        await using var runtime = new ProcessRuntime();
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        await owner.InvokeAsync(async context =>
        {
            await runtime.Modules.Call(
                context,
                ProplistFamilyHistoryNames.Erlang,
                ProplistFamilyHistoryNames.SystemFlag,
                Term.A(ProplistFamilyHistoryNames.BacktraceDepth),
                Term.I(ProplistFamilyHistoryNames.Depth)
            );
            Term input = Cons.From(
                Enumerable.Repeat<Term>(Term.A(ProplistFamilyHistoryNames.Entry), ProplistFamilyHistoryNames.Cells),
                improper ? Term.A(ProplistFamilyHistoryNames.Tail) : Nil.Value
            );
            try
            {
                Term[] arguments = function == ProplistFamilyHistoryNames.Delete
                    ? [Term.A(ProplistFamilyHistoryNames.Missing), input] : [input];
                Term result = await runtime.Modules.Call(
                    context,
                    ProplistFamilyHistoryNames.Proplists,
                    function,
                    arguments
                );
                if (improper || Cons.Items(result).Count() != ProplistFamilyHistoryNames.Cells)
                {
                    throw new InvalidOperationException(ProplistFamilyTestNames.Failure);
                }
            }
            catch (ErlangException error)
            {
                if (!improper || Cons.Items(error.StackTraceTerm).Count() != ProplistFamilyHistoryNames.RepeatedContinuationFrames)
                {
                    throw;
                }
            }

            return Nil.Value;
        }).WaitAsync(ProplistFamilyTestNames.Deadline);
    }

    internal static async Task Cancellation()
    {
        await using var runtime = new ProcessRuntime();
        Term input = Cons.From(Enumerable.Repeat<Term>(Term.A(ProplistFamilyHistoryNames.Entry), ProplistFamilyHistoryNames.CancellationCells));
        var entered = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var bodySettled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var child = runtime.Spawn(async context =>
        {
            entered.TrySetResult(context);
            try
            {
                await runtime.Modules.Call(
                    context,
                    ProplistFamilyHistoryNames.Proplists,
                    ProplistFamilyHistoryNames.Unfold,
                    input
                );
            }
            finally
            {
                bodySettled.TrySetResult();
            }

            return Nil.Value;
        });
        var controller = runtime.Spawn(async context =>
        {
            await entered.Task;
            runtime.Exit(context, child.Pid, Term.A(ProplistFamilyHistoryNames.Kill));

            return Nil.Value;
        });
        Term reason = await child.Completion.WaitAsync(ProplistFamilyTestNames.Deadline);
        await controller.Completion.WaitAsync(ProplistFamilyTestNames.Deadline);
        await bodySettled.Task.WaitAsync(ProplistFamilyTestNames.Deadline);
        if (!bodySettled.Task.IsCompletedSuccessfully || !reason.Equals(Term.A(ProplistFamilyHistoryNames.Killed)))
        {
            throw new InvalidOperationException(ProplistFamilyTestNames.Failure);
        }
    }
}
