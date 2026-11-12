// Derived from lib/kernel/src/logger.erl, Erlang/OTP 29.1.1, Apache-2.0.
// Copyright Ericsson AB 2017-2026. Original notices remain in Otp/Original.
using Erlang.Otp;

namespace Erlang;

/// <summary>Selectable Logger API, sharing the OTP configuration server and handler protocols.</summary>
public static partial class LoggerModule
{
    public const string Name = OtpLoggerNames.Logger;

    public static void Register(ModuleRegistry registry)
        => registry.InstallPreloaded(target =>
        {
            foreach (var export in Exports)
            {
                string function = export.Name;
                target.RegisterReviewedManagedLibrary789(
                    Name,
                    function,
                    export.Arity,
                    (context, arguments) => Invoke(context, function, arguments)
                );
            }
        });

    private static Atom A(string name) => Term.A(name);

    private static bool Is(Term term, string name) => term is Atom atom && atom.Name == name;

    private static ValueTask<Term> Call(
        ProcessContext context,
        string module,
        string function,
        params Term[] arguments
    )
        => context.Runtime.Modules.Call(
            context,
            module,
            function,
            arguments
        );

    private static ValueTask<Term> Server(ProcessContext context, string function, params Term[] arguments)
        => Call(
            context,
            LoggerProtocol.Server,
            function,
            arguments
        );

    private static Term Error(Term reason) => Term.Tuple(A(OtpLoggerNames.Error), reason);

    private static ErlangException FunctionClause() => new(ErlangErrorReasons.FunctionClause);

