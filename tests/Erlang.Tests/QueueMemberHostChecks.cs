using System.Reflection;

namespace Erlang.Tests;

internal static class QueueMemberHostChecks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            var remaining = typeof(ProcessContext).GetProperty(
                QueueMemberHostNames.Remaining,
                BindingFlags.Instance | BindingFlags.NonPublic
            )!;
            foreach (var boundary in new[]
            {
                (Count: QueueMemberHostNames.BeforeBoundary, Committed: 0),
                (Count: QueueMemberHostNames.AtBoundary, Committed: 0),
                (Count: QueueMemberHostNames.AfterBoundary, Committed: QueueMemberHostNames.FirstCommitted),
                (Count: QueueMemberHostNames.AfterSecondBoundary, Committed: QueueMemberHostNames.SecondCommitted)
            })
            {
                foreach (bool searchRear in new[] { true, false })
                {
                    Term input = Cons.From(
                        Enumerable.Range(0, boundary.Count).Select(index => Term.I(index)),
                        Term.A(QueueMemberHostNames.Tail)
                    );
                    while ((int)remaining.GetValue(context)! != QueueMemberHostNames.StartingReductions)
                        await context.ReduceAsync();
                    try
                    {
                        await runtime.Modules.Call(
                            context,
                            QueueModuleNames.Module,
                            QueueModuleNames.Member,
                            Term.A(QueueMemberHostNames.Missing),
                            searchRear ? Term.Tuple(input, Nil.Value) : Term.Tuple(Nil.Value, input)
                        );
                    }
                    catch (ErlangException error)
                    {
                        Require(error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)));
                        var first = (TupleTerm)((Cons)error.StackTraceTerm).Head;
                        Term[] arguments = Cons.Items(first.Items[2]).ToArray();
                        Term expected = input;
                        for (int index = 0; index < boundary.Committed; index++)
                            expected = ((Cons)expected).Tail;
                        Require(first.Items[0].Equals(Term.A(QueueMemberHostNames.Lists))
                            && first.Items[1].Equals(Term.A(QueueModuleNames.Member))
                            && arguments.Length == 2
                            && arguments[0].Equals(Term.A(QueueMemberHostNames.Missing))
                            && ReferenceEquals(arguments[1], expected));
                        int queueCallers = Cons.Items(error.StackTraceTerm).Count(frame => frame is TupleTerm tuple
                            && tuple.Items[0].Equals(Term.A(QueueModuleNames.Module))
                            && tuple.Items[1].Equals(Term.A(QueueModuleNames.Member)));
                        Require(queueCallers == 1);

                        continue;
                    }

                    throw new InvalidOperationException(QueueMemberHostNames.Failure);
                }
            }

            async ValueTask<Term> Call(Term item, Term rear, Term front)
                => await runtime.Modules.Call(
                    context,
                    QueueModuleNames.Module,
                    QueueModuleNames.Member,
                    item,
                    Term.Tuple(rear, front)
                );
            Term improper = new Cons(Term.I(1), Term.A(QueueMemberHostNames.Tail));
            Require((await Call(Term.I(1), improper, improper)).Equals(Term.A(QueueCoreHostNames.True)));
            Require((await Call(new FloatTerm(1), Term.List(Term.I(1)), Nil.Value)).Equals(Term.A(QueueCoreHostNames.False)));
            Require((await Call(Term.I(1), Nil.Value, improper)).Equals(Term.A(QueueCoreHostNames.True)));

            return Nil.Value;
        });
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(QueueMemberHostNames.Failure);
    }
}
