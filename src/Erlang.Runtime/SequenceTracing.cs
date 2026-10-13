namespace Erlang;

internal sealed class SequenceTracing(ProcessRuntime runtime, object gate, Dictionary<Pid, ProcessContext> processes)
{
    private Pid? tracer;

    public SequenceTraceToken? LinkedExit(ProcessContext sender, Pid destination, Term reason)
    {
        lock (gate)
        {
            SequenceTraceToken? token = Advance(sender);
            if (processes.ContainsKey(destination))
                Output(
                    sender,
                    token,
                    SequenceTraceFlags.Send,
                    destination,
                    Term.Tuple(Term.A(ProcessMessageTags.LinkedExit), sender.Self, reason)
                );

            return token;
        }
    }


    public Term Set(ProcessContext context, Term component, Term value)
    {
        lock (gate)
        {
            if (component is not Atom item)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            SequenceTraceState state = context.SequentialTrace;
            if (item.Name == SequenceTraceNames.Token)
            {
                if (value is not Nil)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                Term previous = state.Token?.ToTerm() ?? Nil.Value;
                state.Token = null;

                return previous;
            }
            if (item.Name == SequenceTraceNames.Serial)
            {
                if (value is not TupleTerm { Items: [Integer previous, Integer current] } || previous.Value < SequenceTraceParameters.MinimumSerial || previous.Value > SequenceTraceParameters.MaximumSerial || current.Value < SequenceTraceParameters.MinimumSerial || current.Value > SequenceTraceParameters.MaximumSerial)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                SequenceTraceToken token = EnsureToken(context);
                state.Token = token with { LastCount = (long)previous.Value, Serial = (long)current.Value };
                state.LastCount = SequenceTraceParameters.Unsigned((long)previous.Value);
                state.Clock = SequenceTraceParameters.Unsigned((long)current.Value);

                return Term.Tuple(Term.I(token.LastCount), Term.I(token.Serial));
            }
            if (item.Name == SequenceTraceNames.Label)
            {
                SequenceTraceToken token = EnsureToken(context);
                state.Token = token with { Label = value };

                return token.Label;
            }
            SequenceTraceFlags flag = Flag(item.Name);
            if (flag == SequenceTraceFlags.None || value is not Atom { Name: ErlangBooleanAtoms.True or ErlangBooleanAtoms.False } boolean)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            SequenceTraceToken existing = EnsureToken(context);
            bool old = (existing.Flags & flag) != 0;
            state.Token = existing with { Flags = boolean.Name == ErlangBooleanAtoms.True ? existing.Flags | flag : existing.Flags & ~flag };

            return CoreModules.Boolean(old);
        }
    }

