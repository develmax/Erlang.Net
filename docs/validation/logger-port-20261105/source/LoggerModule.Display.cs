// Derived from OTP 29.1.1 logger.erl (Apache-2.0, Ericsson AB).
using Erlang.Otp;

namespace Erlang;

public static partial class LoggerModule
{
    private static ValueTask<Term> Print(ProcessContext context, string format, params Term[] args)
        => Call(
            context,
            LoggerDisplay.Io,
            OtpLoggerNames.Format,
            Term.String(format),
            Cons.From(args)
        );

    private static async ValueTask<string> Modifier(ProcessContext context)
    {
        Term options = await Call(context, LoggerDisplay.Io, LoggerDisplay.Getopts);
        Term? encoding = Cons.Items(options).FirstOrDefault(item => item is TupleTerm { Items: [Atom { Name: OtpLoggerNames.Encoding }, _] });

        return encoding is null or TupleTerm { Items: [_, Atom { Name: LoggerDisplay.Latin1 }] } ? string.Empty : LoggerDisplay.UnicodeModifier;
    }

    private static async ValueTask<Term> PrintConfiguration(ProcessContext context, Term[] args)
    {
        if (args.Length > 0 && args[0] is not Atom)
        {
            throw LoggerSource.Error(
                context,
                LoggerApi.I,
                args,
                ErlangErrorReasons.BadArgument,
                LoggerSource.InspectBadArgument
            );
        }
        string? section = args.Length == 0 ? null : ((Atom)args[0]).Name;
        if (section is null)
        {
            var config = (MapTerm)await AllConfig(context);
            string modifier = await Modifier(context);
            await PrintPrimary(context, (MapTerm)OtpLoggerTerms.Get(config, OtpLoggerNames.Primary), modifier);
            await PrintHandlers(context, OtpLoggerTerms.Get(config, OtpLoggerNames.Handlers), modifier);
            await PrintProxy(context, OtpLoggerTerms.Get(config, LoggerProtocol.Proxy), modifier);

            return await PrintModules(context, OtpLoggerTerms.Get(config, LoggerProtocol.ModuleLevels), modifier);
        }
        switch (section)
        {
            case OtpLoggerNames.Primary:
                return await PrintPrimary(context, (MapTerm)await Config(context, A(section)), await Modifier(context));
            case OtpLoggerNames.Handlers:
                return await PrintHandlers(context, await HandlerConfigs(context), await Modifier(context));
            case LoggerProtocol.Proxy:
                return await PrintProxy(context, await Config(context, A(section)), await Modifier(context));
            case LoggerProtocol.Modules:
                return await PrintModules(context, await Call(context, LoggerProtocol.Configuration, LoggerApi.GetModuleLevel), await Modifier(context));
            default:
                Term handler = await HandlerConfig(context, args[0]);
                if (handler is TupleTerm { Items: [Atom { Name: OtpLoggerNames.Ok }, var value] })
                {
                    return await PrintHandlers(context, Term.List(value), await Modifier(context));
                }

                return handler;
        }
    }

    private static async ValueTask<Term> PrintPrimary(ProcessContext context, MapTerm config, string modifier)
    {
        await Print(context, LoggerDisplay.PrimaryHeading);
        await Print(context, LoggerDisplay.PrimaryLevel, OtpLoggerTerms.Get(config, OtpLoggerNames.Level));
        await Print(context, LoggerDisplay.PrimaryDefault, OtpLoggerTerms.Get(config, OtpLoggerNames.FilterDefault));
        await Print(context, LoggerDisplay.PrimaryFilters);

        return await PrintFilters(
            context,
            LoggerDisplay.FilterIndent,
            OtpLoggerTerms.Get(config, OtpLoggerNames.Filters),
            modifier
        );
    }

    private static async ValueTask<Term> PrintFilters(
        ProcessContext context,
        string indent,
        Term filters,
        string modifier
    )
    {
        if (filters is TupleTerm { Items: [var id, TupleTerm { Items: [var fun, var arg] }] })
        {
            return await Print(
                context,
                string.Format(System.Globalization.CultureInfo.InvariantCulture, LoggerDisplay.FilterFormat, modifier),
                Term.String(indent),
                id,
                Term.String(indent),
                fun,
                Term.String(indent),
                arg
            );
        }
        if (filters is Nil)
        {
            return await Print(context, LoggerDisplay.NoFilters, Term.String(indent));
        }
        foreach (Term filter in Cons.Items(filters))
        {
            await PrintFilters(
                context,
                indent,
                filter,
                modifier
            );
        }

        return OtpLoggerTerms.Ok;
    }

