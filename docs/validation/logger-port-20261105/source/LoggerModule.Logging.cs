// Derived from OTP 29.1.1 logger.erl (Apache-2.0, Ericsson AB).
using Erlang.Otp;

namespace Erlang;

public static partial class LoggerModule
{
    private static async ValueTask<Term> ApiLog(ProcessContext context, Term level, Term[] arguments)
    {
        Term message = arguments[0];
        Term metadata = OtpLoggerTerms.Empty;
        if (arguments.Length == 2)
        {
            if (arguments[1] is MapTerm && arguments[0] is not FunctionTerm)
            {
                metadata = arguments[1];
            }
            else
            {
                message = Term.Tuple(arguments[0], arguments[1]);
            }
        }
        else if (arguments.Length == 3)
        {
            message = Term.Tuple(arguments[0], arguments[1]);
            metadata = arguments[2];
        }
        Term[] allow = metadata is MapTerm map && map.TryGet(A(OtpLoggerNames.Mfa), out Term? mfa)
            && mfa is TupleTerm { Items: [var module, _, _] } ? [level, module] : [level];
        Term allowed = await Call(
            context,
            LoggerProtocol.Configuration,
            LoggerApi.Allow,
            allow
        );
        if (Is(allowed, OtpLoggerNames.False))
        {
            return OtpLoggerTerms.Ok;
        }

        return await LogAllowed(
            context,
            OtpLoggerTerms.Empty,
            level,
            message,
            metadata
        );
    }

    private static ValueTask<Term> Macro(ProcessContext context, Term[] arguments)
    {
        Term message = arguments[2];
        Term metadata = OtpLoggerTerms.Empty;
        if (arguments.Length == 4)
        {
            if (arguments[3] is MapTerm && arguments[2] is not FunctionTerm)
            {
                metadata = arguments[3];
            }
            else
            {
                message = Term.Tuple(arguments[2], arguments[3]);
            }
        }
        else if (arguments.Length == 5)
        {
            message = Term.Tuple(arguments[2], arguments[3]);
            metadata = arguments[4];
        }

        return LogAllowed(
            context,
            arguments[0],
            arguments[1],
            message,
            metadata
        );
    }

    private static async ValueTask<Term> LogAllowed(
        ProcessContext context,
        Term location,
        Term level,
        Term message,
        Term metadata
    )
    {
        if (message is TupleTerm { Items: [FunctionTerm { Arity: 1 } lazy, var lazyArgument] })
        {
            // A try ... of catches evaluation of the try expression only.
            // Errors while processing its result must escape to the caller.
            Term result;
            try
            {
                result = await lazy.Invoke(context, [lazyArgument]);
            }
            catch (ErlangException failure)
            {
                return await LogAllowed(
                    context,
                    location,
                    level,
                    Term.Tuple(Term.String(OtpLoggerNames.LazyFunCrash), Term.List(message, Term.Tuple(A(failure.ExceptionClass), failure.Reason))),
                    metadata
                );
            }
            if (result is TupleTerm { Items: [var value, MapTerm extra] })
            {
                if (metadata is not MapTerm original)
                {
                    throw new ErlangException(Term.Tuple(A(ErlangErrorReasons.BadMap), metadata));
                }
                metadata = OtpLoggerTerms.Merge(original, extra);
                result = value;
            }
            if (Is(result, OtpLoggerNames.Ignore))
            {
                return OtpLoggerTerms.Ok;
            }
            if (!ValidMessage(result))
            {
                result = Term.Tuple(Term.String(OtpLoggerNames.LazyFunError), Term.List(message, result));
            }

            return await LogAllowed(
                context,
                location,
                level,
                result,
                metadata
            );
        }
        if (metadata is not MapTerm callMetadata)
        {
            throw FunctionClause();
        }
        Term tid = await Call(
            context,
            LoggerProtocol.Ets,
            LoggerProtocol.Whereis,
            A(Name)
        );
        Term primaryResult = await Call(
            context,
            LoggerProtocol.Configuration,
            LoggerProtocol.Get,
            tid,
            A(OtpLoggerNames.Primary)
        );
        if (primaryResult is not TupleTerm { Items: [Atom { Name: OtpLoggerNames.Ok }, MapTerm primary] })
        {
            throw new ErlangException(Term.Tuple(A(ErlangErrorReasons.BadMatch), primaryResult));
        }
        if (location is not MapTerm locationMap)
        {
            throw new ErlangException(Term.Tuple(A(ErlangErrorReasons.BadMap), location));
        }
        MapTerm process = context.Dictionary.GetValueOrDefault(A(OtpLoggerNames.ProcessMetadataKey)) as MapTerm ?? OtpLoggerTerms.Empty;
        MapTerm merged = OtpLoggerTerms.Merge(
            locationMap,
            OtpLoggerTerms.Merge((MapTerm)OtpLoggerTerms.Get(primary, OtpLoggerNames.Metadata), OtpLoggerTerms.Merge(process, callMetadata))
        );
        var meta = (MapTerm)await DefaultMetadata(context, merged);
        Term node;
        using (LoggerSource.Scope(
            context,
            LoggerSource.LogAllowed,
            4,
            LoggerSource.GroupLeaderNode
        ))
        {
            node = await Call(
                context,
                LoggerProtocol.Erlang,
                LoggerProtocol.Node,
                OtpLoggerTerms.Get(meta, OtpLoggerNames.Gl)
            );
        }
        Term local = await Call(context, LoggerProtocol.Erlang, LoggerProtocol.Node);
        if (!node.Equals(local))
        {
            Term request = message is TupleTerm { Items: [var format, var args] }
                ? Term.Tuple(
                    A(LoggerApi.Log),
                    level,
                    format,
                    args,
                    meta
                )
                : Term.Tuple(
                    A(LoggerApi.Log),
                    level,
                    message,
                    meta
                );
            await Call(
                context,
                LoggerProtocol.ProxyModule,
                LoggerApi.Log,
                Term.Tuple(A(LoggerProtocol.Remote), node, request)
            );
        }
        _ = OtpLoggerTerms.Severity(level);
        Term wrapped;
        if (message is TupleTerm { Items: [var formatValue, var argumentValue] }
            && OtpLoggerTerms.IsFormat(formatValue) && OtpLoggerTerms.IsList(argumentValue))
        {
            wrapped = Term.Tuple(formatValue is Atom atom ? Term.String(atom.Name) : formatValue, argumentValue);
        }
        else if (IsReport(message))
        {
            wrapped = Term.Tuple(A(OtpLoggerNames.Report), message);
        }
        else if (OtpLoggerTerms.IsString(message))
        {
            wrapped = Term.Tuple(A(OtpLoggerNames.String), message);
        }
        else
        {
            throw FunctionClause();
        }
        MapTerm log = OtpLoggerTerms.Map((OtpLoggerNames.Level, level), (OtpLoggerNames.Msg, wrapped), (OtpLoggerNames.Meta, meta));

        return await Call(
            context,
            LoggerProtocol.Backend,
            LoggerProtocol.LogAllowed,
            log,
            tid,
            primary
        );
    }

