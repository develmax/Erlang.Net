using System.Runtime.ExceptionServices;

namespace Erlang;

internal static class AllocatorQueryMailbox
{
    internal static async ValueTask<Term> Gather(
        ProcessContext context,
        Term input,
        AllocatorQueryProvider provider
    )
    {
        if (input is not Atom and not Cons and not Nil)
            throw new ErlangException(ErlangErrorReasons.FunctionClause);

        context.Runtime.ThrowIfHalted();
        context.Cancellation.ThrowIfCancellationRequested();
        ReferenceTerm reference = context.Runtime.MakeReference();
        AllocatorQueryRequest request = AllocatorQueryRequest.Create(input, provider.KnownAllocators);
        AllocatorQueryOperation operation = provider.Prepare(context, reference, request);
        var reducer = new AllocatorQueryReplyReducer(request, reference, operation.ReplyCount);
        Term? result = null;
        ExceptionDispatchInfo? primary = null;
        try
        {
            operation.Cancellation.ThrowIfCancellationRequested();
            operation.Start();
            while (reducer.Remaining > 0)
            {
                TupleTerm? reply = await context.ReceiveForOperationAsync<TupleTerm>(message =>
                    message is TupleTerm { Items.Count: 3 } tuple && tuple.Items[0].Equals(reference) ? tuple : null,
                    operation.Cancellation);
                if (reply is null || !reducer.TryAccept(reply))
                    throw new InvalidOperationException(AllocatorQueryMailboxNames.UnexpectedReply);
            }
            await operation.Completion.ConfigureAwait(false);
            result = reducer.Result;
        }
        catch (Exception failure)
        {
            primary = ExceptionDispatchInfo.Capture(failure);
        }
        try
        {
            await operation.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception cleanup)
        {
            if (primary is null)
                primary = ExceptionDispatchInfo.Capture(cleanup);
            else
                primary.SourceException.Data[AllocatorQueryMailboxNames.CleanupFailure] = cleanup;
        }
        if (primary?.SourceException is OperationCanceledException && !context.Cancellation.IsCancellationRequested &&
            operation.ProducerFailure is { } producerFailure and not OperationCanceledException)
        {
            if (primary.SourceException.Data.Contains(AllocatorQueryMailboxNames.CleanupFailure))
                producerFailure.Data[AllocatorQueryMailboxNames.CleanupFailure] = primary.SourceException.Data[AllocatorQueryMailboxNames.CleanupFailure];

            primary = ExceptionDispatchInfo.Capture(producerFailure);
        }
        primary?.Throw();

        return result!;
    }
}
