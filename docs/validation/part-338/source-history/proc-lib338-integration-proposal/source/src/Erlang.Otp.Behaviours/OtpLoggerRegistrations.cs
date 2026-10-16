namespace Erlang.Otp;

internal static class OtpLoggerRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        void Add(string name, int arity, ErlangFunction function)
            => registry.Register(OtpLoggerNames.Logger, name, arity, function);
        Add(OtpLoggerNames.Allow, 2, (context, args) =>
        {
            if (args[1] is not Atom module)
                throw new ErlangException(ErlangErrorReasons.FunctionClause);

            return ValueTask.FromResult<Term>(Term.A(OtpLoggerState.For(context.Runtime).Allow(args[0], module)
                ? OtpLoggerNames.True : OtpLoggerNames.False));
        });
        Add(OtpLoggerNames.MacroLog, 4, (context, args) => args[3] is MapTerm && args[2] is not FunctionTerm
            ? OtpLoggerBackend.MacroLog(context, args[0], args[1], args[2], args[3])
            : OtpLoggerBackend.MacroLog(context, args[0], args[1], Term.Tuple(args[2], args[3]), OtpLoggerTerms.Empty));
        Add(OtpLoggerNames.GetPrimaryConfig, 0, (context, _) =>
        {
            var state = OtpLoggerState.For(context.Runtime);
            lock (state.Gate)
                return ValueTask.FromResult<Term>(state.Primary);
        });
        Add(OtpLoggerNames.SetPrimaryConfig, 1, (context, args) => Primary(context, args[0], false));
        Add(OtpLoggerNames.UpdatePrimaryConfig, 1, (context, args) => Primary(context, args[0], true));
        Add(OtpLoggerNames.SetPrimaryConfig, 2, (context, args)
            => Primary(context, new MapTerm([new(args[0], args[1])]), true));
        Add(OtpLoggerNames.AddHandler, 3, (context, args)
            => OtpLoggerConfiguration.AddHandler(context, args[0], args[1], args[2]));
        Add(OtpLoggerNames.RemoveHandler, 1, (context, args) => OtpLoggerConfiguration.RemoveHandler(context, args[0]));
        Add(OtpLoggerNames.GetHandlerConfig, 1, (context, args) => HandlerConfig(context, args[0]));
        Add(OtpLoggerNames.GetHandlerConfig, 0, async (context, _) =>
        {
            var state = OtpLoggerState.For(context.Runtime);
            Term[] ids;
            lock (state.Gate)
                ids = state.Handlers.Keys.Order().ToArray();
            var configs = new List<Term>();
            foreach (Term id in ids)
            {
                Term config = await HandlerConfig(context, id);
                if (config is TupleTerm { Items: [Atom { Name: OtpLoggerNames.Ok }, var value] })
                    configs.Add(value);
            }

            return Cons.From(configs);
        });
        Add(OtpLoggerNames.GetHandlerIds, 0, (context, _) =>
        {
            var state = OtpLoggerState.For(context.Runtime);
            lock (state.Gate)
                return ValueTask.FromResult(Cons.From(state.HandlerOrder.Order()));
        });
        Add(OtpLoggerNames.SetHandlerConfig, 2, (context, args) => Handler(context, args[0], args[1], false));
        Add(OtpLoggerNames.UpdateHandlerConfig, 2, (context, args) => Handler(context, args[0], args[1], true));
        Add(OtpLoggerNames.SetHandlerConfig, 3, (context, args)
            => OtpLoggerConfiguration.SetHandler(context, args[0], new MapTerm([new(args[1], args[2])]), false, true));
        Add(OtpLoggerNames.AddPrimaryFilter, 2, (context, args) => ValueTask.FromResult(
            OtpLoggerConfiguration.AddFilter(OtpLoggerState.For(context.Runtime), OtpLoggerTerms.Primary, args[0], args[1])));
        Add(OtpLoggerNames.RemovePrimaryFilter, 1, (context, args) => ValueTask.FromResult(
            OtpLoggerConfiguration.RemoveFilter(OtpLoggerState.For(context.Runtime), OtpLoggerTerms.Primary, args[0])));
        Add(OtpLoggerNames.AddHandlerFilter, 3, (context, args) => ValueTask.FromResult(
            OtpLoggerConfiguration.AddFilter(OtpLoggerState.For(context.Runtime), args[0], args[1], args[2])));
        Add(OtpLoggerNames.RemoveHandlerFilter, 2, (context, args) => ValueTask.FromResult(
            OtpLoggerConfiguration.RemoveFilter(OtpLoggerState.For(context.Runtime), args[0], args[1])));
        Add(OtpLoggerNames.SetModuleLevel, 2, (context, args) =>
        {
            _ = OtpLoggerTerms.Severity(args[1], true);
            Term[] modules = args[0] is Atom ? [args[0]] : Cons.Items(args[0]).ToArray();
            if (modules.Any(module => module is not Atom))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            var state = OtpLoggerState.For(context.Runtime);
            lock (state.Gate)
                foreach (Atom module in modules)
                    state.ModuleLevels[module] = args[1];

            return ValueTask.FromResult<Term>(OtpLoggerTerms.Ok);
        });
        Add(OtpLoggerNames.UnsetModuleLevel, 0, (context, _) =>
        {
            var state = OtpLoggerState.For(context.Runtime);
            lock (state.Gate)
                state.ModuleLevels.Clear();

            return ValueTask.FromResult<Term>(OtpLoggerTerms.Ok);
        });
        Add(OtpLoggerNames.UnsetModuleLevel, 1, (context, args) =>
        {
            Term[] modules = args[0] is Atom ? [args[0]] : Cons.Items(args[0]).ToArray();
            if (modules.Any(module => module is not Atom))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            var state = OtpLoggerState.For(context.Runtime);
            lock (state.Gate)
                foreach (Atom module in modules)
                    state.ModuleLevels.Remove(module);

            return ValueTask.FromResult<Term>(OtpLoggerTerms.Ok);
        });
        Add(OtpLoggerNames.GetModuleLevel, 0, (context, _) =>
        {
            var state = OtpLoggerState.For(context.Runtime);
            lock (state.Gate)
                return ValueTask.FromResult(Cons.From(state.ModuleLevels.OrderBy(entry => entry.Key)
                    .Select(entry => Term.Tuple(entry.Key, entry.Value))));
        });
        Add(OtpLoggerNames.GetProcessMetadata, 0, (context, _) => ValueTask.FromResult(
            context.Dictionary.GetValueOrDefault(Term.A(OtpLoggerNames.ProcessMetadataKey), OtpLoggerTerms.Undefined)));
        Add(OtpLoggerNames.SetProcessMetadata, 1, (context, args) => ProcessMetadata(context, args[0], false));
        Add(OtpLoggerNames.UpdateProcessMetadata, 1, (context, args) => ProcessMetadata(context, args[0], true));
        Add(OtpLoggerNames.UnsetProcessMetadata, 0, (context, _) =>
        {
            context.Dictionary.Remove(Term.A(OtpLoggerNames.ProcessMetadataKey));

            return ValueTask.FromResult<Term>(OtpLoggerTerms.Ok);
        });
    }

    private static ValueTask<Term> Primary(ProcessContext context, Term patch, bool update)
        => ValueTask.FromResult(patch is MapTerm config
            ? OtpLoggerConfiguration.SetPrimary(context, config, update)
            : OtpLoggerTerms.Error(OtpLoggerNames.InvalidConfig, patch));

    private static ValueTask<Term> Handler(ProcessContext context, Term id, Term patch, bool update)
        => patch is MapTerm config
            ? OtpLoggerConfiguration.SetHandler(context, id, config, update)
            : ValueTask.FromResult(OtpLoggerTerms.Error(OtpLoggerNames.InvalidConfig, patch));

    private static async ValueTask<Term> HandlerConfig(ProcessContext context, Term id)
    {
        var state = OtpLoggerState.For(context.Runtime);
        MapTerm? config;
        lock (state.Gate)
            state.Handlers.TryGetValue(id, out config);
        if (config is null)
            return OtpLoggerTerms.Error(OtpLoggerNames.NotFound, id);
        var module = (Atom)OtpLoggerTerms.Get(config, OtpLoggerNames.Module);
        if (context.Runtime.Modules.HasExport(module.Name, OtpLoggerNames.FilterConfig, 1))
        {
            try
            {
                return Term.Tuple(OtpLoggerTerms.Ok, await context.Runtime.Modules.Call(context, module.Name, OtpLoggerNames.FilterConfig, config));
            }
            catch (ErlangException)
            {
                // Public logger:get_handler_config retains the stored config on any guest failure.
            }
        }

        return Term.Tuple(OtpLoggerTerms.Ok, config);
    }

    private static ValueTask<Term> ProcessMetadata(ProcessContext context, Term value, bool update)
    {
        if (value is not MapTerm metadata)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        var key = Term.A(OtpLoggerNames.ProcessMetadataKey);
        if (update && context.Dictionary.GetValueOrDefault(key) is MapTerm previous)
            metadata = OtpLoggerTerms.Merge(previous, metadata);
        context.Dictionary[key] = metadata;

        return ValueTask.FromResult<Term>(OtpLoggerTerms.Ok);
    }
}