    public static async ValueTask<Term> Invoke(ProcessContext context, string function, IReadOnlyList<Term> arguments)
    {
        if (!Exports.Contains((function, arguments.Count)))
        {
            throw new ErlangException(ErlangErrorReasons.UndefinedFunction);
        }
        using var tail = context.EnterSourceTailCall(Name, function);
        Term[] a = arguments.ToArray();
        if (LogLevels.Contains(function, StringComparer.Ordinal))
        {
            return await ApiLog(context, A(function), a);
        }
        switch (function)
        {
            case LoggerApi.Log:
                return await ApiLog(context, a[0], a[1..]);
            case LoggerApi.Allow:
                if (a[1] is not Atom)
                {
                    throw LoggerSource.Error(
                        context,
                        function,
                        a,
                        ErlangErrorReasons.FunctionClause,
                        LoggerSource.AllowClause
                    );
                }

                return await Call(
                    context,
                    LoggerProtocol.Configuration,
                    function,
                    a
                );
            case LoggerApi.MacroLog:
                return await Macro(context, a);
            case LoggerApi.AddDefaultMetadata:
                return await DefaultMetadata(context, a[0]);
            case LoggerApi.Timestamp:
                return await Call(
                    context,
                    LoggerProtocol.Os,
                    LoggerProtocol.SystemTime,
                    A(LoggerProtocol.Microsecond)
                );
            case LoggerApi.CompareLevels:
                try
                {
                    int left = OtpLoggerTerms.Severity(a[0], true);
                    int right = OtpLoggerTerms.Severity(a[1], true);

                    return A(left == right ? LoggerProtocol.Equal : left < right ? LoggerProtocol.Greater : LoggerProtocol.Less);
                }
                catch (ErlangException)
                {
                    throw LoggerSource.Error(
                        context,
                        function,
                        a,
                        ErlangErrorReasons.BadArgument,
                        LoggerSource.CompareBadArgument
                    );
                }
            case LoggerApi.SetPrimaryConfig:
            case LoggerApi.UpdatePrimaryConfig:
            case LoggerApi.SetProxyConfig:
            case LoggerApi.UpdateProxyConfig:
                return await Server(
                    context,
                    function is LoggerApi.SetPrimaryConfig or LoggerApi.SetProxyConfig ? LoggerProtocol.SetConfig : LoggerProtocol.UpdateConfig,
                    new[] { A(function is LoggerApi.SetProxyConfig or LoggerApi.UpdateProxyConfig ? LoggerProtocol.Proxy : OtpLoggerNames.Primary) }.Concat(a).ToArray()
                );
            case LoggerApi.SetHandlerConfig:
            case LoggerApi.UpdateHandlerConfig:
                return await Server(context, function == LoggerApi.SetHandlerConfig ? LoggerProtocol.SetConfig : LoggerProtocol.UpdateConfig, a);
            case LoggerApi.UpdateFormatterConfig:
                return await Server(context, function, a.Length == 2 ? a : [a[0], new MapTerm([new(a[1], a[2])])]);
            case LoggerApi.AddPrimaryFilter:
                return await Server(
                    context,
                    LoggerProtocol.AddFilter,
                    A(OtpLoggerNames.Primary),
                    Term.Tuple(a[0], a[1])
                );
            case LoggerApi.RemovePrimaryFilter:
                return await Server(
                    context,
                    function == LoggerApi.AddPrimaryFilter ? LoggerProtocol.AddFilter : LoggerProtocol.RemoveFilter,
                    new[] { A(OtpLoggerNames.Primary) }.Concat(a).ToArray()
                );
            case LoggerApi.AddHandlerFilter:
                return await Server(
                    context,
                    LoggerProtocol.AddFilter,
                    a[0],
                    Term.Tuple(a[1], a[2])
                );
            case LoggerApi.RemoveHandlerFilter:
                return await Server(context, function == LoggerApi.AddHandlerFilter ? LoggerProtocol.AddFilter : LoggerProtocol.RemoveFilter, a);
            case LoggerApi.AddHandler:
                return await AddHandler(
                    context,
                    a[0],
                    a[1],
                    a[2]
                );
            case LoggerApi.RemoveHandler:
                return await Server(context, function, a);
            case LoggerApi.SetModuleLevel:
            case LoggerApi.UnsetModuleLevel:
                if (a.Length > 0 && a[0] is Atom)
                {
                    a[0] = Term.List(a[0]);
                }

                return await Server(context, function, a);
            case LoggerApi.SetApplicationLevel:
            case LoggerApi.UnsetApplicationLevel:
                Term modules = await Call(
                    context,
                    OtpLoggerNames.Application,
                    LoggerProtocol.GetKey,
                    a[0],
                    A(LoggerProtocol.Modules)
                );
                if (modules is TupleTerm { Items: [Atom { Name: OtpLoggerNames.Ok }, var value] })
                {
                    return await Server(
                        context,
                        function == LoggerApi.SetApplicationLevel ? LoggerApi.SetModuleLevel : LoggerApi.UnsetModuleLevel,
                        function == LoggerApi.SetApplicationLevel ? [value, a[1]] : [value]
                    );
                }

                return Error(Term.Tuple(A(LoggerProtocol.NotLoaded), a[0]));
            case LoggerApi.GetModuleLevel:
                Term levels = await Call(context, LoggerProtocol.Configuration, function);
                if (a.Length == 0)
                {
                    return levels;
                }
                if (a[0] is not (Atom or Cons or Nil))
                {
                    throw LoggerSource.Error(
                        context,
                        function,
                        a,
                        ErlangErrorReasons.FunctionClause,
                        LoggerSource.GetModuleLevelClause
                    );
                }
                Term[] selected = a[0] is Atom ? [a[0]] : Cons.Items(a[0]).ToArray();

                return Cons.From(Cons.Items(levels).Where(item => item is TupleTerm tuple && selected.Contains(tuple.Items[0])));
            case LoggerApi.GetPrimaryConfig:
                return OtpLoggerTerms.Without((MapTerm)await Config(context, A(OtpLoggerNames.Primary)), OtpLoggerNames.Handlers);
            case LoggerApi.GetProxyConfig:
                return await Config(context, A(LoggerProtocol.Proxy));
            case LoggerApi.GetHandlerConfig:
                return a.Length == 0 ? await HandlerConfigs(context) : await HandlerConfig(context, a[0]);
            case LoggerApi.GetHandlerIds:
                return Cons.From(Cons.Items(OtpLoggerTerms.Get((MapTerm)await Config(context, A(OtpLoggerNames.Primary)), OtpLoggerNames.Handlers)).Order());
            case LoggerApi.GetConfig:
                return await AllConfig(context);
            case LoggerApi.SetProcessMetadata:
            case LoggerApi.UpdateProcessMetadata:
                if (a[0] is not MapTerm metadata)
                {
                    throw LoggerSource.Error(
                        context,
                        function,
                        a,
                        ErlangErrorReasons.BadArgument,
                        function == LoggerApi.SetProcessMetadata ? LoggerSource.SetMetadataBadArgument : LoggerSource.UpdateMetadataBadArgument
                    );
                }
                var key = A(OtpLoggerNames.ProcessMetadataKey);
                if (function == LoggerApi.UpdateProcessMetadata && context.Dictionary.GetValueOrDefault(key) is MapTerm previous)
                {
                    metadata = OtpLoggerTerms.Merge(previous, metadata);
                }
                context.Dictionary[key] = metadata;

                return OtpLoggerTerms.Ok;
            case LoggerApi.GetProcessMetadata:
                return context.Dictionary.GetValueOrDefault(A(OtpLoggerNames.ProcessMetadataKey), OtpLoggerTerms.Undefined);
            case LoggerApi.UnsetProcessMetadata:
                context.Dictionary.Remove(A(OtpLoggerNames.ProcessMetadataKey));

                return OtpLoggerTerms.Ok;
            case LoggerApi.FormatOtpReport:
                Term report = a[0];
                if (report is MapTerm map && map.TryGet(A(LoggerProtocol.Label), out _) && map.TryGet(A(OtpLoggerNames.Report), out Term? inner))
                {
                    report = inner!;
                }

                return await FormatReport(context, report);
            case LoggerApi.FormatReport:
                return await FormatReport(context, a[0]);
            case LoggerApi.FilterStacktrace:
                var frames = new List<Term>();
                foreach (Term frame in Cons.Items(a[1]))
                {
                    if (frame is TupleTerm { Items.Count: 4 } tuple && tuple.Items[0].Equals(a[0]))
                    {
                        break;
                    }
                    frames.Add(frame);
                }

                return Cons.From(frames);
            case LoggerApi.InternalLog:
                if (a[0] is not Atom level)
                {
                    throw FunctionClause();
                }
                await Call(
                    context,
                    LoggerProtocol.Erlang,
                    LoggerProtocol.DisplayString,
                    Term.String(LoggerProtocol.InternalPrefix + level.Name + LoggerProtocol.InternalSeparator)
                );

                return await Call(
                    context,
                    LoggerProtocol.Erlang,
                    LoggerProtocol.Display,
                    a[1]
                );
            case LoggerApi.AddHandlers:
                return await AddHandlers(context, a[0]);
            case LoggerApi.InternalInitLogger:
                return await InitializeLogger(context);
            case LoggerApi.Reconfigure:
                return await Reconfigure(context);
            case LoggerApi.I:
                return await PrintConfiguration(context, a);
            default:
                throw FunctionClause();
        }
    }

