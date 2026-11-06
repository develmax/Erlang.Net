// Derived from OTP 29.1.1 logger.erl (Apache-2.0, Ericsson AB).
using Erlang.Otp;

namespace Erlang;

public static partial class LoggerModule
{
    private static ValueTask<Term> Environment(
        ProcessContext context,
        Term app,
        string key,
        Term? fallback = null
    )
        => Call(
            context,
            OtpLoggerNames.Application,
            OtpLoggerNames.GetEnv,
            fallback is null ? [app, A(key)] : [app, A(key), fallback]
        );

    private static void Throw(Term reason) => throw new ErlangException(reason, ErlangExceptionClasses.Throw);

    private static async ValueTask<Term> DefaultFilters(ProcessContext context, bool initial = false)
    {
        Term domain = Term.List(A(LoggerProtocol.Otp));
        if (!initial && Is(
            await Environment(
                context,
                A(OtpLoggerNames.Kernel),
                LoggerProtocol.SaslCompatible,
                A(OtpLoggerNames.False)
            ),
            OtpLoggerNames.False
        ))
        {
            domain = Term.List(A(LoggerProtocol.Otp), A(LoggerProtocol.Sasl));
        }

        FunctionTerm Filter(string function) => context.Runtime.Functions.External(
            context.Runtime,
            new ExternalFunctionIdentity(A(LoggerProtocol.FiltersModule), A(function), 2)
        );

        return Term.List(
            Term.Tuple(A(LoggerProtocol.RemoteGl), Term.Tuple(Filter(LoggerProtocol.RemoteGl), A(OtpLoggerNames.Stop))),
            Term.Tuple(
                A(LoggerProtocol.Domain),
                Term.Tuple(Filter(LoggerProtocol.Domain), Term.Tuple(A(OtpLoggerNames.Log), A(LoggerProtocol.Super), domain))
            ),
            Term.Tuple(
                A(LoggerProtocol.NoDomain),
                Term.Tuple(Filter(LoggerProtocol.Domain), Term.Tuple(A(OtpLoggerNames.Log), A(OtpLoggerNames.Undefined), Nil.Value))
            )
        );
    }

    private static async ValueTask<Term> AddHandler(
        ProcessContext context,
        Term id,
        Term module,
        Term configuration
    )
    {
        bool isDefault = Is(id, OtpLoggerNames.Default);
        if (isDefault && configuration is MapTerm config && !config.TryGet(A(OtpLoggerNames.Filters), out _))
        {
            configuration = OtpLoggerTerms.Merge(
                OtpLoggerTerms.Map(
                (OtpLoggerNames.FilterDefault, A(OtpLoggerNames.Stop)), (OtpLoggerNames.Filters, await DefaultFilters(context))),
                config
            );
        }
        Term result = await Server(
            context,
            LoggerApi.AddHandler,
            id,
            module,
            configuration
        );
        if (isDefault && Is(result, OtpLoggerNames.Ok))
        {
            await Server(context, LoggerApi.RemoveHandler, A(LoggerProtocol.Simple));

            return OtpLoggerTerms.Ok;
        }

        return result;
    }

    private static void CheckConfiguration(Term app, Term configuration)
    {
        Term remaining = configuration;
        while (remaining is Cons cons)
        {
            bool valid = cons.Head is TupleTerm { Items: [Atom { Name: LoggerProtocol.Handler }, _, _, _] };
            if (Is(app, OtpLoggerNames.Kernel))
            {
                valid |= cons.Head is TupleTerm { Items: [Atom { Name: LoggerProtocol.Handler }, Atom { Name: OtpLoggerNames.Default }, Atom { Name: OtpLoggerNames.Undefined }] }
                    or TupleTerm { Items: [Atom { Name: OtpLoggerNames.Filters or LoggerProtocol.ModuleLevel }, _, _] }
                    or TupleTerm { Items: [Atom { Name: LoggerProtocol.Proxy }, _] };
            }
            if (!valid)
            {
                Throw(remaining);
            }
            remaining = cons.Tail;
        }
        if (remaining is not Nil)
        {
            Throw(remaining);
        }
    }

    private static Term SingleOption(
        Term environment,
        string key,
        string multiple,
        Term fallback
    )
    {
        int arity = key == OtpLoggerNames.Filters ? 3 : 2;
        Term[] options = Cons.Items(environment).Where(item => item is TupleTerm tuple && tuple.Items.Count == arity && Is(tuple.Items[0], key)).ToArray();
        if (options.Length > 1)
        {
            Throw(Term.Tuple(A(multiple), environment));
        }

        return options.Length == 0 ? fallback : ((TupleTerm)options[0]).Items[^1];
    }