    private static async ValueTask<Term> PrintHandlers(ProcessContext context, Term handlers, string modifier)
    {
        await Print(context, LoggerDisplay.HandlerHeading);
        if (handlers is Nil)
        {
            return await Print(context, LoggerDisplay.None);
        }
        foreach (MapTerm config in Cons.Items(handlers))
        {
            var formatter = (TupleTerm)OtpLoggerTerms.Get(config, OtpLoggerNames.Formatter);
            await Print(
                context,
                string.Format(System.Globalization.CultureInfo.InvariantCulture, LoggerDisplay.HandlerFormat, modifier),
                OtpLoggerTerms.Get(config, OtpLoggerNames.Id),
                OtpLoggerTerms.Get(config, OtpLoggerNames.Module),
                OtpLoggerTerms.Get(config, OtpLoggerNames.Level),
                formatter.Items[0]
            );
            await PrintCustom(
                context,
                LoggerDisplay.FormatterIndent,
                formatter.Items[1],
                modifier
            );
            await Print(context, LoggerDisplay.HandlerFilters, OtpLoggerTerms.Get(config, OtpLoggerNames.FilterDefault));
            await PrintFilters(
                context,
                LoggerDisplay.HandlerIndent,
                OtpLoggerTerms.Get(config, OtpLoggerNames.Filters),
                modifier
            );
            if (config.TryGet(A(OtpLoggerNames.Config), out Term? handlerConfig))
            {
                await Print(context, LoggerDisplay.HandlerConfigHeading);
                await PrintCustom(
                    context,
                    LoggerDisplay.HandlerIndent,
                    handlerConfig!,
                    modifier
                );
            }
            MapTerm custom = OtpLoggerTerms.Without(
                config,
                OtpLoggerNames.FilterDefault,
                OtpLoggerNames.Filters,
                OtpLoggerNames.Formatter,
                OtpLoggerNames.Level,
                OtpLoggerNames.Module,
                OtpLoggerNames.Id,
                OtpLoggerNames.Config
            );
            if (custom.Entries.Any())
            {
                await Print(context, LoggerDisplay.CustomConfigHeading);
                await PrintCustom(
                    context,
                    LoggerDisplay.HandlerIndent,
                    custom,
                    modifier
                );
            }
        }

        return OtpLoggerTerms.Ok;
    }

    private static async ValueTask<Term> PrintProxy(ProcessContext context, Term proxy, string modifier)
    {
        await Print(context, LoggerDisplay.ProxyHeading);

        return await PrintCustom(
            context,
            LoggerDisplay.Indent,
            proxy,
            modifier
        );
    }

    private static async ValueTask<Term> PrintCustom(
        ProcessContext context,
        string indent,
        Term value,
        string modifier
    )
    {
        if (value is TupleTerm { Items: [var key, var item] })
        {
            return await Print(
                context,
                string.Format(System.Globalization.CultureInfo.InvariantCulture, LoggerDisplay.PairFormat, modifier),
                Term.String(indent),
                key,
                item
            );
        }
        if (value is MapTerm map)
        {
            value = Cons.From(map.Entries.OrderBy(pair => pair.Key).Select(pair => Term.Tuple(pair.Key, pair.Value)));

            return await PrintCustom(
                context,
                indent,
                value,
                modifier
            );
        }
        if (value is Cons { Head: TupleTerm })
        {
            foreach (Term pair in Cons.Items(value))
            {
                await PrintCustom(
                    context,
                    indent,
                    pair,
                    modifier
                );
            }

            return OtpLoggerTerms.Ok;
        }

        return await Print(
            context,
            string.Format(System.Globalization.CultureInfo.InvariantCulture, LoggerDisplay.ValueFormat, modifier),
            Term.String(indent),
            value
        );
    }

    private static async ValueTask<Term> PrintModules(ProcessContext context, Term modules, string modifier)
    {
        await Print(context, LoggerDisplay.ModulesHeading);
        if (modules is Nil)
        {
            return await Print(context, LoggerDisplay.None);
        }
        foreach (TupleTerm module in Cons.Items(modules))
        {
            await Print(
                context,
                string.Format(System.Globalization.CultureInfo.InvariantCulture, LoggerDisplay.ModuleFormat, modifier),
                module.Items[0],
                module.Items[1]
            );
        }

        return OtpLoggerTerms.Ok;
    }
}