    private static readonly string[] LogLevels = [OtpLoggerNames.Emergency, OtpLoggerNames.Alert, OtpLoggerNames.Critical,
        OtpLoggerNames.Error, OtpLoggerNames.Warning, OtpLoggerNames.Notice, OtpLoggerNames.Info, OtpLoggerNames.Debug];

    private static async ValueTask<Term> Config(ProcessContext context, Term key)
    {
        Term result = await Call(
            context,
            LoggerProtocol.Configuration,
            LoggerProtocol.Get,
            A(Name),
            key
        );
        if (result is TupleTerm { Items: [Atom { Name: OtpLoggerNames.Ok }, var config] })
        {
            return config;
        }

        throw new ErlangException(Term.Tuple(A(ErlangErrorReasons.BadMatch), result));
    }

    private static async ValueTask<Term> FilterConfig(ProcessContext context, MapTerm config)
    {
        try
        {
            return await Call(
                context,
                ((Atom)OtpLoggerTerms.Get(config, OtpLoggerNames.Module)).Name,
                OtpLoggerNames.FilterConfig,
                config
            );
        }
        catch (ErlangException)
        {
            return config;
        }
    }

    private static async ValueTask<Term> HandlerConfig(ProcessContext context, Term id)
    {
        Term result = await Call(
            context,
            LoggerProtocol.Configuration,
            LoggerProtocol.Get,
            A(Name),
            id
        );
        if (result is TupleTerm { Items: [Atom { Name: OtpLoggerNames.Ok }, MapTerm config] })
        {
            return Term.Tuple(OtpLoggerTerms.Ok, await FilterConfig(context, config));
        }

        return result;
    }

    private static async ValueTask<Term> HandlerConfigs(ProcessContext context)
    {
        Term stored = await Call(
            context,
            LoggerProtocol.Configuration,
            LoggerProtocol.Get,
            A(Name)
        );
        var configs = new List<Term>();
        foreach (MapTerm config in Cons.Items(stored))
        {
            configs.Add(await FilterConfig(context, config));
        }

        return Cons.From(configs.OrderBy(value => OtpLoggerTerms.Get((MapTerm)value, OtpLoggerNames.Id)));
    }

    private static async ValueTask<Term> AllConfig(ProcessContext context)
        => OtpLoggerTerms.Map(
            (OtpLoggerNames.Primary, OtpLoggerTerms.Without((MapTerm)await Config(context, A(OtpLoggerNames.Primary)), OtpLoggerNames.Handlers)),
            (OtpLoggerNames.Handlers, await HandlerConfigs(context)),
            (LoggerProtocol.Proxy, await Config(context, A(LoggerProtocol.Proxy))),
            (LoggerProtocol.ModuleLevels, Cons.From(Cons.Items(await Call(context, LoggerProtocol.Configuration, LoggerApi.GetModuleLevel)).Order()))
        );
}