    private static async ValueTask<Term> AddHandlers(ProcessContext context, Term application)
    {
        Term app = application is Atom ? application : OtpLoggerTerms.Undefined;
        Term configuration = application is Atom ? await Environment(
            context,
            app,
            Name,
            Nil.Value
        ) : application;
        if (Is(app, OtpLoggerNames.Kernel))
        {
            Term options = SingleOption(
                configuration,
                LoggerProtocol.Proxy,
                LoggerProtocol.MultipleProxies,
                OtpLoggerTerms.Undefined
            );
            if (!Is(options, OtpLoggerNames.Undefined))
            {
                Term proxyResult = await Server(
                    context,
                    LoggerProtocol.SetConfig,
                    A(LoggerProtocol.Proxy),
                    options
                );
                if (proxyResult is TupleTerm { Items: [Atom { Name: OtpLoggerNames.Error }, var reason] })
                {
                    return Error(Term.Tuple(A(LoggerProtocol.BadProxyConfig), reason));
                }
            }
        }
        try
        {
            CheckConfiguration(app, configuration);
            foreach (Term item in Cons.Items(configuration))
            {
                if (item is TupleTerm { Items: [Atom { Name: LoggerProtocol.Handler }, var id, var module, var config] })
                {
                    Term result = await AddHandler(
                        context,
                        id,
                        module,
                        config
                    );
                    if (result is TupleTerm { Items: [Atom { Name: OtpLoggerNames.Error }, var reason] })
                    {
                        Throw(reason);
                    }
                }
            }

            return OtpLoggerTerms.Ok;
        }
        catch (ErlangException exception) when (exception.ExceptionClass == ErlangExceptionClasses.Throw)
        {
            Term reason = Is(app, OtpLoggerNames.Undefined) ? exception.Reason : Term.Tuple(app, exception.Reason);
            await ConfigurationError(
                context,
                LoggerApi.AddHandlers,
                2,
                LoggerSource.InvalidHandlerConfiguration,
                LoggerDisplay.InvalidHandlerConfig,
                reason
            );

            return Error(Term.Tuple(A(LoggerProtocol.BadConfig), Term.Tuple(A(LoggerProtocol.Handler), reason)));
        }
    }

    private static async ValueTask<Term> InitializeLogger(ProcessContext context)
    {
        Term kernel = A(OtpLoggerNames.Kernel);
        try
        {
            Term env = await Environment(
                context,
                kernel,
                Name,
                Nil.Value
            );
            CheckConfiguration(kernel, env);
            Term level = await Environment(
                context,
                kernel,
                LoggerProtocol.LoggerLevel,
                A(OtpLoggerNames.Info)
            );
            try
            {
                _ = OtpLoggerTerms.Severity(level, true);
            }
            catch (ErlangException)
            {
                Throw(Term.Tuple(A(LoggerProtocol.LoggerLevel), level));
            }
            Term metaEnv = await Environment(context, kernel, LoggerProtocol.LoggerMetadata);
            Term metadata = metaEnv is TupleTerm { Items: [Atom { Name: OtpLoggerNames.Ok }, var explicitMeta] }
                ? explicitMeta : await Environment(
                    context,
                    kernel,
                    LoggerProtocol.LoggerDefaultMetadata,
                    OtpLoggerTerms.Empty
                );
            if (metadata is not MapTerm)
            {
                Throw(Term.Tuple(A(LoggerProtocol.LoggerMetadata), metadata));
            }
            Term[] filterOptions = Cons.Items(env).Where(item => item is TupleTerm { Items: [Atom { Name: OtpLoggerNames.Filters }, _, _] }).ToArray();
            Term defaultFilter = filterOptions.Length == 0 ? A(OtpLoggerNames.Log) : ((TupleTerm)filterOptions[0]).Items[1];
            await RequireOk(
                context,
                LoggerProtocol.SetConfig,
                A(OtpLoggerNames.Primary),
                A(OtpLoggerNames.Level),
                level
            );
            await RequireOk(
                context,
                LoggerProtocol.SetConfig,
                A(OtpLoggerNames.Primary),
                A(OtpLoggerNames.Metadata),
                metadata
            );
            await RequireOk(
                context,
                LoggerProtocol.SetConfig,
                A(OtpLoggerNames.Primary),
                A(OtpLoggerNames.FilterDefault),
                defaultFilter
            );
            await RequireOk(
                context,
                LoggerProtocol.SetConfig,
                A(OtpLoggerNames.Primary),
                A(OtpLoggerNames.Filters),
                Nil.Value
            );
            Term filters = SingleOption(
                env,
                OtpLoggerNames.Filters,
                LoggerProtocol.MultipleFilters,
                Nil.Value
            );
            if (Cons.Items(filters).Any(item => item is not TupleTerm { Items.Count: 2 }))
            {
                Throw(Term.Tuple(A(OtpLoggerNames.InvalidFilters), filters));
            }
            foreach (TupleTerm filter in Cons.Items(filters))
            {
                await ThrowError(
                    context,
                    LoggerProtocol.AddFilter,
                    A(OtpLoggerNames.Primary),
                    filter
                );
            }
            foreach (Term option in Cons.Items(env))
            {
                if (option is TupleTerm { Items: [Atom { Name: LoggerProtocol.ModuleLevel }, var moduleLevel, var modules] })
                {
                    await ThrowError(
                        context,
                        LoggerApi.SetModuleLevel,
                        modules is Atom ? Term.List(modules) : modules,
                        moduleLevel
                    );
                }
            }
            await Server(
                context,
                LoggerProtocol.SetConfig,
                A(LoggerProtocol.Simple),
                A(OtpLoggerNames.Filters),
                await DefaultFilters(context)
            );

            return await InitializeHandlers(context, env);
        }
        catch (ErlangException exception) when (exception.ExceptionClass == ErlangExceptionClasses.Throw)
        {
            await ConfigurationError(
                context,
                LoggerApi.InternalInitLogger,
                0,
                LoggerSource.InvalidPrimaryConfiguration,
                LoggerDisplay.InvalidConfig,
                exception.Reason
            );

            return Error(Term.Tuple(A(LoggerProtocol.BadConfig), Term.Tuple(kernel, exception.Reason)));
        }
    }

