namespace Erlang;

internal sealed partial class SequenceTracing
{
    internal SequenceTraceToken? LinkedExitPrepared(
        ProcessContext sender,
        Pid destination,
        Term reason,
        PreparedSequenceTermination prepared,
        out PreparedMailboxMessage? signal
    )
    {
        lock (gate)
        {
            SequenceTraceToken? token = prepared.ConsumeNext(
                destination,
                reason,
                out PreparedSequenceTraceOutput? plan,
                out signal
            );
            if (token is not null)
                sender.SequentialTrace.Clock = SequenceTraceParameters.Unsigned(token.Serial);
            if (processes.ContainsKey(destination))
                OutputPrepared(
                    sender.Self,
                    token,
                    SequenceTraceFlags.Send,
                    destination,
                    Term.Tuple(Term.A(ProcessMessageTags.LinkedExit), sender.Self, reason),
                    plan
                );

            return token;
        }
    }
}
