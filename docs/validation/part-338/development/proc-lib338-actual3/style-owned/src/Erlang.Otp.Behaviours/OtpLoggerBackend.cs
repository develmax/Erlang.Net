namespace Erlang.Otp;

internal static class OtpLoggerBackend
{
    public static async ValueTask<Term> MacroLog(
        ProcessContext context,
        Term location,
        Term level,
        Term message,
        Term metadata
    )
    {
        if (message is TupleTerm { Items: [FunctionTerm { Arity: 1 } lazy, var lazyArgs] })
        {
            Term result;
            try
            {
                result = await lazy.Invoke(context, [lazyArgs]);
            }
            catch (ErlangException failure)
            {
                return await MacroLog(
                    context,
                    location,
                    level,
                    Term.Tuple(
                    Term.String(OtpLoggerNames.LazyFunCrash),
                    Term.List(message, Term.Tuple(Term.A(failure.ExceptionClass), failure.Reason))
                ),
                    metadata
                );
            }
            if (result is TupleTerm { Items: [var value, MapTerm extra] } && metadata is MapTerm baseMetadata)
            {
                metadata = OtpLoggerTerms.Merge(baseMetadata, extra);
                result = value;
            }
            if (result.Equals(OtpLoggerTerms.Ignore))
                return OtpLoggerTerms.Ok;
            if (!(OtpLoggerTerms.IsReport(result) || OtpLoggerTerms.IsString(result) ||
                result is TupleTerm { Items: [Term lazyFormat, Term lazyArguments] } &&
                OtpLoggerTerms.IsFormat(lazyFormat) && OtpLoggerTerms.IsList(lazyArguments)))
                result = Term.Tuple(Term.String(OtpLoggerNames.LazyFunError), Term.List(message, result));

            return await MacroLog(
                context,
                location,
                level,
                result,
                metadata
            );
        }
        _ = OtpLoggerTerms.Severity(level);
        if (location is not MapTerm locationMap || metadata is not MapTerm callMetadata)
            throw new ErlangException(ErlangErrorReasons.FunctionClause);
        var state = OtpLoggerState.For(context.Runtime);
        MapTerm primary;
        Term[] handlers;
        lock (state.Gate)
        {
            primary = state.Primary;
            handlers = state.HandlerOrder.ToArray();
        }
        var processMetadata = context.Dictionary.GetValueOrDefault(Term.A(OtpLoggerNames.ProcessMetadataKey)) as MapTerm
            ?? OtpLoggerTerms.Empty;
        MapTerm meta = OtpLoggerTerms.Merge(
            locationMap,
            OtpLoggerTerms.Merge(
            (MapTerm)OtpLoggerTerms.Get(primary, OtpLoggerNames.Metadata),
            OtpLoggerTerms.Merge(processMetadata, callMetadata)
        )
        );
        if (!meta.TryGet(Term.A(OtpLoggerNames.Pid), out _))
            meta = OtpLoggerTerms.Put(meta, OtpLoggerNames.Pid, context.Self);
        if (!meta.TryGet(Term.A(OtpLoggerNames.Gl), out _))
            meta = OtpLoggerTerms.Put(meta, OtpLoggerNames.Gl, context.GroupLeader);
        if (!meta.TryGet(Term.A(OtpLoggerNames.Time), out _))
            meta = OtpLoggerTerms.Put(
                meta,
                OtpLoggerNames.Time,
                Term.I(
                (DateTimeOffset.UtcNow.Ticks - DateTimeOffset.UnixEpoch.Ticks) / TimeSpan.TicksPerMicrosecond
            )
            );
        Term wrappedMessage = message is TupleTerm { Items: [var format, var args] } &&
            OtpLoggerTerms.IsFormat(format) && OtpLoggerTerms.IsList(args)
            ? Term.Tuple(format is Atom atom ? Term.String(atom.Name) : format, args)
            : OtpLoggerTerms.IsReport(message)
                ? Term.Tuple(Term.A(OtpLoggerNames.Report), message)
                : OtpLoggerTerms.IsString(message)
                    ? Term.Tuple(Term.A(OtpLoggerNames.String), message)
                    : throw new ErlangException(ErlangErrorReasons.FunctionClause);
        var log = OtpLoggerTerms.Map(
            (OtpLoggerNames.Level, level),
            (OtpLoggerNames.Msg, wrappedMessage),
            (OtpLoggerNames.Meta, meta)
        );
        var allowed = await Filter(
            context,
            state,
            OtpLoggerTerms.Primary,
            log,
            primary
        );
        if (allowed is null)
            return OtpLoggerTerms.Ok;
        foreach (Term id in handlers)
        {
            MapTerm? config;
            lock (state.Gate)
                state.Handlers.TryGetValue(id, out config);
            if (config is null || OtpLoggerTerms.Severity(OtpLoggerTerms.Get(allowed, OtpLoggerNames.Level)) >
                OtpLoggerTerms.Severity(OtpLoggerTerms.Get(config, OtpLoggerNames.Level), true))
                continue;
            MapTerm? filtered = await Filter(
                context,
                state,
                id,
                allowed,
                config
            );
            if (filtered is null)
                continue;
            var module = (Atom)OtpLoggerTerms.Get(config, OtpLoggerNames.Module);
            var callbackConfig = OtpLoggerTerms.Without(
                config,
                OtpLoggerNames.Level,
                OtpLoggerNames.Filters,
                OtpLoggerNames.FilterDefault,
                OtpLoggerNames.Handlers
            );
            try
            {
                if (module.Name == OtpLoggerNames.LoggerStdH)
                    await OtpLoggerDefaultOutput.Write(context, filtered, callbackConfig);
                else
                    await context.Runtime.Modules.Call(
                        context,
                        module.Name,
                        OtpLoggerNames.Log,
                        filtered,
                        callbackConfig
                    );
            }
            catch (ErlangException)
            {
                await OtpLoggerConfiguration.RemoveHandler(context, id);
            }
        }

        return OtpLoggerTerms.Ok;
    }

