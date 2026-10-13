namespace Erlang;

internal sealed class ProcessTracing(ProcessRuntime runtime, object gate, Dictionary<Pid, ProcessContext> processes)
{
    private readonly Dictionary<Pid, ProcessTraceSettings> settings = new();
    private readonly Dictionary<(string Module, string Function, int Arity), ProcessTracePattern> patterns = new();
    private bool traceSend = true;
    private bool traceReceive = true;
    private ProcessTracePattern? sendPattern;
    private ProcessTracePattern? receivePattern;
    private readonly ProcessTraceActions callActions = new(true);
    private readonly ProcessTraceActions eventActions = new(false);

    public Term Configure(ProcessContext caller, IReadOnlyList<Term> arguments)
    {
        ProcessTraceFlags flags = ProcessTraceFlags.None;
        Pid? tracer = null;
        bool specifiedTracer = false;
        try
        {
            foreach (Term item in Cons.Items(arguments[2]))
            {
                if (item is TupleTerm { Items: [Atom { Name: ProcessTraceNames.Tracer }, Pid recipient] })
                {
                    tracer = recipient;
                    specifiedTracer = true;
                }
                else if (item is Atom atom && Flag(atom.Name) != ProcessTraceFlags.None)
                    flags |= Flag(atom.Name);
                else
                    throw Invalid(
                        caller,
                        arguments,
                        ProcessTraceNames.Configure,
                        ProcessTraceNames.BadOption
                    );
            }
        }
        catch (ErlangException error) when (!error.ExplicitStackTrace)
        {
            throw Invalid(
                caller,
                arguments,
                ProcessTraceNames.Configure,
                ProcessTraceNames.BadOption
            );
        }
        if (arguments[1] is not Atom { Name: ErlangBooleanAtoms.True or ErlangBooleanAtoms.False } how)
            throw Invalid(caller, arguments, ProcessTraceNames.Configure);
        lock (gate)
        {
            tracer ??= caller.Self;
            if (tracer.Node != runtime.Node || !processes.ContainsKey(tracer))
                throw Invalid(caller, arguments, ProcessTraceNames.Configure);
            Pid[] targets;
            if (arguments[0] is Pid pid && pid.Node == runtime.Node && processes.ContainsKey(pid))
                targets = [pid];
            else if (arguments[0] is Atom { Name: ProcessTraceNames.Existing or ProcessTraceNames.ExistingProcesses })
                targets = flags == ProcessTraceFlags.None ? [] : processes.Keys.ToArray();
            else
                throw Invalid(caller, arguments, ProcessTraceNames.Configure);
            int changed = 0;
            foreach (Pid target in targets)
            {
                settings.TryGetValue(target, out ProcessTraceSettings? previous);
                if (how.Name == ErlangBooleanAtoms.True && previous is not null && !previous.Tracer.Equals(tracer) && processes.ContainsKey(previous.Tracer))
                {
                    if (targets.Length == 1)
                        throw Invalid(caller, arguments, ProcessTraceNames.Configure);
                    continue;
                }
                ProcessTraceFlags next = how.Name == ErlangBooleanAtoms.True ? (previous?.Flags ?? ProcessTraceFlags.None) | flags : (previous?.Flags ?? ProcessTraceFlags.None) & ~flags;
                if (next == ProcessTraceFlags.None)
                    settings.Remove(target);
                else
                    settings[target] = new(next, how.Name == ErlangBooleanAtoms.True || specifiedTracer ? tracer : previous!.Tracer);
                changed++;
            }

            return Term.I(changed);
        }
    }

