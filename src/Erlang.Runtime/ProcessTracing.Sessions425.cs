namespace Erlang;

internal sealed partial class ProcessTracing
{
    private readonly LinkedList<TraceSessionState425> namedSessions425 = new();
    private long nextSessionId425;
    private bool sessionView425;
    private TraceSessionState425? owningSession425;
    private bool sessionsClosed425;

    private ProcessTracing[] LiveTraceViews425()
    {
        lock (gate)
            return sessionView425 || sessionsClosed425 || namedSessions425.Count == 0 ? []
                : namedSessions425.Reverse().Where(item => item.Alive).Select(item => item.Tracing).ToArray();
    }

    internal Term CreateTraceSession425(IReadOnlyList<Term> arguments)
    {
        if (arguments[0] is not Atom name || arguments[2] is not Nil) throw new TraceValidation425(null);
        lock (gate)
        {
            runtime.ThrowIfHalted();
            if (sessionsClosed425) throw new ObjectDisposedException(InternalTraceNames425.Module);
            Term tracer = arguments[1];
            if (tracer is not Atom { Name: InternalTraceNames425.Undefined }
                && (tracer is not Pid pid || !runtime.IsCurrentLocal(pid)))
            {
                // A tuple is a source-valid custom tracer module/state shape, not badarg.
                if (tracer is TupleTerm { Items.Count: 2 }) throw new NotSupportedException(InternalTraceNames425.Unsupported);
                throw new TraceValidation425(null);
            }
            long id = nextSessionId425;
            nextSessionId425 = nextSessionId425 == InternalTraceNames425.WeakIdMaximum ? InternalTraceNames425.WeakIdMinimum : nextSessionId425 + 1;
            var view = new ProcessTracing(runtime, gate, processes) { sessionView425 = true };
            var state = new TraceSessionState425(
                name,
                id,
                view,
                tracer
            );
            view.owningSession425 = state;
            var handle = new TraceSessionHandle425(this, state);
            state.Handle = new(handle);
            ResourceReferenceTerm resource = runtime.ReferenceResources.CreateTraceSession425(handle);
            Term result = Term.Tuple(resource, state.WeakTerm);
            state.Link = namedSessions425.AddLast(state);

            return result;
        }
    }

    private TraceSessionHandle425 ResolveTraceSession425(Term value, bool allowDead)
    {
        if (value is not TupleTerm { Items.Count: 2 } tuple) throw new TraceValidation425(InternalTraceNames425.Session);
        if (tuple.Items[0] is ReferenceTerm reference && tuple.Items[1] is TupleTerm weak)
        {
            using var lease = runtime.ReferenceResources.AcquireTraceSession425(reference);
            if (lease is null) throw new TraceValidation425(InternalTraceNames425.Session);
            TraceSessionHandle425 handle = lease.Value;
            if (!ReferenceEquals(handle.Root, this) || !handle.State.WeakTerm.Equals(weak)
                || !allowDead && !handle.State.Alive) throw new TraceValidation425(InternalTraceNames425.Session);

            return handle;
        }
        if (tuple.Items[0] is Atom name && tuple.Items[1] is Integer id
            && id.Value >= InternalTraceNames425.WeakIdMinimum && id.Value <= InternalTraceNames425.WeakIdMaximum)
            for (var node = namedSessions425.Last; node is not null; node = node.Previous)
            {
                TraceSessionState425 state = node.Value;
                if (state.Id == (long)id.Value && state.Name.Equals(name) && state.Handle is { } weakHandle
                    && weakHandle.TryGetTarget(out var aliveHandle) && (allowDead || state.Alive)) return aliveHandle;
            }
        throw new TraceValidation425(InternalTraceNames425.Session);
    }

    internal Term DestroyTraceSession425(Term value)
    {
        lock (gate)
        {
            TraceSessionHandle425 handle;
            try
            {
                handle = ResolveTraceSession425(value, true);
            }
            catch (TraceValidation425)
            {
                throw new TraceValidation425(InternalTraceNames425.BadOption);
            }
            bool existed = handle.State.Alive;
            if (existed) ReleaseTraceSession425(handle.State);

            return CoreModules.Boolean(existed);
        }
    }

    internal void ReleaseTraceSession425(TraceSessionState425 state)
    {
        lock (gate)
        {
            if (!state.Alive) return;
            state.Alive = false;
            if (state.Link is { List: not null } link) namedSessions425.Remove(link);
            state.Link = null;
            state.Tracing.ClearTraceLocal425();
        }
    }