    private static async ValueTask RequireOk(ProcessContext context, string function, params Term[] args)
    {
        Term result = await Server(context, function, args);
        if (!Is(result, OtpLoggerNames.Ok))
        {
            throw new ErlangException(Term.Tuple(A(ErlangErrorReasons.BadMatch), result));
        }
    }

    private static async ValueTask ThrowError(ProcessContext context, string function, params Term[] args)
    {
        Term result = await Server(context, function, args);
        if (result is TupleTerm { Items: [Atom { Name: OtpLoggerNames.Error }, var reason] })
        {
            Throw(reason);
        }
    }

    private static async ValueTask<Term> InitializeHandlers(ProcessContext context, Term environment)
    {
        Term kernel = A(OtpLoggerNames.Kernel);
        try
        {
            Term errorLogger = await Environment(context, kernel, OtpLoggerNames.ErrorLogger);
            Term type;
            if (Is(errorLogger, OtpLoggerNames.Undefined))
            {
                type = Cons.Items(environment).Any(item => item.Equals(Term.Tuple(A(LoggerProtocol.Handler), A(OtpLoggerNames.Default), OtpLoggerTerms.Undefined)))
                    ? A(OtpLoggerNames.False) : A(OtpLoggerNames.StandardIo);
            }
            else if (errorLogger is TupleTerm { Items: [Atom { Name: OtpLoggerNames.Ok }, var configured] }
                && (configured is Atom { Name: LoggerProtocol.Tty or OtpLoggerNames.False or LoggerProtocol.Silent }
                    || configured is TupleTerm { Items: [Atom { Name: LoggerProtocol.File }, Cons or Nil] }))
            {
                type = Is(configured, LoggerProtocol.Tty) ? A(OtpLoggerNames.StandardIo) : configured;
            }
            else
            {
                Term bad = errorLogger is TupleTerm tuple ? tuple.Items[1] : errorLogger;
                Throw(Term.Tuple(A(OtpLoggerNames.ErrorLogger), bad));

                return OtpLoggerTerms.Ok;
            }
            if (Is(type, LoggerProtocol.Silent))
            {
                await RequireOk(context, LoggerApi.RemoveHandler, A(LoggerProtocol.Simple));

                return OtpLoggerTerms.Ok;
            }
            if (Is(type, OtpLoggerNames.False))
            {
                return OtpLoggerTerms.Ok;
            }
            MapTerm formatter = OtpLoggerTerms.Map((OtpLoggerNames.Formatter, Term.Tuple(
                A(OtpLoggerNames.LoggerFormatter),
                OtpLoggerTerms.Map((LoggerProtocol.LegacyHeader, A(OtpLoggerNames.True)), (OtpLoggerNames.SingleLine, A(OtpLoggerNames.False)))
            )));
            MapTerm defaults = OtpLoggerTerms.Put(formatter, OtpLoggerNames.Config, OtpLoggerTerms.Map((OtpLoggerNames.Type, type)));
            var updated = Cons.Items(environment).ToList();
            int position = updated.FindIndex(item => item is TupleTerm { Items.Count: >= 2 } tuple && Is(tuple.Items[1], OtpLoggerNames.Default));
            if (position >= 0 && updated[position] is TupleTerm { Items: [Atom { Name: LoggerProtocol.Handler }, var id, var module, MapTerm config] })
            {
                updated[position] = Term.Tuple(
                    A(LoggerProtocol.Handler),
                    id,
                    module,
                    OtpLoggerTerms.Merge(Is(module, OtpLoggerNames.LoggerStdH) ? defaults : formatter, config)
                );
            }
            else
            {
                updated.Insert(0, Term.Tuple(
                    A(LoggerProtocol.Handler),
                    A(OtpLoggerNames.Default),
                    A(OtpLoggerNames.LoggerStdH),
                    defaults
                ));
            }

            return await Call(
                context,
                OtpLoggerNames.Application,
                OtpLoggerNames.SetEnv,
                kernel,
                A(Name),
                Cons.From(updated),
                Term.List(Term.Tuple(A(LoggerProtocol.Timeout), A(LoggerProtocol.Infinity)))
            );
        }
        catch (ErlangException exception) when (exception.ExceptionClass == ErlangExceptionClasses.Throw)
        {
            await ConfigurationError(
                context,
                LoggerSource.InitializeHandlers,
                1,
                LoggerSource.InvalidDefaultConfiguration,
                LoggerDisplay.InvalidDefaultConfig,
                exception.Reason
            );

            return Error(Term.Tuple(A(LoggerProtocol.BadConfig), Term.Tuple(kernel, exception.Reason)));
        }
    }