    public Term Pattern(ProcessContext caller, IReadOnlyList<Term> arguments)
    {
        Term options = arguments.Count == 3 ? arguments[2] : Nil.Value;
        try
        {
            foreach (Term item in Cons.Items(options))
                if (item is not Atom { Name: ProcessTraceNames.Global })
                    throw Invalid(
                        caller,
                        arguments,
                        ProcessTraceNames.Pattern,
                        ProcessTraceNames.BadOption
                    );
        }
        catch (ErlangException error) when (!error.ExplicitStackTrace)
        {
            throw Invalid(
                caller,
                arguments,
                ProcessTraceNames.Pattern,
                ProcessTraceNames.BadOption
            );
        }
        bool enable = arguments[1] is not Atom { Name: ErlangBooleanAtoms.False };
        MatchSpecProgram? program = null;
        if (arguments[1] is not Nil and not Atom { Name: ErlangBooleanAtoms.True or ErlangBooleanAtoms.False })
        {
            MatchSpecCompilation compilation = MatchSpecCompiler.Compile(arguments[1], MatchSpecDialect.Trace);
            if (compilation.Program is null)
                throw Invalid(
                    caller,
                    arguments,
                    ProcessTraceNames.Pattern,
                    ProcessTraceNames.MatchSpec
                );
            program = compilation.Program;
        }
        var pattern = new ProcessTracePattern(program is null ? Nil.Value : arguments[1], program);
        if (arguments[0] is Atom { Name: ProcessTraceNames.Send or ProcessTraceNames.Receive } eventName)
        {
            if (options is not Nil)
                throw Invalid(caller, arguments, ProcessTraceNames.Pattern);
            if (program?.RequiredTraceActions.Any(action => action.Name is MatchSpecNames.ReturnTrace or MatchSpecNames.ExceptionTrace) == true)
                throw Invalid(
                    caller,
                    arguments,
                    ProcessTraceNames.Pattern,
                    ProcessTraceNames.MatchSpec
                );
            RequireActions(program, eventActions);
            lock (gate)
            {
                if (eventName.Name == ProcessTraceNames.Send)
                {
                    traceSend = enable;
                    sendPattern = pattern;
                }
                else
                {
                    traceReceive = enable;
                    receivePattern = pattern;
                }
            }

            return Term.I(1);
        }
        if (arguments[0] is not TupleTerm { Items: [Atom module, Atom function, var arity] } || arity is not Integer && arity is not Atom { Name: ProcessTraceNames.Wildcard } || module.Name == ProcessTraceNames.Wildcard && function.Name != ProcessTraceNames.Wildcard || function.Name == ProcessTraceNames.Wildcard && arity is not Atom { Name: ProcessTraceNames.Wildcard })
            throw Invalid(caller, arguments, ProcessTraceNames.Pattern);
        if (arity is Integer actualArity && (actualArity.Value < ProcessTraceParameters.MinimumMfaArity || actualArity.Value > ProcessTraceParameters.MaximumMfaArity))
            throw Invalid(caller, arguments, ProcessTraceNames.Pattern);
        RequireActions(program, callActions);
        var matches = runtime.Modules.Exports.Where(key => (module.Name == ProcessTraceNames.Wildcard || module.Name == key.Module) && (function.Name == ProcessTraceNames.Wildcard || function.Name == key.Function) && (arity is not Integer number || number.Value == key.Arity)).ToArray();
        lock (gate)
        {
            foreach (var match in matches)
                if (enable)
                    patterns[match] = pattern;
                else
                    patterns.Remove(match);
        }

        return Term.I(matches.Length);
    }

