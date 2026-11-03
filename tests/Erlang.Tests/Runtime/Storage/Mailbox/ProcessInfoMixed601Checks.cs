using System.Runtime.ExceptionServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.MailboxSnapshot601TestNames;
using static Erlang.Tests.ProcessInfoMixed601Names;

namespace Erlang.Tests;

internal static class ProcessInfoMixed601Checks
{
    internal static Task Run() => NativeMagicReference576Fixture.RunDefault(async fixture =>
    {
        var failures = new List<Exception>();
        RuntimeNodeFoundationHostActor? actor = null;
        try
        {
            actor = new(fixture.Runtime);
            ProcessContext context = await actor.Ready.WaitAsync(Deadline);
            object payload = fixture.CreatePayload();
            Term resource = (Term)Call(fixture.Runtime.ReferenceResources, Make, payload)!;
            object mailbox = Property(context, MailboxProperty);
            using (var publication = (IDisposable)MailboxSnapshot601Fixture.Static(
                PublicationType,
                Capture,
                fixture.Runtime,
                resource,
                null
            )!)
                Call(
                    mailbox,
                    Send,
                    resource,
                    null,
                    false,
                    false,
                    null,
                    true,
                    null,
                    publication
                );
            fixture.ReleaseNative();
            int invoked = 0;
            var metadata = new LocalFunctionIdentity(
                Term.A(CallbackModule),
                null,
                0,
                0,
                new BitString(new byte[UniqueBytes])
            );
            var callback = new FunctionTerm(
                metadata,
                0,
                Array.Empty<Term>(),
                (_, _) =>
            {
                Interlocked.Increment(ref invoked);

                return ValueTask.FromResult<Term>(Term.I(CallbackValue));
            }
            );
            Term plain = Term.Tuple(callback, Term.List(Term.I(CallbackValue)));
            fixture.Runtime.Send(context.Self, plain);
            await actor.InvokeAsync(async active =>
            {
                using var host = new NativeTermHostScope524(fixture.Runtime);
                Term result = await fixture.Runtime.Modules.Call(
                    active,
                    ErlangModuleNames.Module,
                    ProcessInfoNames.Function,
                    active.Self,
                    Term.A(Messages)
                );
                Term[] values = Cons.Items(((TupleTerm)result).Items[1]).ToArray();
                MailboxSnapshot601Fixture.Require(values.Length == 2 && values[0].Equals(resource)
                    && ReferenceEquals(values[1], plain) && ReferenceEquals(((TupleTerm)values[1]).Items[0], callback));
                Term called = await callback.Invoke(active, Array.Empty<Term>());
                MailboxSnapshot601Fixture.Require(called.Equals(Term.I(CallbackValue)) && invoked == 1);

                return Nil.Value;
            });
            // Queue is the remaining actual Guest owner; normal actor exit drops
            // it. Neither public local callback nor stale CLR values own native.
            await actor.DisposeAsync().AsTask().WaitAsync(Deadline);
            actor = null;
            await ((Task)Property(payload, Retired)).WaitAsync(Deadline);
            GC.KeepAlive(resource);
            GC.KeepAlive(plain);
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
        }
        if (failures.Count == 1) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1) throw new AggregateException(CleanupFailure, failures);
    });
}
