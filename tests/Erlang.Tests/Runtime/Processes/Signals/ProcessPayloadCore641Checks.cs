using System.Collections;
using static Erlang.Tests.ProcessPayloadCore641Access;
using static Erlang.Tests.ProcessPayloadCore641Names;

namespace Erlang.Tests;

internal static class ProcessPayloadCore641Checks
{
    internal static async Task CompletedFailureIsolation()
    {
        var fixture = new ProcessPayloadCore641Fixture();
        var failures = new List<Exception>();
        object? work = null;
        try
        {
            var a = await fixture.Actor();
            var b = await fixture.Actor();
            object owner = Property(fixture.Runtime, Owner);
            var expected = new InvalidOperationException(Injected);
            fixture.ExpectedFailure = expected;
            var ambient = new AsyncLocal<object?>();
            var cleanup = new ProcessPayloadCore641Fault(expected, ambient);
            work = Call(
                owner,
                Reserve,
                a.Context,
                false
            )!;
            Call(work, Attach, cleanup);
            await a.Actor.Invoke(_ =>
            {
                object? previous = ambient.Value;
                ambient.Value = new object();
                try
                {
                    Call(work, End);
                }
                finally
                {
                    ambient.Value = previous;
                }

                return true;
            });
            await Fault((Task)Property(work, Completion), expected);
            // Completion is deliberately published before Finished. Wait until
            // actual removal so the next assertion exercises FAILED HISTORY.
            long deadline = Environment.TickCount64 + (long)Deadline.TotalMilliseconds;
            while (true)
            {
                bool present;
                lock (Field(owner, Gate)!)
                    present = ((IEnumerable)Field(owner, Live)!).Cast<object>().Any(item => ReferenceEquals(item, work));
                if (!present) break;
                Require(Environment.TickCount64 < deadline);
                await Task.Delay(1);
            }
            await Fault((Task)Call(owner, JoinContext, a.Context)!, expected);
            await ((Task)Call(owner, JoinContext, b.Context)!).WaitAsync(Deadline);
            await Fault((Task)Call(owner, Drain)!, expected);
            Require(cleanup.Disposals == 1 && cleanup.DefaultContext);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            if (work is not null)
                try
                {
                    Call(work, End);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            await fixture.Finish(failures);
        }
    }

    internal static async Task UnusedStopDrain()
    {
        var fixture = new ProcessPayloadCore641Fixture();
        var failures = new List<Exception>();
        try
        {
            Require(Field(fixture.Runtime, OwnerField) is null);
            lock (Field(fixture.Runtime, Gate)!)
                Call(fixture.Runtime, StopRuntime);
            Require(Field(fixture.Runtime, OwnerField) is not null);
            await ((Task)Call(fixture.Runtime, DrainRuntime)!).WaitAsync(Deadline);
            Refused(() => Static(
                Prepare,
                fixture.Runtime,
                null,
                Term.I(1)
            ));
            Refused(() => Static(Reserve, fixture.Runtime, null));
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            await fixture.Finish(failures);
        }
    }

    internal static async Task ContextSeal()
    {
        var fixture = new ProcessPayloadCore641Fixture();
        var failures = new List<Exception>();
        try
        {
            var actor = await fixture.Actor();
            object owner = Property(fixture.Runtime, Owner);
            object reserved = fixture.Hold(Static(Reserve, fixture.Runtime, actor.Context));
            object source = fixture.Hold(Static(
                Prepare,
                fixture.Runtime,
                null,
                Term.Tuple(Term.I(7))
            ));
            lock (Field(fixture.Runtime, Gate)!)
                Call(owner, Seal, actor.Context);
            Refused(() => Static(
                Prepare,
                fixture.Runtime,
                actor.Context,
                Term.I(2)
            ));
            Refused(() => Call(source, Borrow, actor.Context));
            Task joining = (Task)Call(owner, JoinContext, actor.Context)!;
            Require(!joining.IsCompleted);
            // Existing reserved holder may still capture under its actual
            // producer; seal only refuses NEW context-bound records.
            Call(reserved, Capture, Term.Tuple(Term.I(3)));
            object independent = fixture.Hold(Call(source, Borrow, (object?)null)!);
            Call(reserved, Close);
            await joining.WaitAsync(Deadline);
            Task draining = (Task)Call(owner, Drain)!;
            Require(!draining.IsCompleted);
            Call(source, Close);
            Call(independent, Close);
            await draining.WaitAsync(Deadline);
            await ((Task)Property(reserved, Completion)).WaitAsync(Deadline);
            await ((Task)Property(independent, Completion)).WaitAsync(Deadline);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            await fixture.Finish(failures);
        }
    }
}