    public Term Info(ProcessContext caller, IReadOnlyList<Term> arguments)
    {
        if (arguments[1] is not Atom key)
            throw Invalid(caller, arguments, ProcessTraceNames.Info);
        lock (gate)
        {
            Term value;
            if (arguments[0] is Pid pid && pid.Node == runtime.Node)
            {
                if (key.Name is not (ProcessTraceNames.Flags or ProcessTraceNames.Tracer))
                    throw Invalid(caller, arguments, ProcessTraceNames.Info);
                if (!processes.ContainsKey(pid))
                    return Term.A(ProcessTraceNames.Undefined);
                ProcessTraceSettings? current = Current(pid);
                value = key.Name == ProcessTraceNames.Tracer ? current?.Tracer ?? (Term)Nil.Value : FlagList(current?.Flags ?? ProcessTraceFlags.None);
            }
            else if (arguments[0] is Atom { Name: ProcessTraceNames.Send or ProcessTraceNames.Receive } eventName && key.Name == ProcessTraceNames.MatchSpec)
            {
                bool enabled = eventName.Name == ProcessTraceNames.Send ? traceSend : traceReceive;
                ProcessTracePattern? eventPattern = eventName.Name == ProcessTraceNames.Send ? sendPattern : receivePattern;
                value = !enabled ? CoreModules.Boolean(false) : eventPattern?.Program is null ? CoreModules.Boolean(true) : eventPattern.Source;
            }
            else if (arguments[0] is TupleTerm { Items: [Atom module, Atom function, Integer arity] } && arity.Value >= ProcessTraceParameters.MinimumMfaArity && arity.Value <= ProcessTraceParameters.MaximumMfaArity)
            {
                if (key.Name is not (ProcessTraceNames.Traced or ProcessTraceNames.MatchSpec))
                    throw Invalid(caller, arguments, ProcessTraceNames.Info);
                if (arity.Value < 0 || arity.Value > int.MaxValue)
                    return Term.A(ProcessTraceNames.Undefined);
                var mfa = (module.Name, function.Name, (int)arity.Value);
                if (!runtime.Modules.HasExport(module.Name, function.Name, (int)arity.Value))
                    return Term.A(ProcessTraceNames.Undefined);
                bool enabled = patterns.TryGetValue(mfa, out ProcessTracePattern? functionPattern);
                value = key.Name == ProcessTraceNames.Traced ? enabled ? Term.A(ProcessTraceNames.Global) : CoreModules.Boolean(false) : enabled ? functionPattern!.Source : CoreModules.Boolean(false);
            }
            else
                throw Invalid(caller, arguments, ProcessTraceNames.Info);

            return Term.Tuple(key, value);
        }
    }

    public ReferenceTerm Delivered(Term target, ProcessContext caller)
    {
        if (target is not Atom { Name: ProcessTraceNames.All } && (target is not Pid pid || pid.Node != runtime.Node || pid.Creation != runtime.InitializationPid.Creation))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        lock (gate)
        {
            ReferenceTerm reference = runtime.MakeReference();
            Term notification = Term.Tuple(Term.A(ProcessTraceNames.Delivered), target, reference);
            caller.Mailbox.SendTraced(
                notification,
                null,
                true,
                traceAdmission: false
            );
            Admitted(caller, notification, Term.A(ProcessTraceNames.System));

            return reference;
        }
    }

    public void Sent(
        ProcessContext sender,
        Term destination,
        Term message,
        bool exists
    )
    {
        lock (gate)
        {
            if (traceSend)
            {
                MatchSpecResult result = Run(
                    sendPattern,
                    sender,
                    Cons.From([destination, message]),
                    eventActions
                );
                if (!result.Matched || result.Value is Atom { Name: ErlangBooleanAtoms.False })
                    return;
                Output(
                    sender.Self,
                    ProcessTraceFlags.Send,
                    [Term.A(exists ? ProcessTraceNames.Send : ProcessTraceNames.MissingSend), message, destination],
                    result.Value
                );
            }
        }
    }

    public void Admitted(ProcessContext receiver, Term message, Term? from = null)
    {
        lock (gate)
        {
            if (traceReceive)
            {
                Term source = from ?? Term.A(ProcessTraceNames.System);
                Term node = source is Pid sender ? Term.A(sender.Node) : source;
                Term pid = source is Pid ? source : Term.A(ProcessTraceNames.Undefined);
                MatchSpecResult result = Run(
                    receivePattern,
                    receiver,
                    Cons.From([node, pid, message]),
                    eventActions
                );
                if (!result.Matched || result.Value is Atom { Name: ErlangBooleanAtoms.False })
                    return;
                Output(
                    receiver.Self,
                    ProcessTraceFlags.Receive,
                    [Term.A(ProcessTraceNames.Receive), message],
                    result.Value
                );
            }
        }
    }

