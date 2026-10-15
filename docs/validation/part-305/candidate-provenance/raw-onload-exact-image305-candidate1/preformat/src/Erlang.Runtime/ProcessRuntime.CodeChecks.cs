namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal ValueTask<Term> CheckProcessCode(
        ProcessContext requester,
        Pid target,
        string module,
        bool async,
        Term requestId
    ) => CheckProcessCodeRequest(requester, target, module, null, async, requestId);

    internal ValueTask<Term> CheckProcessCodeImage(
        ProcessContext requester,
        Pid target,
        ModuleCodeImage image,
        bool async,
        Term requestId
    )
    {
        ArgumentNullException.ThrowIfNull(image);

        return CheckProcessCodeRequest(requester, target, image.Module, image, async, requestId);
    }

    private async ValueTask<Term> CheckProcessCodeRequest(
        ProcessContext requester,
        Pid target,
        string module,
        ModuleCodeImage? image,
        bool async,
        Term requestId
    )
    {
        requester.Cancellation.ThrowIfCancellationRequested();
        if (!async && target.Equals(requester.Self))
            return Term.A((image is null ? Modules.IsExecutingOldCode(requester, module) : requester.IsExecutingCode(image)) ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False);
        if (!async)
            requestId = MakeReference();
        var request = new ProcessCodeRequest(requester, requestId, module, image);
        lock (gate)
        {
            if (!processes.TryGetValue(target, out var process))
                ReplyCodeCheck(request, false);
            else
            {
                process.PendingCodeChecks.Add(request);
                process.Mailbox.SendPending(
                    Nil.Value,
                    () =>
                {
                    lock (gate)
                    {
                        if (process.PendingCodeChecks.Remove(request))
                            ReplyCodeCheck(request, request.Image is null ? Modules.IsExecutingOldCode(process, request.Module) : process.IsExecutingCode(request.Image), process.Self);
                    }

                    return null;
                }
                );
            }
        }
        if (async)
            return ProcessCodeAtoms.AsyncResult;
        var reply = await requester.ReceiveAsync(message => message is TupleTerm { Items.Count: 3 } tuple &&
            tuple.Items[0] is Atom { Name: ProcessCodeCheckNames.ReplyTag } && tuple.Items[1].Equals(requestId) ? tuple : null);

        return reply!.Items[2];
    }

    private void ReplyCodeCheck(ProcessCodeRequest request, bool result, Term? from = null) => SendSystem(
        request.Requester.Self,
        Term.Tuple(
        ProcessCodeAtoms.ReplyTag,
        request.Id,
        Term.A(result ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False)
    ),
        from
    );

}
