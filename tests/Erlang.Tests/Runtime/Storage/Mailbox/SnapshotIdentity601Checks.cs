using System.Runtime.ExceptionServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.MailboxSnapshot601TestNames;
using static Erlang.Tests.SnapshotIdentity601Names;

namespace Erlang.Tests;

internal static class SnapshotIdentity601Checks
{
    internal static async Task Plain()
    {
        var runtime = new ProcessRuntime();
        RuntimeNodeFoundationHostActor? actor = null;
        var failures = new List<Exception>();
        try
        {
            actor = new(runtime);
            ProcessContext context = await actor.Ready.WaitAsync(Deadline);
            var identity = new LocalFunctionIdentity(
                Term.A(HostModule),
                null,
                0,
                0,
                new BitString(new byte[UniqueBytes])
            );
            int invoked = 0;
            // A valid public managed callback with user metadata. It has no
            // canonical source/portable recipe and Snapshot must not require one.
            var callback = new FunctionTerm(
                identity,
                0,
                Array.Empty<Term>(),
                (_, _) =>
            {
                Interlocked.Increment(ref invoked);

                return ValueTask.FromResult<Term>(Term.I(CallbackResult));
            }
            );
            Term message = Term.Tuple(callback, Term.List(Term.I(CallbackResult)));
            runtime.Send(context.Self, message);
            var snapshot = (IReadOnlyList<Term>)Call(Property(context, MailboxProperty), Snapshot)!;
            MailboxSnapshot601Fixture.Require(snapshot.Count == 1 && ReferenceEquals(snapshot[0], message));
            Term info = await actor.InvokeAsync(async active =>
                await runtime.Modules.Call(
                    active,
                    ErlangModuleNames.Module,
                    ProcessInfoNames.Function,
                    active.Self,
                    Term.A(SnapshotIdentity601Names.Messages)
                ));
            Term returned = Cons.Items(((TupleTerm)info).Items[1]).Single();
            MailboxSnapshot601Fixture.Require(ReferenceEquals(returned, message)
                && ReferenceEquals(((TupleTerm)returned).Items[0], callback));
            Term result = await actor.InvokeAsync(async active => await callback.Invoke(active, Array.Empty<Term>()));
            MailboxSnapshot601Fixture.Require(result.Equals(Term.I(CallbackResult)) && invoked == 1);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            if (actor is not null)
                try
                {
                    await actor.DisposeAsync().AsTask().WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            try
            {
                await runtime.DisposeAsync().AsTask().WaitAsync(Deadline);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        }
        Finish(failures);
    }

    internal static Task Mixed() => MailboxSnapshot601Fixture.Run(async fixture =>
    {
        int invoked = 0;
        var callback = new FunctionTerm(0, (_, _) =>
        {
            Interlocked.Increment(ref invoked);

            return ValueTask.FromResult<Term>(Term.I(CallbackResult));
        });
        Term plain = Term.Tuple(callback, Term.List(Term.I(CallbackResult)));
        fixture.Runtime.Send(fixture.TargetContext.Self, plain);
        using (var host = new NativeTermHostScope524(fixture.Runtime))
        {
            var values = (IReadOnlyList<Term>)Call(fixture.MailboxOwner, Snapshot)!;
            MailboxSnapshot601Fixture.Require(values.Count == 2 && ReferenceEquals(values[1], plain)
                && ReferenceEquals(((TupleTerm)values[1]).Items[0], callback));
            fixture.Adopt(values[0]);
        }
        Term actual = await fixture.Target.InvokeAsync(async context => await callback.Invoke(context, Array.Empty<Term>()));
        MailboxSnapshot601Fixture.Require(actual.Equals(Term.I(CallbackResult)) && invoked == 1);
        await fixture.Dequeue();
        await fixture.VerifyAndRetire();
        GC.KeepAlive(plain);
    });

    private static void Finish(List<Exception> failures)
    {
        if (failures.Count == 1) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1) throw new AggregateException(CleanupFailure, failures);
    }
}
