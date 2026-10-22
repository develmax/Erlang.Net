using System.Reflection;
using System.Runtime.CompilerServices;
using Erlang;

namespace Erlang.Tests;

internal static partial class EtsCompressed441Checks
{
    internal static async Task MidEncodeCancellation442()
    {
        await using var runtime = new ProcessRuntime(1);
        Install(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        object storage = EtsFoundation405Access.Get(runtime);
        Term table = await Create(runtime, actor, EtsCompressed441Names.Kinds[0]);
        TupleTerm original = Term.Tuple(Term.I(1), Term.I(EtsCompressed441Names.KeyField));
        await actor.InvokeAsync(async context => await Call(
            runtime,
            context,
            EtsCompressed441Names.Insert,
            table,
            original
        ));
        object?[] before = Enumerable.Range(0, EtsFoundation405Access.Snapshot(storage).Length)
            .Select(index => EtsFoundation405Access.Snapshot(storage)[index]).ToArray();
        WeakReference<object> resource = await actor.Invoke(context => CancelCapture442(runtime, storage));
        BeamStoredFunction406Access.RequireCollected(resource);
        var after = EtsFoundation405Access.Snapshot(storage);
        Require(before.SequenceEqual(Enumerable.Range(0, after.Length).Select(index => after[index])));
        Require((await actor.InvokeAsync(async context => await Call(
            runtime,
            context,
            EtsCompressed441Names.Lookup,
            table,
            original.Items[EtsCompressed441Names.KeyField - 1]
        ))).Equals(Term.List(original)));
        await actor.InvokeAsync(async context => await Call(
            runtime,
            context,
            EtsCompressed441Names.Delete,
            table
        ));
        EtsFoundation405Access.Empty(storage);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<object> CancelCapture442(ProcessRuntime runtime, object storage)
    {
        ResourceReferenceTerm resource = runtime.ReferenceResources.CreateBinary(BeamStoredFunction406Names.Bytes);
        TupleTerm row = Term.Tuple(
            Term.Tuple(resource, new BitString(new byte[EtsCodecCancellation442Names.PayloadBytes])),
            Term.I(EtsCompressed441Names.KeyField)
        );
        using var cancellation = new CancellationTokenSource();
        using var listener = new EtsCodecCancellation442Listener(cancellation, EtsCodecCancellation442Names.Encode);
        MethodInfo capture = typeof(ProcessRuntime).Assembly.GetType(EtsCompressed441Names.StoredType, true)!
            .GetMethod(EtsCompressed441Names.Capture, Members)!;
        bool canceled = false;
        try
        {
            object? captured = capture.Invoke(
                null,
                [Field(storage, EtsCompressed441Names.Accounting), runtime,
                row, cancellation.Token, true, (long)EtsCompressed441Names.KeyField]
            );
            if (captured is not null) EtsFoundation405Access.Invoke(captured, EtsCodecCancellation442Names.Release);
        }
        catch (TargetInvocationException error) when (error.InnerException is OperationCanceledException)
        {
            canceled = true;
        }
        Require(canceled && listener.Cancellations == 1 && listener.Completed > 0 && listener.Completed < listener.Total);

        return new(resource);
    }

    internal static async Task MidDecodeCancellation442()
    {
        await using var runtime = new ProcessRuntime(1);
        Install(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        object storage = EtsFoundation405Access.Get(runtime);
        Term table = await Create(runtime, actor, EtsCompressed441Names.Kinds[0]);
        TupleTerm row = Term.Tuple(new BitString(new byte[EtsCodecCancellation442Names.PayloadBytes]),
            Term.I(EtsCompressed441Names.KeyField));
        await actor.InvokeAsync(async context => await Call(
            runtime,
            context,
            EtsCompressed441Names.Insert,
            table,
            row
        ));
        await actor.InvokeAsync(async context =>
        {
            using var cancellation = new CancellationTokenSource();
            using var listener = new EtsCodecCancellation442Listener(cancellation, EtsCodecCancellation442Names.Decode);
            Term member = await Call(
                runtime,
                context,
                EtsCompressed441Names.Member,
                table,
                row.Items[EtsCompressed441Names.KeyField - 1]
            );
            Require(member.Equals(Term.A(ErlangBooleanAtoms.True)) && listener.Cancellations == 0);

            return true;
        });
        object lease = await actor.Invoke(context => EtsFoundation405Access.Invoke(
            storage,
            EtsCompressed441Names.Acquire,
            context,
            table,
            (long)Property(Slot(storage, table), EtsCompressed441Names.Id)
        )!);
        try
        {
            await actor.Invoke(context =>
            {
                using var cancellation = new CancellationTokenSource();
                using var listener = new EtsCodecCancellation442Listener(cancellation, EtsCodecCancellation442Names.Decode);
                bool canceled = false;
                try
                {
                    EtsFoundation405Access.Invoke(lease, EtsCompressed441Names.Materialize, cancellation.Token);
                }
                catch (OperationCanceledException)
                {
                    canceled = true;
                }
                Require(canceled && listener.Cancellations == 1 && listener.Completed > 0 && listener.Completed < listener.Total);
                Require(row.Equals(EtsFoundation405Access.Invoke(lease, EtsCompressed441Names.Materialize, CancellationToken.None)));

                return true;
            });
            Require((await actor.InvokeAsync(async context => await Call(
                runtime,
                context,
                EtsCompressed441Names.Lookup,
                table,
                row.Items[EtsCompressed441Names.KeyField - 1]
            ))).Equals(Term.List(row)));
            await actor.InvokeAsync(async context => await Call(
                runtime,
                context,
                EtsCompressed441Names.Delete,
                table
            ));
            Require((long)EtsFoundation405Access.Snapshot(storage)[3]! == 1);
            Require(row.Equals(EtsFoundation405Access.Invoke(lease, EtsCompressed441Names.Materialize, CancellationToken.None)));
        }
        finally
        {
            ((IDisposable)lease).Dispose();
        }
        EtsFoundation405Access.Empty(storage);
    }
}