    public ProcessTraceInvocation? Called(
        ProcessContext context,
        string module,
        string function,
        IReadOnlyList<Term> arguments
    )
    {
        lock (gate)
        {
            if (!patterns.TryGetValue((module, function, arguments.Count), out ProcessTracePattern? pattern))
                return null;
            ProcessTraceSettings? current = Current(context.Self);
            if (current is null || (current.Flags & ProcessTraceFlags.Call) == 0)
                return null;
            MatchSpecResult result = Run(
                pattern,
                context,
                Cons.From(arguments),
                callActions
            );
            if (!result.Matched)
                return null;
            Term final = current is not null && (current.Flags & ProcessTraceFlags.Arity) != 0 ? Term.I(arguments.Count) : Cons.From(arguments);
            if (result.Value is not Atom { Name: ErlangBooleanAtoms.False })
                Output(
                    context.Self,
                    ProcessTraceFlags.Call,
                    [Term.A(ProcessTraceNames.Call), Term.Tuple(Term.A(module), Term.A(function), final)],
                    result.Value
                );

            return new(Term.Tuple(Term.A(module), Term.A(function), Term.I(arguments.Count)), result.ReturnTrace, result.ExceptionTrace);
        }
    }

    public ValueTask<Term> Complete(ProcessContext context, ProcessTraceInvocation? invocation, ValueTask<Term> pending)
    {
        if (invocation is null || !invocation.ReturnTrace && !invocation.ExceptionTrace)
            return pending;
        if (pending.IsCompletedSuccessfully)
        {
            Term value = pending.Result;
            Returned(context, invocation, value);

            return ValueTask.FromResult(value);
        }

        return AwaitCompletion(context, invocation, pending);
    }

    private async ValueTask<Term> AwaitCompletion(ProcessContext context, ProcessTraceInvocation invocation, ValueTask<Term> pending)
    {
        try
        {
            Term value = await pending;
            Returned(context, invocation, value);

            return value;
        }
        catch (ErlangException exception)
        {
            Failed(context, invocation, exception);
            throw;
        }
    }

    private void Returned(ProcessContext context, ProcessTraceInvocation invocation, Term value)
    {
        lock (gate)
            if (invocation.ReturnTrace || invocation.ExceptionTrace)
                Output(context.Self, ProcessTraceFlags.Call, [Term.A(ProcessTraceNames.Return), invocation.Mfa, value]);
    }

    public void Failed(ProcessContext context, ProcessTraceInvocation? invocation, ErlangException exception)
    {
        lock (gate)
            if (invocation?.ExceptionTrace == true)
                Output(
                    context.Self,
                    ProcessTraceFlags.Call,
                    [Term.A(ProcessTraceNames.Exception), invocation.Mfa, Term.Tuple(Term.A(exception.ExceptionClass), exception.Reason)]
                );
    }

    private static void RequireActions(MatchSpecProgram? program, ProcessTraceActions actions)
    {
        if (program is null)
            return;
        foreach (MatchSpecTraceAction action in program.RequiredTraceActions)
            if (!actions.Supports(action.Name, action.Arity))
                throw MatchSpecNames.UnsupportedAction(action.Name, action.Arity);
    }

    private static MatchSpecResult Run(
        ProcessTracePattern? pattern,
        ProcessContext context,
        Term against,
        ProcessTraceActions actions
    ) => pattern?.Program?.Run(
        context,
        against,
        MatchSpecRunMode.Production,
        actions
    ) ?? new(
        true,
        CoreModules.Boolean(true),
        false,
        false
    );