    private static async ValueTask<MapTerm?> Filter(
        ProcessContext context,
        OtpLoggerState state,
        Term owner,
        MapTerm log,
        MapTerm config
    )
    {
        MapTerm original = log;
        bool changed = false;
        foreach (Term filter in Cons.Items(OtpLoggerTerms.Get(config, OtpLoggerNames.Filters)))
        {
            var entry = (TupleTerm)filter;
            var callback = (TupleTerm)entry.Items[1];
            Term result;
            try
            {
                result = await ((FunctionTerm)callback.Items[0]).Invoke(context, [log, callback.Items[1]]);
                if (result.Equals(OtpLoggerTerms.Stop))
                    return null;
                if (result.Equals(OtpLoggerTerms.Ignore))
                    continue;
                if (result is not MapTerm next || !ValidEvent(next))
                    throw new ErlangException(Term.Tuple(Term.A(OtpLoggerNames.BadReturnValue), result));
                log = next;
                changed = true;
            }
            catch (ErlangException)
            {
                // The outer OTP apply_filters retains the owner's input event. A failed
                // chain returns ignore, so prior transformations are discarded.
                OtpLoggerConfiguration.RemoveFilter(state, owner, entry.Items[0]);

                return OtpLoggerTerms.Get(config, OtpLoggerNames.FilterDefault).Equals(OtpLoggerTerms.Stop)
                    ? null
                    : original;
            }
        }

        return changed || !OtpLoggerTerms.Get(config, OtpLoggerNames.FilterDefault).Equals(OtpLoggerTerms.Stop) ? log : null;
    }

    private static bool ValidEvent(MapTerm map)
        => map.TryGet(Term.A(OtpLoggerNames.Level), out var level) && level is Atom &&
            map.TryGet(Term.A(OtpLoggerNames.Msg), out var message) && OtpLoggerTerms.IsMessage(message!) &&
            map.TryGet(Term.A(OtpLoggerNames.Meta), out var metadata) && metadata is MapTerm;
}
