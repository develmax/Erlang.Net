namespace Erlang.Otp;

internal static class OtpLoggerConfiguration
{
    public static Term SetPrimary(ProcessContext context, MapTerm patch, bool update)
    {
        Term? error = Validate(patch, true);
        if (error is not null)
            return error;
        var state = OtpLoggerState.For(context.Runtime);
        lock (state.Gate)
            state.Primary = OtpLoggerTerms.Merge(update ? state.Primary : OtpLoggerTerms.PrimaryDefaults(), patch);

        return OtpLoggerTerms.Ok;
    }

    public static async ValueTask<Term> AddHandler(
        ProcessContext context,
        Term id,
        Term moduleTerm,
        Term configTerm
    )
    {
        if (id is not Atom || moduleTerm is not Atom module)
            throw new ErlangException(ErlangErrorReasons.FunctionClause);
        if (configTerm is not MapTerm patch)
            return OtpLoggerTerms.Error(OtpLoggerNames.InvalidConfig, configTerm);
        Term? error = Validate(patch, false);
        if (error is not null)
            return error;
        var state = OtpLoggerState.For(context.Runtime);
        lock (state.Gate)
        {
            if (state.Handlers.ContainsKey(id))
                return OtpLoggerTerms.Error(OtpLoggerNames.AlreadyExist, id);
        }
        MapTerm config = OtpLoggerTerms.Merge(OtpLoggerTerms.HandlerDefaults(id, module), patch);
        Term added = await Callback(context, module, OtpLoggerNames.AddingHandler, [config], Term.Tuple(OtpLoggerTerms.Ok, config));
        if (added is not TupleTerm { Items: [Atom { Name: OtpLoggerNames.Ok }, MapTerm actual] })
        {
            Term reason = added is TupleTerm { Items: [Atom { Name: OtpLoggerNames.Error }, var detail] } ? detail : added;

            return OtpLoggerTerms.Error(OtpLoggerNames.HandlerNotAdded, reason);
        }
        if (module.Name != OtpLoggerNames.LoggerStdH && !context.Runtime.Modules.HasExport(module.Name, OtpLoggerNames.Log, 2))
            return OtpLoggerTerms.Error(OtpLoggerNames.InvalidHandler, Term.Tuple(
                Term.A(OtpLoggerNames.FunctionNotExported),
                Term.Tuple(module, Term.A(OtpLoggerNames.Log), Term.I(2))
            ));
        lock (state.Gate)
        {
            if (state.Handlers.ContainsKey(id))
                return OtpLoggerTerms.Error(OtpLoggerNames.AlreadyExist, id);
            state.Handlers.Add(id, actual);
            state.HandlerOrder.Insert(0, id);
        }

        return OtpLoggerTerms.Ok;
    }

    public static async ValueTask<Term> RemoveHandler(ProcessContext context, Term id)
    {
        var state = OtpLoggerState.For(context.Runtime);
        MapTerm? config;
        lock (state.Gate)
            state.Handlers.TryGetValue(id, out config);
        if (config is null)
            return OtpLoggerTerms.Error(OtpLoggerNames.NotFound, id);
        await Callback(context, (Atom)OtpLoggerTerms.Get(config, OtpLoggerNames.Module), OtpLoggerNames.RemovingHandler, [config], OtpLoggerTerms.Ok);
        lock (state.Gate)
        {
            if (state.Handlers.GetValueOrDefault(id) == config)
            {
                state.Handlers.Remove(id);
                state.HandlerOrder.Remove(id);
            }
        }

        return OtpLoggerTerms.Ok;
    }

    public static async ValueTask<Term> SetHandler(
        ProcessContext context,
        Term id,
        MapTerm patch,
        bool update,
        bool preserve = false
    )
    {
        Term? error = Validate(patch, false);
        if (error is not null)
            return error;
        var state = OtpLoggerState.For(context.Runtime);
        MapTerm? old;
        lock (state.Gate)
            state.Handlers.TryGetValue(id, out old);
        if (old is null)
            return OtpLoggerTerms.Error(OtpLoggerNames.NotFound, id);
        var module = (Atom)OtpLoggerTerms.Get(old, OtpLoggerNames.Module);
        MapTerm next = OtpLoggerTerms.Merge(update || preserve ? old : OtpLoggerTerms.HandlerDefaults(id, module), patch);
        if (!OtpLoggerTerms.Get(next, OtpLoggerNames.Id).Equals(id) || !OtpLoggerTerms.Get(next, OtpLoggerNames.Module).Equals(module))
            return Term.Tuple(Term.A(OtpLoggerNames.Error), Term.Tuple(
                Term.A(OtpLoggerNames.IllegalConfigChange),
                new MapTerm(old.Entries.Where(entry =>
                    (entry.Key.Equals(Term.A(OtpLoggerNames.Id)) || entry.Key.Equals(Term.A(OtpLoggerNames.Module))) &&
                    !entry.Value.Equals(next.Get(entry.Key)))),
                new MapTerm(next.Entries.Where(entry =>
                    (entry.Key.Equals(Term.A(OtpLoggerNames.Id)) || entry.Key.Equals(Term.A(OtpLoggerNames.Module))) &&
                    !entry.Value.Equals(old.Get(entry.Key))))
            ));
        Term changed = await Callback(context, module, OtpLoggerNames.ChangingConfig,
            [Term.A(update ? OtpLoggerNames.Update : OtpLoggerNames.Set), old, next], Term.Tuple(OtpLoggerTerms.Ok, next));
        if (changed is not TupleTerm { Items: [Atom { Name: OtpLoggerNames.Ok }, MapTerm actual] })
            return changed;
        lock (state.Gate)
        {
            if (state.Handlers.GetValueOrDefault(id) == old)
                state.Handlers[id] = actual;
        }

        return OtpLoggerTerms.Ok;
    }