    public Term Info(ProcessContext context, Term component)
    {
        lock (gate)
        {
            if (component is not Atom item)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            SequenceTraceToken? token = context.SequentialTrace.Token;
            SequenceTraceFlags flag = Flag(item.Name);
            if (flag != SequenceTraceFlags.None || token is null && item.Name == SequenceTraceNames.Spawn)
                return Term.Tuple(item, CoreModules.Boolean(token is not null && (token.Flags & flag) != 0));
            if (item.Name is SequenceTraceNames.Label or SequenceTraceNames.Serial)
                return token is null ? Nil.Value : Term.Tuple(item, item.Name == SequenceTraceNames.Label ? token.Label : Term.Tuple(Term.I(token.LastCount), Term.I(token.Serial)));

            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
    }

    public Term Print(ProcessContext context, Term? label, Term message)
    {
        lock (gate)
        {
            SequenceTraceToken? token = context.SequentialTrace.Token;
            if (token is null || label is not null && !label.Equals(token.Label))
                return CoreModules.Boolean(false);
            token = Advance(context);
            Output(
                context,
                token,
                SequenceTraceFlags.Print,
                Nil.Value,
                message
            );

            return CoreModules.Boolean(true);
        }
    }

    public Term SetTracer(Term value)
    {
        lock (gate)
        {
            Pid? next = value is Atom { Name: ErlangBooleanAtoms.False } ? null : value as Pid;
            if (next is null && value is not Atom { Name: ErlangBooleanAtoms.False } || next is not null && (next.Node != runtime.Node || !processes.ContainsKey(next)))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            Term old = Tracer();
            tracer = next;

            return old;
        }
    }

    public Term Tracer()
    {
        lock (gate)
        {
            if (tracer is not null && !processes.ContainsKey(tracer))
                tracer = null;

            return tracer ?? (Term)Term.A(ErlangBooleanAtoms.False);
        }
    }

    public SequenceTraceToken? Sent(ProcessContext sender, Term destination, Term message)
    {
        lock (gate)
        {
            SequenceTraceToken? token = Advance(sender);
            Output(
                sender,
                token,
                SequenceTraceFlags.Send,
                destination,
                message
            );

            return token;
        }
    }

    public void Received(
        ProcessContext receiver,
        Term message,
        SequenceTraceToken? token,
        bool preserve
    )
    {
        if (preserve)
            return;
        lock (gate)
        {
            receiver.SequentialTrace.Token = token;
            if (token is not null)
            {
                ulong serial = SequenceTraceParameters.Unsigned(token.Serial);
                receiver.SequentialTrace.LastCount = serial;
                receiver.SequentialTrace.Clock = Math.Max(receiver.SequentialTrace.Clock, serial);
                Output(
                    receiver,
                    token,
                    SequenceTraceFlags.Receive,
                    receiver.Self,
                    message
                );
            }
        }
    }

    public void Spawned(
        ProcessContext parent,
        ProcessContext child,
        SequenceSpawnDescription? description,
        ReferenceTerm? spawnReference,
        bool asynchronous = false,
        bool successReply = true
    )
    {
        lock (gate)
        {
            if (asynchronous && description is not null)
                description = description.Asynchronous();
            SequenceTraceToken? requestToken = Advance(parent);
            if (requestToken is null)
                return;
            child.SequentialTrace.Token = requestToken;
            child.SequentialTrace.Clock = SequenceTraceParameters.Unsigned(requestToken.Serial);
            child.SequentialTrace.LastCount = child.SequentialTrace.Clock;
            ReferenceTerm reference = spawnReference ?? runtime.MakeReference();
            if (description is not null)
            {
                Term request = Term.Tuple(
                    Term.A(SequenceTraceNames.SpawnRequest),
                    reference,
                    parent.Self,
                    child.GroupLeader,
                    description.Mfa,
                    description.Options,
                    description.ReplyTag ?? Term.A(SequenceTraceNames.SpawnReply),
                    description.Arguments
                );
                Output(
                    parent,
                    requestToken,
                    SequenceTraceFlags.Send,
                    child.Self,
                    request
                );
                Output(
                    parent,
                    requestToken,
                    SequenceTraceFlags.Receive,
                    child.Self,
                    request
                );
            }
            SequenceTraceToken? replyToken = Advance(child);
            if (replyToken is not null)
            {
                if (!asynchronous)
                {
                    parent.SequentialTrace.LastCount = SequenceSpawnSerialLayout.TaggedWord(replyToken.Serial);
                    parent.SequentialTrace.Clock = parent.SequentialTrace.LastCount;
                }
                if (description is not null && successReply)
                {
                    Term reply = Term.Tuple(
                        description.ReplyTag ?? Term.A(SequenceTraceNames.SpawnReply),
                        reference,
                        Term.A(SequenceTraceNames.Success),
                        child.Self
                    );
                    Output(
                        parent,
                        replyToken,
                        SequenceTraceFlags.Send,
                        parent.Self,
                        reply
                    );
                    if (!asynchronous)
                        Output(
                            parent,
                            replyToken,
                            SequenceTraceFlags.Receive,
                            parent.Self,
                            reply
                        );
                }
            }
        }
    }

    private SequenceTraceToken EnsureToken(ProcessContext context)
    {
        return context.SequentialTrace.Token ??= new(
            SequenceTraceFlags.None,
            Term.I(0),
            0,
            context.Self,
            SequenceTraceParameters.Signed(context.SequentialTrace.LastCount)
        );
    }

    private SequenceTraceToken? Advance(ProcessContext context)
    {
        SequenceTraceState state = context.SequentialTrace;
        if (state.Token is null || Tracer().Equals(context.Self))
            return state.Token;
        state.Clock = unchecked(state.Clock + 1);
        state.Token = state.Token with { Sender = context.Self, Serial = SequenceTraceParameters.Signed(state.Clock), LastCount = SequenceTraceParameters.Signed(state.LastCount) };

        return state.Token;
    }

    private void Output(
        ProcessContext context,
        SequenceTraceToken? token,
        SequenceTraceFlags kind,
        Term destination,
        Term message
    )
    {
        if (token is null || (token.Flags & kind) == 0 || Tracer() is not Pid recipient || recipient.Equals(context.Self))
            return;
        string type = kind switch
        {
            SequenceTraceFlags.Send => SequenceTraceNames.Send,
            SequenceTraceFlags.Receive => SequenceTraceNames.Receive,
            _ => SequenceTraceNames.PrintEvent
        };
        Term details = Term.Tuple(
            Term.A(type),
            Term.Tuple(Term.I(token.LastCount), Term.I(token.Serial)),
            token.Sender,
            destination,
            message
        );
        Term notification;
        if ((token.Flags & SequenceTraceFlags.Timestamp) != 0)
            notification = Term.Tuple(
                Term.A(SequenceTraceNames.Message),
                token.Label,
                details,
                ErlangClockState.Timestamp(false)
            );
        else if ((token.Flags & SequenceTraceFlags.StrictMonotonicTimestamp) != 0)
            notification = Term.Tuple(
                Term.A(SequenceTraceNames.Message),
                token.Label,
                details,
                Term.Tuple(
                    new Integer(ErlangClockState.MonotonicNanoseconds),
                    ErlangClockState.Unique([Cons.From([Term.A(ErlangClockNames.Monotonic)])])
                )
            );
        else if ((token.Flags & SequenceTraceFlags.MonotonicTimestamp) != 0)
            notification = Term.Tuple(
                Term.A(SequenceTraceNames.Message),
                token.Label,
                details,
                new Integer(ErlangClockState.MonotonicNanoseconds)
            );
        else
            notification = Term.Tuple(Term.A(SequenceTraceNames.Message), token.Label, details);
        processes[recipient].Mailbox.SendTraced(
            notification,
            null,
            true,
            traceAdmission: false
        );
    }

    private static SequenceTraceFlags Flag(string name) => name switch
    {
        SequenceTraceNames.Send => SequenceTraceFlags.Send,
        SequenceTraceNames.Receive => SequenceTraceFlags.Receive,
        SequenceTraceNames.PrintEvent => SequenceTraceFlags.Print,
        SequenceTraceNames.Timestamp => SequenceTraceFlags.Timestamp,
        SequenceTraceNames.MonotonicTimestamp => SequenceTraceFlags.MonotonicTimestamp,
        SequenceTraceNames.StrictMonotonicTimestamp => SequenceTraceFlags.StrictMonotonicTimestamp,
        _ => SequenceTraceFlags.None
    };
}
