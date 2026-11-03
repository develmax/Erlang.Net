using System.Runtime.ExceptionServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.OpaqueBoundary602Names;
using static Erlang.Tests.OpaqueBoundary602Fixture;

namespace Erlang.Tests;

internal static class OpaqueBoundary602Checks
{
    internal static Task RejectionRollback() => Run(
        true,
        1,
        async fixture =>
    {
        long atomicsBefore = fixture.Atomics.Count(HostRoots), nativeBefore = fixture.NativeRootCount();
        int invoked = 0;

        void Attempt()
        {
            Exception? failure = null;
            try
            {
                fixture.Mailbox.Remove(_ =>
                {
                    invoked++;

                    return false;
                });
            }
            catch (Exception error)
            {
                failure = error;
            }
            Require(failure is NotSupportedException or ObjectDisposedException);
            Require(invoked == 0 && fixture.Mailbox.Count == 1);
            Require(fixture.Atomics.Count(HostRoots) == atomicsBefore && fixture.NativeRootCount() == nativeBefore);
        }
        // Missing current scope.
        Attempt();
        // A genuinely captured ExecutionContext sees its scope object AFTER
        // ordinary same-context disposal; no private AsyncLocal is modified.
        ExecutionContext captured;
        using (var closed = new NativeTermHostScope524(fixture.Runtime))
            captured = ExecutionContext.Capture() ?? throw new InvalidOperationException(Failure);
        ExecutionContext.Run(captured, _ => Attempt(), null);
        // Correctly live, but explicitly belongs to another real VM.
        var foreign = new ProcessRuntime();
        Exception? primary = null;
        try
        {
            using var wrong = new NativeTermHostScope524(foreign);
            Attempt();
        }
        catch (Exception error)
        {
            primary = error;
            throw;
        }
        finally
        {
            try
            {
                await foreign.DisposeAsync().AsTask().WaitAsync(Deadline);
            }
            catch (Exception cleanup) when (primary is not null)
            {
                throw new AggregateException(primary, cleanup);
            }
        }
    }
    );

    internal static Task RetainedFalseAndThrow() => Run(
        true,
        1,
        async fixture =>
    {
        var captured = new List<Term>();
        var errors = new List<Exception>();
        NativeTermHostScope524? scope = null;
        // Everything that changes/uses this AsyncLocal scope is synchronous in
        // this exact context; no scope is returned from an async helper.
        try
        {
            scope = new NativeTermHostScope524(fixture.Runtime);
            int calls = 0;
            Require(fixture.Mailbox.Remove(value =>
 {
     captured.Add(value);
     calls++;

     return false;
 }) == 0);
            Require(calls == 1 && fixture.Mailbox.Count == 1);
            var sentinel = new InvalidOperationException(Failure);
            Exception? seen = null;
            try
            {
                fixture.Mailbox.Remove(value =>
                {
                    captured.Add(value);
                    calls++;
                    throw sentinel;
                });
            }
            catch (Exception error)
            {
                seen = error;
            }
            Require(ReferenceEquals(seen, sentinel) && calls == 2 && fixture.Mailbox.Count == 1);
            Require(fixture.Mailbox.Remove(value =>
 {
     captured.Add(value);
     calls++;

     return true;
 }) == 1);
            Require(calls == 3 && fixture.Mailbox.Count == 0 && captured.Count == 3);
            // Initial sender construction frames/invocation have already been
            // disposed and major-collected. Inputs survive through real Host
            // custody after false/throw/dequeue, not the source header.
            foreach (Term value in captured) fixture.CheckEscaped(value);
        }
        catch (Exception error)
        {
            errors.Add(error);
        }
        finally
        {
            foreach (Term value in captured)
                try
                {
                    ReleaseHost(value);
                }
                catch (Exception error)
                {
                    errors.Add(error);
                }
            try
            {
                scope?.Dispose();
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
        }
        if (errors.Count == 1) ExceptionDispatchInfo.Capture(errors[0]).Throw();
        if (errors.Count != 0) throw new AggregateException(CleanupFailure, errors);
        await fixture.Retired();
        GC.KeepAlive(captured); // Bare CLR wrappers do not have Guest authority.
    }
    );

    internal static Task PredicateClosesVm() => Run(
        false,
        2,
        async fixture =>
    {
        object[] works = fixture.Works();
        Require(works.Length == 2);
        fixture.AssertQueueCustody(2);
        var escaped = new List<Term>();
        var errors = new List<Exception>();
        bool closed = false;
        try
        {
            fixture.Mailbox.Remove(value =>
            {
                escaped.Add(value);
                // Current AND later operation interests must all be gone;
                // each work has only its real queue-owner reference.
                Require(works.All(work => (int)OpaqueBoundary602Atomics.Field(work, References)! == (closed ? 0 : 1)));
                if (!closed)
                {
                    Call(fixture.Mailbox, Close);
                    // Failure is finite before Dispose; it does not wait for
                    // the very callback whose unwind would release the pin.
                    Require(works.All(work => (int)OpaqueBoundary602Atomics.Field(work, References)! == 0));
                    fixture.Runtime.DisposeAsync().AsTask().WaitAsync(Deadline).GetAwaiter().GetResult();
                    closed = true;
                    Require(!(bool)Property(fixture.Atomics.Array, Allocated));
                }

                return false;
            });
            Require(closed && escaped.Count != 0);
        }
        catch (Exception error)
        {
            errors.Add(error);
        }
        finally
        {
            foreach (Term value in escaped)
                try
                {
                    ReleaseHost(value);
                }
                catch (Exception error)
                {
                    errors.Add(error);
                }
        }
        if (errors.Count == 1) ExceptionDispatchInfo.Capture(errors[0]).Throw();
        if (errors.Count != 0) throw new AggregateException(CleanupFailure, errors);
        await ((Task)Property(fixture.Atomics.Array, AtomicsRetired)).WaitAsync(Deadline);
    }
    );
}