    internal void CloseTraceSessions425()
    {
        lock (gate)
        {
            if (sessionView425 || sessionsClosed425) return;
            sessionsClosed425 = true;
            while (namedSessions425.First is { } node) ReleaseTraceSession425(node.Value);
        }
    }

    private void ClearTraceLocal425()
    {
        settings.Clear();
        patterns.Clear();
        sendPattern = null;
        receivePattern = null;
        traceSend = false;
        traceReceive = false;
    }

    private void RequireTraceLive425()
    {
        if (owningSession425 is { Alive: false }) throw new TraceValidation425(InternalTraceNames425.Session);
    }

    internal Term InvokeTrace425(ProcessContext context, string function, IReadOnlyList<Term> arguments)
    {
        if (function == InternalTraceNames425.Create) return CreateTraceSession425(arguments);
        if (function == InternalTraceNames425.Destroy) return DestroyTraceSession425(arguments[0]);
        if (function == InternalTraceNames425.Info && arguments[0] is Atom { Name: InternalTraceNames425.Any })
        {
            lock (gate) return AllSessionInfo425(arguments[1], arguments[2]);
        }
        ProcessTracing target = this;
        TraceSessionHandle425? handle = null;
        int offset = 0;
        bool sessionArgument = function == InternalTraceNames425.Info || arguments.Count == 4;
        if (sessionArgument)
        {
            lock (gate) handle = ResolveTraceSession425(arguments[0], false);
            target = handle.State.Tracing;
            offset = 1;
        }
        Term[] args = arguments.Skip(offset).ToArray();
        if (function == InternalTraceNames425.Trace)
        {
            Pid? fixedTracer = handle?.State.Tracer as Pid;
            if (fixedTracer is not null && Cons.Items(args[2]).Any(value => value is TupleTerm))
                throw new TraceValidation425(InternalTraceNames425.BadOption);
            if (Cons.Items(args[2]).Any(value => value is Atom flag && InternalTraceNames425.UnsupportedFlags.Contains(flag.Name)))
                throw new NotSupportedException(InternalTraceNames425.Unsupported);

            return target.Configure(context, args, fixedTracer);
        }
        // Match-spec compilation and module export enumeration retain the core's
        // existing outside-runtime-lock order. The final apply rechecks session liveness.
        if (function == InternalTraceNames425.Pattern)
        {
            if (Cons.Items(args[2]).Any(value => value is Atom flag && InternalTraceNames425.UnsupportedPatterns.Contains(flag.Name)))
                throw new NotSupportedException(InternalTraceNames425.Unsupported);

            return target.Pattern(context, args);
        }
        if (function == InternalTraceNames425.Info)
        {
            if (args[0] is Atom { Name: InternalTraceNames425.System } && args[1] is Atom { Name: InternalTraceNames425.All })
            {
                lock (gate)
                {
                    target.RequireTraceLive425();

                    return Term.Tuple(Term.A(InternalTraceNames425.System), Nil.Value);
                }
            }

            return target.Info(context, args);
        }
        throw new InvalidOperationException(InternalTraceNames425.Foreign);
    }

    private Term AllSessionInfo425(Term what, Term key)
    {
        if (key is not Atom { Name: InternalTraceNames425.Session }) throw new TraceValidation425(null);
        if (what is Pid target && !processes.ContainsKey(target)) return Term.A(InternalTraceNames425.Undefined);
        IEnumerable<TraceSessionState425> all = namedSessions425.Where(state => state.Alive);
        var result = new List<Term>();

        bool Include(ProcessTracing owner)
        {
            if (what is Atom unsupported && InternalTraceNames425.UnsupportedSessionSelectors.Contains(unsupported.Name))
                throw new NotSupportedException(InternalTraceNames425.Unsupported);
            if (what is Atom atom)
                return atom.Name switch
                {
                    InternalTraceNames425.Any => true,
                    ProcessTraceNames.Send => owner.traceSend,
                    ProcessTraceNames.Receive => owner.traceReceive,
                    _ => throw new TraceValidation425(InternalTraceNames425.BadOption)
                };
            if (what is Pid pid) return owner.Current(pid) is not null;
            throw new NotSupportedException(InternalTraceNames425.Unsupported);
        }
        // C inserts at head then conses while walking: named results are creation
        // order, followed by the immortal legacy descriptor.
        foreach (var state in all) if (Include(state.Tracing)) result.Add(state.WeakTerm);
        if (Include(this)) result.Add(Term.Tuple(Term.A(InternalTraceNames425.Legacy), Term.A(InternalTraceNames425.Default)));

        return Term.Tuple(key, Cons.From(result));
    }
}