    private static bool IsReport(Term value) => value is MapTerm or Cons { Head: TupleTerm };

    private static bool ValidMessage(Term value)
        => IsReport(value) || OtpLoggerTerms.IsString(value)
            || value is TupleTerm { Items: [var format, var args] } && OtpLoggerTerms.IsFormat(format) && OtpLoggerTerms.IsList(args);

    private static async ValueTask<Term> DefaultMetadata(ProcessContext context, Term metadata)
    {
        if (metadata is not MapTerm map)
        {
            throw new ErlangException(Term.Tuple(A(ErlangErrorReasons.BadMap), metadata));
        }
        if (!map.TryGet(A(OtpLoggerNames.Pid), out _))
        {
            map = OtpLoggerTerms.Put(map, OtpLoggerNames.Pid, context.Self);
        }
        if (!map.TryGet(A(OtpLoggerNames.Gl), out _))
        {
            map = OtpLoggerTerms.Put(map, OtpLoggerNames.Gl, context.GroupLeader);
        }
        if (!map.TryGet(A(OtpLoggerNames.Time), out _))
        {
            map = OtpLoggerTerms.Put(map, OtpLoggerNames.Time,
                await Call(
                    context,
                    LoggerProtocol.Os,
                    LoggerProtocol.SystemTime,
                    A(LoggerProtocol.Microsecond)
                ));
        }

        return map;
    }

    private static async ValueTask<bool> Printable(ProcessContext context, Term value)
    {
        try
        {
            Term flat = await Call(
                context,
                LoggerProtocol.Lists,
                LoggerProtocol.Flatten,
                value
            );

            return flat is not Nil && Is(await Call(
                context,
                LoggerProtocol.IoLib,
                LoggerProtocol.PrintableUnicodeList,
                flat
            ), OtpLoggerNames.True);
        }
        catch (ErlangException)
        {
            return false;
        }
    }

    private static async ValueTask<Term> FormatReport(ProcessContext context, Term report)
    {
        if (report is MapTerm map)
        {
            report = await Call(
                context,
                LoggerProtocol.Maps,
                LoggerProtocol.ToList,
                map
            );
        }
        Term flat;
        try
        {
            flat = await Call(
                context,
                LoggerProtocol.Lists,
                LoggerProtocol.Flatten,
                report
            );
        }
        catch (ErlangException)
        {
            return Term.Tuple(Term.String(LoggerProtocol.TermFormat), Term.List(report));
        }
        if (flat is Nil)
        {
            return Term.Tuple(Term.String(LoggerProtocol.TermFormat), Term.List(Nil.Value));
        }
        if (await Printable(context, flat))
        {
            return Term.Tuple(Term.String(LoggerProtocol.StringFormat), Term.List(flat));
        }
        var formats = new List<string>();
        var arguments = new List<Term>();
        foreach (Term item in Cons.Items(report))
        {
            if (item is TupleTerm { Items: [var tag, var data] })
            {
                formats.Add(await Printable(context, data) ? LoggerProtocol.ReportPairStringFormat : LoggerProtocol.ReportPairTermFormat);
                arguments.Add(tag);
                arguments.Add(data);
            }
            else
            {
                formats.Add(LoggerProtocol.ReportTermFormat);
                arguments.Add(item);
            }
        }

        return Term.Tuple(Term.String(string.Join(OtpLoggerNames.Newline, formats)), Cons.From(arguments));
    }
}