    public void Terminated(Pid tracee)
    {
        lock (gate)
            settings.Remove(tracee);
    }

    private ProcessTraceSettings? Current(Pid tracee)
    {
        if (!settings.TryGetValue(tracee, out ProcessTraceSettings? current))
            return null;
        if (!processes.ContainsKey(current.Tracer))
        {
            settings.Remove(tracee);

            return null;
        }

        return current;
    }

    private void Output(
        Pid tracee,
        ProcessTraceFlags flag,
        IReadOnlyList<Term> details,
        Term? matchValue = null
    )
    {
        ProcessTraceSettings? current = Current(tracee);
        if (current is null || (current.Flags & flag) == 0 || current.Tracer.Equals(tracee))
            return;
        var fields = new List<Term> { Term.A(ProcessTraceNames.Message), tracee };
        fields.AddRange(details);
        if (matchValue is not null && matchValue is not Atom { Name: ErlangBooleanAtoms.True })
            fields.Add(matchValue);
        Term? timestamp = null;
        if ((current.Flags & ProcessTraceFlags.Timestamp) != 0)
            timestamp = ErlangClockState.Timestamp(false);
        else if ((current.Flags & ProcessTraceFlags.StrictMonotonicTimestamp) != 0)
            timestamp = Term.Tuple(
                new Integer(ErlangClockState.MonotonicNanoseconds),
                ErlangClockState.Unique([Cons.From([Term.A(ErlangClockNames.Monotonic)])])
            );
        else if ((current.Flags & ProcessTraceFlags.MonotonicTimestamp) != 0)
            timestamp = new Integer(ErlangClockState.MonotonicNanoseconds);
        if (timestamp is not null)
        {
            fields[0] = Term.A(ProcessTraceNames.TimestampedMessage);
            fields.Add(timestamp);
        }
        processes[current.Tracer].Mailbox.SendTraced(
            new TupleTerm(fields),
            null,
            true,
            traceAdmission: false
        );
    }

    private static Term FlagList(ProcessTraceFlags flags) => Cons.From(new[]
    {
        ProcessTraceNames.Arity,
        ProcessTraceNames.Call,
        ProcessTraceNames.Receive,
        ProcessTraceNames.Send,
        ProcessTraceNames.MonotonicTimestamp,
        ProcessTraceNames.StrictMonotonicTimestamp,
        ProcessTraceNames.Timestamp
    }.Where(name => (flags & Flag(name)) != 0).Select(Term.A));

    private static ProcessTraceFlags Flag(string name) => name switch
    {
        ProcessTraceNames.Send => ProcessTraceFlags.Send,
        ProcessTraceNames.Receive => ProcessTraceFlags.Receive,
        ProcessTraceNames.Call => ProcessTraceFlags.Call,
        ProcessTraceNames.Arity => ProcessTraceFlags.Arity,
        ProcessTraceNames.Timestamp => ProcessTraceFlags.Timestamp,
        ProcessTraceNames.MonotonicTimestamp => ProcessTraceFlags.MonotonicTimestamp,
        ProcessTraceNames.StrictMonotonicTimestamp => ProcessTraceFlags.StrictMonotonicTimestamp,
        _ => ProcessTraceFlags.None
    };

    private static ErlangException Invalid(
        ProcessContext caller,
        IReadOnlyList<Term> arguments,
        string function,
        string? cause = null
    )
    {
        var fields = new List<KeyValuePair<Term, Term>> { new(Term.A(ProcessTraceNames.ErrorModule), Term.A(ProcessTraceNames.ErrorFormatter)) };
        if (cause is not null)
            fields.Add(new(Term.A(ProcessTraceNames.Cause), Term.A(cause)));

        return new ErlangException(ErlangErrorReasons.BadArgument)
        {
            ExplicitStackTrace = true,
            StackTraceTerm = caller.CaptureErrorStack(Cons.From(arguments), new MapTerm(fields), function)
        };
    }
}
