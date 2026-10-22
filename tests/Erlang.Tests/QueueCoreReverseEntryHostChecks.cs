using System.Reflection;

namespace Erlang.Tests;

internal static class QueueCoreReverseEntryHostChecks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            var failures = new List<Exception>();
            foreach (var boundary in new[]
            {
                (Count: QueueCoreReverseEntryHostNames.BeforeBoundary, Committed: 0),
                (Count: QueueCoreReverseEntryHostNames.AtBoundary, Committed: 0),
                (Count: QueueCoreReverseEntryHostNames.AfterBoundary, Committed: QueueCoreReverseEntryHostNames.FirstCommittedCells),
                (Count: QueueCoreReverseEntryHostNames.AfterFirstTrap, Committed: QueueCoreReverseEntryHostNames.FirstCommittedCells),
                (Count: QueueCoreReverseEntryHostNames.AfterSecondTrap, Committed: QueueCoreReverseEntryHostNames.SecondCommittedCells)
            })
            {
                try
                {
                    await Check(context, boundary.Count, boundary.Committed);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            }
            if (failures.Count != 0)
                throw new AggregateException(QueueCoreReverseEntryHostNames.Failure, failures);

            return Nil.Value;
        });
    }

    private static async Task Check(ProcessContext context, int count, int committed)
    {
        Term original = Cons.From(
            Enumerable.Range(0, count).Select(index => Term.I(index)),
            Term.A(QueueCoreReverseEntryHostNames.Tail)
        );
        PropertyInfo remaining = typeof(ProcessContext).GetProperty(
            QueueCoreReverseEntryHostNames.RemainingReductions,
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;
        while ((int)remaining.GetValue(context)! != QueueCoreReverseEntryHostNames.StartingReductions)
            await context.ReduceAsync();
        Term expectedList = original;
        Term expectedTail = Nil.Value;
        for (int index = 0; index < committed; index++)
        {
            var cell = (Cons)expectedList;
            expectedTail = new Cons(cell.Head, expectedTail);
            expectedList = cell.Tail;
        }
        try
        {
            await context.Runtime.Modules.Call(
                context,
                QueueModuleNames.Module,
                QueueModuleNames.ToList,
                Term.Tuple(original, Nil.Value)
            );
        }
        catch (ErlangException error)
        {
            Require(error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)));
            var frame = (TupleTerm)((Cons)error.StackTraceTerm).Head;
            Require(frame.Items[0].Equals(Term.A(QueueCoreReverseEntryHostNames.Lists))
                && frame.Items[1].Equals(Term.A(QueueCoreReverseEntryHostNames.Reverse)));
            Term[] arguments = Cons.Items(frame.Items[2]).ToArray();
            Require(arguments.Length == QueueCoreReverseEntryHostNames.ReverseArity
                && ReferenceEquals(arguments[0], expectedList)
                && arguments[1].Equals(expectedTail));

            return;
        }

        throw new InvalidOperationException(QueueCoreReverseEntryHostNames.Failure);
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(QueueCoreReverseEntryHostNames.Failure);
    }
}
