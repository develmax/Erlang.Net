using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.NativeResource524Names;
using static Erlang.Tests.PreparedMailboxRead292Names;

namespace Erlang.Tests;

internal static class PreparedMailboxRead292Checks
{
    internal static async Task Run(bool owned)
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create(owned);
        await using var foreignRuntime = OwnedGuest506RuntimeFactory.Create(owned);
        await using var image = runtime.Modules.PrepareBeam(BeamLiteralExportHostBytes.Image(ExternalTermFormat.Encode(Term.I(Literal))));
        await using var foreignImage = foreignRuntime.Modules.PrepareBeam(BeamLiteralExportHostBytes.Image(ExternalTermFormat.Encode(Term.I(ForeignLiteral))));
        var source = runtime.ReferenceResources.CreatePrepared(image);
        var foreign = foreignRuntime.ReferenceResources.CreatePrepared(foreignImage);
        object owner = Property(source, PreparedLoader807Names.Owner);
        object foreignOwner = Property(foreign, PreparedLoader807Names.Owner);
        Task retired = (Task)Property(owner, PreparedLoader807Names.Settled);
        Task foreignRetired = (Task)Property(foreignOwner, PreparedLoader807Names.Settled);
        uint[] expected = source.Words.ToArray();
        object publication = Static(
            Publication,
            Capture,
            runtime,
            source,
            null
        )!;
        object foreignPublication = Static(
            Publication,
            Capture,
            foreignRuntime,
            foreign,
            null
        )!;
        IDisposable? operation = null, loan = null, exportedHost = null;
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        try
        {
            await actor.InvokeAsync(context =>
            {
                Call(
                    context.Mailbox,
                    Send,
                    source,
                    null,
                    true,
                    false,
                    null,
                    false,
                    null,
                    publication
                );

                return Task.FromResult<Term>(Nil.Value);
            });
            ProcessContext context = await actor.Ready.WaitAsync(Deadline);
            object custody;
            lock (Field(context.Mailbox, SuspensionAck175Names.Gate))
            {
                object entry = ((IEnumerable)Field(context.Mailbox, Messages)).Cast<object>().Single();
                custody = Property(entry, Custody);
                operation = (IDisposable)Call(custody, BorrowOperation)!;
            }
            ((IDisposable)Property(source, PreparedLoader807Names.Host)).Dispose();
            ((IDisposable)Property(foreign, PreparedLoader807Names.Host)).Dispose();
            Check(!retired.IsCompleted && !foreignRetired.IsCompleted);
            Reject<ObjectDisposedException>(() => Static(
                PreparedRoots,
                HostCopy,
                source,
                null
            ));
            using (var scope = new NativeTermHostScope524(runtime))
                Reject<ObjectDisposedException>(() => Static(
                    PreparedRoots,
                    HostCopy,
                    source,
                    null
                ));
            Reject<NotSupportedException>(() => Call(
                custody,
                BorrowRead,
                foreignRuntime,
                source,
                operation
            ));
            var closed = (IDisposable)Call(custody, BorrowOperation)!;
            closed.Dispose();
            Reject<ObjectDisposedException>(() => Call(
                custody,
                BorrowRead,
                runtime,
                source,
                closed
            ));
            loan = (IDisposable)Call(
                custody,
                BorrowRead,
                runtime,
                source,
                operation
            )!;
            Reject<NotSupportedException>(() => Call(
                loan,
                CopyHost,
                foreign,
                null
            ));
            // Same native owner/words is insufficient: this new genuine source
            // wrapper was never the actual Entry graph object.
            var outside = runtime.ReferenceResources.CreatePrepared(image);
            ((IDisposable)Property(outside, PreparedLoader807Names.Host)).Dispose();
            Reject<InvalidOperationException>(() => Call(
                loan,
                CopyHost,
                outside,
                null
            ));
            loan.Dispose();
            Reject<ObjectDisposedException>(() => Call(
                loan,
                CopyHost,
                source,
                null
            ));
            loan = null;
            // Genuine competing selector removes the Entry while this reader's
            // exact operation remains active. A released custody latch must not
            // revoke its physical work or create a spurious receive failure.
            await actor.InvokeAsync(current =>
            {
                using var scope = new NativeTermHostScope524(runtime);
                int removed = current.Mailbox.Remove(value =>
                {
                    var actual = (ResourceReferenceTerm)value;
                    using var host = (IDisposable)Property(actual, PreparedLoader807Names.Host);
                    Check(actual.Words.SequenceEqual(expected));

                    return true;
                });
                Check(removed == SingleEntry);

                return Task.FromResult<Term>(Nil.Value);
            });
            loan = (IDisposable)Call(
                custody,
                BorrowRead,
                runtime,
                source,
                operation
            )!;
            var copied = (ResourceReferenceTerm)Call(
                loan,
                CopyHost,
                source,
                null
            )!;
            exportedHost = (IDisposable)Property(copied, PreparedLoader807Names.Host);
            Check(copied.Words.SequenceEqual(expected));
            loan.Dispose();
            loan = null;
            operation.Dispose();
            operation = null;
            await ((IAsyncDisposable)publication).DisposeAsync();
            Check(!retired.IsCompleted);
            using (var reader = runtime.ReferenceResources.AcquirePrepared(copied))
                Check(reader is not null && ReferenceEquals(reader.Value, image));
            exportedHost.Dispose();
            exportedHost = null;
            await retired.WaitAsync(Deadline);
            Check((long)Property(owner, PreparedLoader807Names.Bytes) == NoNativeBytes);
            Reject<ObjectDisposedException>(() => Static(
                PreparedRoots,
                HostCopy,
                source,
                null
            ));
            await ((IAsyncDisposable)foreignPublication).DisposeAsync();
            await foreignRetired.WaitAsync(Deadline);
            Check((long)Property(foreignOwner, PreparedLoader807Names.Bytes) == NoNativeBytes);
        }
        finally
        {
            exportedHost?.Dispose();
            loan?.Dispose();
            operation?.Dispose();
            ((IDisposable)Property(source, PreparedLoader807Names.Host)).Dispose();
            ((IDisposable)Property(foreign, PreparedLoader807Names.Host)).Dispose();
            await ((IAsyncDisposable)publication).DisposeAsync();
            await ((IAsyncDisposable)foreignPublication).DisposeAsync();
            await runtime.DisposeAsync();
            await foreignRuntime.DisposeAsync();
            await Task.WhenAll(retired, foreignRetired).WaitAsync(Deadline);
            GC.KeepAlive(source);
            GC.KeepAlive(foreign);
        }
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        bool rejected = false;
        try
        {
            action();
        }
        catch (T)
        {
            rejected = true;
        }
        Check(rejected);
    }

    private static object Field(object value, string name) => value.GetType().GetField(name, Members)!.GetValue(value)!;

    private static object? Static(string type, string name, params object?[] values)
    {
        try
        {
            return typeof(ProcessRuntime).Assembly.GetType(type, true)!.GetMethod(name,
                       BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!.Invoke(null, values);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }
}