    public static Term AddFilter(OtpLoggerState state, Term owner, Term id, Term filter)
    {
        var candidate = OtpLoggerTerms.Map((OtpLoggerNames.Filters, Term.List(Term.Tuple(id, filter))));
        Term? error = Validate(candidate, false);
        if (error is not null)
            return error;
        lock (state.Gate)
        {
            MapTerm? config = owner.Equals(OtpLoggerTerms.Primary) ? state.Primary : state.Handlers.GetValueOrDefault(owner);
            if (config is null)
                return OtpLoggerTerms.Error(OtpLoggerNames.NotFound, owner);
            Term filters = OtpLoggerTerms.Get(config, OtpLoggerNames.Filters);
            if (Cons.Items(filters).Any(entry => ((TupleTerm)entry).Items[0].Equals(id)))
                return OtpLoggerTerms.Error(OtpLoggerNames.AlreadyExist, id);
            MapTerm next = OtpLoggerTerms.Put(config, OtpLoggerNames.Filters, new Cons(Term.Tuple(id, filter), filters));
            if (owner.Equals(OtpLoggerTerms.Primary))
                state.Primary = next;
            else
                state.Handlers[owner] = next;
        }

        return OtpLoggerTerms.Ok;
    }

    public static Term RemoveFilter(OtpLoggerState state, Term owner, Term id)
    {
        lock (state.Gate)
        {
            MapTerm? config = owner.Equals(OtpLoggerTerms.Primary) ? state.Primary : state.Handlers.GetValueOrDefault(owner);
            if (config is null)
                return OtpLoggerTerms.Error(OtpLoggerNames.NotFound, owner);
            Term[] filters = Cons.Items(OtpLoggerTerms.Get(config, OtpLoggerNames.Filters)).ToArray();
            if (!filters.Any(entry => ((TupleTerm)entry).Items[0].Equals(id)))
                return OtpLoggerTerms.Error(OtpLoggerNames.NotFound, id);
            MapTerm next = OtpLoggerTerms.Put(config, OtpLoggerNames.Filters,
                Cons.From(filters.Where(entry => !((TupleTerm)entry).Items[0].Equals(id))));
            if (owner.Equals(OtpLoggerTerms.Primary))
                state.Primary = next;
            else
                state.Handlers[owner] = next;
        }

        return OtpLoggerTerms.Ok;
    }

    public static async ValueTask<Term> Callback(
        ProcessContext context,
        Atom module,
        string function,
        Term[] args,
        Term fallback
    )
    {
        if (!context.Runtime.Modules.HasExport(module.Name, function, args.Length))
        {
            if (function == OtpLoggerNames.ChangingConfig && args.Length == 3 && context.Runtime.Modules.HasExport(module.Name, function, 2))
                return await Callback(context, module, function, args[1..], fallback);

            return fallback;
        }
        try
        {
            return await context.Runtime.Modules.Call(context, module.Name, function, args);
        }
        catch (ErlangException exception)
        {
            return OtpLoggerTerms.Error(OtpLoggerNames.CallbackCrashed, Term.Tuple(
                Term.A(exception.ExceptionClass), exception.Reason, exception.StackTraceTerm
            ));
        }
    }

    private static Term? Validate(MapTerm config, bool primary)
    {
        foreach (var entry in config.Entries)
        {
            try
            {
                if (entry.Key is Atom { Name: OtpLoggerNames.Level })
                    _ = OtpLoggerTerms.Severity(entry.Value, true);
                else if (entry.Key is Atom { Name: OtpLoggerNames.FilterDefault })
                {
                    if (entry.Value is not Atom { Name: OtpLoggerNames.Log or OtpLoggerNames.Stop })
                        return OtpLoggerTerms.Error(OtpLoggerNames.InvalidFilterDefault, entry.Value);
                }
                else if (entry.Key is Atom { Name: OtpLoggerNames.Filters })
                {
                    Term tail = entry.Value;
                    while (tail is Cons cons)
                    {
                        if (cons.Head is not TupleTerm { Items: [Atom, TupleTerm { Items: [FunctionTerm { Arity: 2 }, _] }] })
                            return OtpLoggerTerms.Error(OtpLoggerNames.InvalidFilter, cons.Head);
                        tail = cons.Tail;
                    }
                    if (tail is not Nil)
                        return OtpLoggerTerms.Error(OtpLoggerNames.InvalidFilters, tail);
                }
                else if (!primary && entry.Key is Atom { Name: OtpLoggerNames.Formatter })
                {
                    if (entry.Value is not TupleTerm { Items: [Atom, _] })
                        return OtpLoggerTerms.Error(OtpLoggerNames.InvalidFormatter, entry.Value);
                }
                else if (primary && (entry.Key is not Atom { Name: OtpLoggerNames.Metadata } || entry.Value is not MapTerm))
                    return OtpLoggerTerms.Error(OtpLoggerNames.InvalidPrimaryConfig, Term.Tuple(entry.Key, entry.Value));
            }
            catch (ErlangException)
            {
                return OtpLoggerTerms.Error(entry.Key.Equals(Term.A(OtpLoggerNames.Level))
                    ? OtpLoggerNames.InvalidLevel : OtpLoggerNames.InvalidFilters, entry.Value);
            }
        }

        return null;
    }
}
