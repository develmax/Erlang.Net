namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal void SendConnectedTraceMessage(
        Pid recipient,
        Term payload,
        Term token,
        Term from
    )
    {
        lock (gate)
        {
            if (!IsCurrentLocal(recipient) || !processes.TryGetValue(recipient, out var target))
                return;
            target.Mailbox.SendTraced(
                payload,
                null,
                false,
                from: from,
                distributionTraceToken: token
            );
        }
    }
}