    private static async ValueTask ConfigurationError(
        ProcessContext context,
        string function,
        int arity,
        int line,
        string format,
        Term reason
    )
    {
        Term allowed = await Call(
            context,
            LoggerProtocol.Configuration,
            LoggerApi.Allow,
            A(OtpLoggerNames.Error),
            A(Name)
        );
        if (Is(allowed, OtpLoggerNames.True))
        {
            MapTerm location = OtpLoggerTerms.Map(
                (OtpLoggerNames.Mfa, Term.Tuple(A(Name), A(function), Term.I(arity))),
                (ErlangSourceLocationNames.File, Term.String(LoggerSource.File)),
                (ErlangSourceLocationNames.Line, Term.I(line))
            );
            await LogAllowed(
                context,
                location,
                A(OtpLoggerNames.Error),
                Term.Tuple(Term.String(format), Term.List(reason)),
                OtpLoggerTerms.Empty
            );
        }
    }

    private static async ValueTask<Term> Reconfigure(ProcessContext context)
    {
        try
        {
            await AddHandler(
                context,
                A(LoggerProtocol.Simple),
                A(LoggerProtocol.SimpleHandler),
                OtpLoggerTerms.Map((OtpLoggerNames.FilterDefault, A(OtpLoggerNames.Stop)), (OtpLoggerNames.Filters, await DefaultFilters(context, true)))
            );
            Term ids = await Invoke(context, LoggerApi.GetHandlerIds, []);
            foreach (Term id in Cons.Items(ids).Where(id => !Is(id, LoggerProtocol.Simple)))
            {
                Term result = await Server(context, LoggerApi.RemoveHandler, id);
                if (result is TupleTerm { Items: [Atom { Name: OtpLoggerNames.Error }, var reason] })
                {
                    Throw(Term.Tuple(A(LoggerProtocol.Remove), id, reason));
                }
            }
            await Server(context, LoggerApi.UnsetModuleLevel);
            Term initialized = await InitializeLogger(context);

            return Is(initialized, OtpLoggerNames.Ok) ? await AddHandlers(context, A(OtpLoggerNames.Kernel)) : initialized;
        }
        catch (ErlangException exception) when (exception.ExceptionClass == ErlangExceptionClasses.Throw)
        {
            return Error(exception.Reason);
        }
    }
}
