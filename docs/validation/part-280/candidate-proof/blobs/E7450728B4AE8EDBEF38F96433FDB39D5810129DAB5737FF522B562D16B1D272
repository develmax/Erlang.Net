namespace Erlang.Tests;

internal static class GarbageCollectionResultHostChecks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        var actor = runtime.Spawn(async context =>
        {
            var reference = runtime.MakeReference();
            var other = runtime.MakeReference();
            Term ignored = Term.A(GarbageCollectionResultHostNames.Ignored);
            Term marker = Term.A(GarbageCollectionResultHostNames.Marker);
            Term wrongReference = Term.Tuple(other, Term.Tuple(ignored, Term.I(8), Term.I(9)));
            Term wrongShape = Term.Tuple(reference, Term.Tuple(ignored, Term.I(1)));
            Term extra = Term.Tuple(reference, Term.Tuple(ignored, Term.I(99), Term.I(99)));
            runtime.Send(context.Self, marker);
            runtime.Send(context.Self, wrongReference);
            runtime.Send(context.Self, wrongShape);
            runtime.Send(context.Self, Term.Tuple(reference, Term.Tuple(ignored, Term.I(2), Term.I(3))));
            runtime.Send(context.Self, Term.Tuple(reference, Term.Tuple(ignored, new FloatTerm(0.5), new FloatTerm(1.25))));
            runtime.Send(context.Self, extra);
            Term reduced = await GarbageCollectionResultHostAccess.GatherAsync(context, reference, 2);
            Require(reduced.Equals(Term.Tuple(new FloatTerm(2.5), new FloatTerm(4.25), Term.I(0))));
            foreach (Term expected in new[] { marker, wrongReference, wrongShape, extra })
                Require((await context.ReceiveAsync<Term>(message => message, TimeSpan.Zero))!.Equals(expected));
            Require(await context.ReceiveAsync<Term>(message => message, TimeSpan.Zero) is null);
            try
            {
                _ = await GarbageCollectionResultHostAccess.GatherAsync(context, Term.I(0), 0);
                Require(false);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.FunctionClause)))
            {
            }
            Term good = Term.Tuple(reference, Term.Tuple(ignored, Term.I(1), Term.I(2)));
            runtime.Send(context.Self, Term.Tuple(reference, Term.Tuple(ignored, marker, Term.I(1))));
            runtime.Send(context.Self, good);
            try
            {
                _ = await GarbageCollectionResultHostAccess.GatherAsync(context, reference, 2);
                Require(false);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.BadArithmetic)))
            {
            }
            Require((await context.ReceiveAsync<Term>(message => message, TimeSpan.Zero))!.Equals(good));
            Require((await GarbageCollectionResultHostAccess.GatherAsync(context, reference, 0)).Equals(
                Term.Tuple(Term.I(0), Term.I(0), Term.I(0))));

            return Nil.Value;
        });
        Require((await actor.Completion.WaitAsync(GarbageCollectionResultHostNames.Deadline)).Equals(Term.A(ProcessExitReasons.Normal)));
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var waiting = runtime.Spawn(async context =>
        {
            entered.SetResult();

            return await GarbageCollectionResultHostAccess.GatherAsync(context, runtime.MakeReference(), 1);
        });
        await entered.Task.WaitAsync(GarbageCollectionResultHostNames.Deadline);
        var killer = runtime.Spawn(context =>
        {
            runtime.Exit(context, waiting.Pid, Term.A(ProcessExitSignals.Kill));

            return ValueTask.FromResult<Term>(Nil.Value);
        });
        Require((await waiting.Completion.WaitAsync(GarbageCollectionResultHostNames.Deadline)).Equals(Term.A(ProcessExitReasons.Killed)));
        _ = await killer.Completion.WaitAsync(GarbageCollectionResultHostNames.Deadline);
    }

    private static void Require(bool valid)
    {
        if (!valid)
            throw new InvalidOperationException(GarbageCollectionResultHostNames.Failure);
    }
}
