namespace Erlang;

public static partial class ErrorLoggerModule
{
    private static string Level(string tag) => tag.StartsWith(ErrorLoggerMessagePrefixes.Error, StringComparison.Ordinal)
        ? ErrorLoggerProtocolNames.Error : tag.StartsWith(ErrorLoggerMessagePrefixes.Warning, StringComparison.Ordinal) ? ErrorLoggerProtocolNames.Warning : ErrorLoggerProtocolNames.Notice;

    private static Term ReportCallback(ProcessContext context) => ErrorLoggerCallbackRecipe.Create(context, true);

    private static MapTerm Meta(ProcessContext context, string tag, Term? type = null)
    {
        MapTerm legacy = Map((ErrorLoggerMetadataFields.Tag, A(tag)));
        if (type is not null && !Is(type, ErrorLoggerProtocolNames.Undefined))
        {
            legacy = Map((ErrorLoggerMetadataFields.Tag, A(tag)), (ErrorLoggerMetadataFields.Type, type));
        }
        MapTerm metadata = Map((ErrorLoggerMetadataFields.ReportCb, ReportCallback(context)), (Name, legacy));
        if (type is null || Is(type, ErrorLoggerProtocolNames.Undefined)
            || tag == ErrorLoggerFunctions.ErrorReport && Is(type, ErrorLoggerProtocolNames.StdError)
            || tag == ErrorLoggerFunctions.WarningReport && Is(type, ErrorLoggerProtocolNames.StdWarning)
            || tag == ErrorLoggerFunctions.InfoReport && Is(type, ErrorLoggerProtocolNames.StdInfo))
        {
            return metadata;
        }

        return Map(
            (ErrorLoggerMetadataFields.ReportCb, ReportCallback(context)),
            (Name, legacy),
            (ErrorLoggerMetadataFields.Domain, Term.List(type))
        );
    }

    private static ValueTask<Term> Message(
        ProcessContext context,
        string tag,
        Term format,
        Term arguments
    )
        => Call(
            context,
            ErrorLoggerDependencyModules.Logger,
            ErrorLoggerFunctions.Log,
            A(Level(tag)),
            Map(
                (ErrorLoggerMetadataFields.Label, Term.Tuple(A(Name), A(tag))),
                (ErrorLoggerMetadataFields.Format, format),
                (ErrorLoggerMetadataFields.Args, arguments)
            ),
            Meta(context, tag == ErrorLoggerFunctions.ErrorMsg ? ErrorLoggerProtocolNames.Error : tag)
        );

    private static ValueTask<Term> Report(
        ProcessContext context,
        string tag,
        Term type,
        Term report
    )
        => Call(
            context,
            ErrorLoggerDependencyModules.Logger,
            ErrorLoggerFunctions.Log,
            A(Level(tag)),
            Map((ErrorLoggerMetadataFields.Label, Term.Tuple(A(Name), A(tag))), (ErrorLoggerMetadataFields.Report, report)),
            Meta(context, tag, type)
        );

    private static async ValueTask<Term> ErrorInfo(ProcessContext context, Term error)
    {
        bool printable = false;
        if (error is Cons or Nil)
        {
            using var infoFrame = ErrorLoggerSource.Frame(
                context,
                ErrorLoggerFunctions.ErrorInfo,
                1,
                ErrorLoggerSourceLines.ErrorInfoStringCheck
            );
            Term flattened;
            using (ErrorLoggerSource.Frame(
                context,
                ErrorLoggerProtocolNames.StringP,
                1,
                ErrorLoggerSourceLines.StringFlatten
            ))
            {
                flattened = await Call(
                    context,
                    ErrorLoggerDependencyModules.Lists,
                    ErrorLoggerFunctions.Flatten,
                    error
                );
            }
            printable = flattened is not Nil && Is(
                await Call(
                    context,
                    ErrorLoggerDependencyModules.IoLib,
                    ErrorLoggerFunctions.PrintableList,
                    flattened
                ),
                ErrorLoggerProtocolNames.True
            );
        }

        return await Call(
            context,
            ErrorLoggerDependencyModules.Logger,
            ErrorLoggerFunctions.Log,
            A(ErrorLoggerProtocolNames.Notice),
            printable ? error : Term.String(ErrorLoggerFormats.Term),
            printable ? Nil.Value : Term.List(error),
            Map(
                (Name, Map((ErrorLoggerMetadataFields.Tag, A(ErrorLoggerProtocolNames.Info)), (ErrorLoggerMetadataFields.Type, error))),
                (ErrorLoggerMetadataFields.Domain, Term.List(error))
            )
        );
    }

    internal static async ValueTask<Term> ReportToFormat(ProcessContext context, Term report)
    {
        bool own = Field(report, ErrorLoggerMetadataFields.Label, out var label) && label is TupleTerm { Items: [Atom { Name: Name }, _] };
        if (own && Field(report, ErrorLoggerMetadataFields.Report, out var value) && value is MapTerm)
        {
            return Term.Tuple(Term.String(ErrorLoggerFormats.MapReport), Term.List(value));
        }
        if (own && Field(report, ErrorLoggerMetadataFields.Format, out var format) && Field(report, ErrorLoggerMetadataFields.Args, out var args))
        {
            try
            {
                await Call(
                    context,
                    ErrorLoggerDependencyModules.IoLib,
                    ErrorLoggerFunctions.ScanFormat,
                    format,
                    args
                );

                return Term.Tuple(format, args);
            }
            catch (ErlangException)
            {
                return Term.Tuple(Term.String(ErrorLoggerFormats.InvalidFormat), Term.List(format, args));
            }
        }

        return await Call(
            context,
            ErrorLoggerDependencyModules.Logger,
            ErrorLoggerFunctions.FormatOtpReport,
            report
        );
    }

    private static async ValueTask<Term> Log(
        ProcessContext context,
        Term level,
        Term message,
        Term metadata
    )
    {
        if (!Field(metadata, Name, out var legacy) || !Field(legacy, ErrorLoggerMetadataFields.Tag, out var tag))
        {
            return A(ErrorLoggerProtocolNames.Ok);
        }
        if (message is TupleTerm { Items: [Atom { Name: ErrorLoggerProtocolNames.Report }, var report] })
        {
            if (Field(legacy, ErrorLoggerMetadataFields.Type, out var type))
            {
                Term payload = Field(report, ErrorLoggerMetadataFields.Label, out _) && Field(report, ErrorLoggerMetadataFields.Report, out var body) ? body : report;

                return await Notify(
                    context,
                    level,
                    tag,
                    type,
                    payload,
                    metadata,
                    legacy
                );
            }
            Term pair;
            if (Field(report, ErrorLoggerMetadataFields.Label, out _) && Field(report, ErrorLoggerMetadataFields.Format, out var format) && Field(report, ErrorLoggerMetadataFields.Args, out var args))
            {
                pair = Term.Tuple(format, args);
            }
            else
            {
                Term callback = Field(legacy, ErrorLoggerMetadataFields.ReportCb, out var nested) ? nested
                    : Field(metadata, ErrorLoggerMetadataFields.ReportCb, out var outer) ? outer
                    : context.Runtime.Functions.External(
                        context.Runtime,
                        new ExternalFunctionIdentity(A(ErrorLoggerProtocolNames.Logger), A(ErrorLoggerProtocolNames.FormatReport), 1)
                    );
                pair = await ConvertReport(context, callback, report);
            }
            if (pair is not TupleTerm { Items: [var outputFormat, var outputArguments] })
            {
                throw new ErlangException(Term.Tuple(A(ErrorLoggerProtocolNames.Badmatch), pair));
            }

            return await Notify(
                context,
                level,
                tag,
                outputFormat,
                outputArguments,
                metadata,
                legacy
            );
        }
        if (message is TupleTerm { Items: [Cons or Nil, Cons or Nil] } tuple)
        {
            return await Notify(
                context,
                level,
                tag,
                tuple.Items[0],
                tuple.Items[1],
                metadata,
                legacy
            );
        }

        return A(ErrorLoggerProtocolNames.Ok);
    }

    private static async ValueTask<Term> ConvertReport(ProcessContext context, Term callback, Term report)
    {
        if (callback is not FunctionTerm { Arity: 1 or 2 } function)
        {
            throw ErrorLoggerSource.Capture(
                context,
                Term.Tuple(A(ErrorLoggerProtocolNames.CaseClause), callback),
                [Term.Tuple(
                    A(Name),
                    A(ErrorLoggerProtocolNames.DoLog),
                    Term.I(3),
                    ErrorLoggerSource.Location(ErrorLoggerSourceLines.ReportCallbackCase)
                )]
            );
        }
        // The arity-2 options are evaluated inside the upstream try expression.
        try
        {
            using var frame = ErrorLoggerSource.Frame(
                context,
                ErrorLoggerProtocolNames.DoLog,
                3,
                function.Arity == 1 ? ErrorLoggerSourceLines.ReportCallbackInvoke : ErrorLoggerSourceLines.ReportCallbackOptions
            );
            Term value = function.Arity == 1 ? await function.Invoke(context, [report])
                : await function.Invoke(
                    context,
                    [report, Map(
                        (ErrorLoggerMetadataFields.Depth, await Self(context, ErrorLoggerFunctions.GetFormatDepth)),
                        (ErrorLoggerMetadataFields.CharsLimit, A(ErrorLoggerProtocolNames.Unlimited)),
                        (ErrorLoggerMetadataFields.SingleLine, A(ErrorLoggerProtocolNames.False))
                    )]
                );
            if (function.Arity == 1 && value is TupleTerm { Items: [Cons or Nil, Cons or Nil] })
            {
                return value;
            }
            if (function.Arity == 2 && value is Cons or Nil or BitString { IsBinary: true })
            {
                return Term.Tuple(Term.String(ErrorLoggerFormats.Characters), Term.List(value));
            }

            return Term.Tuple(Term.String(ErrorLoggerFormats.BadReportCallback), Term.List(report, value));
        }
        catch (ErlangException error)
        {
            Term reason = function.Arity == 1
                ? Term.Tuple(A(error.ExceptionClass), error.Reason, error.StackTraceTerm)
                : Term.Tuple(A(error.ExceptionClass), error.Reason);

            return Term.Tuple(Term.String(ErrorLoggerFormats.ReportCallbackCrash), Term.List(report, reason));
        }
    }

    private static async ValueTask<Term> Notify(
        ProcessContext context,
        Term level,
        Term tag,
        Term formatOrType,
        Term payload,
        Term metadata,
        Term legacy
    )
    {
        if (!Field(metadata, ErrorLoggerMetadataFields.Pid, out var pid) || !Field(metadata, ErrorLoggerMetadataFields.Gl, out var leader))
        {
            throw ErrorLoggerSource.Error(
                context,
                ErrorLoggerProtocolNames.Notify,
                [level, tag, formatOrType, payload, metadata],
                ErlangErrorReasons.FunctionClause,
                ErrorLoggerSourceLines.Notify
            );
        }
        if (Is(level, ErrorLoggerProtocolNames.Warning))
        {
            Term mapping = await Call(context, Name, ErrorLoggerFunctions.WarningMap);
            if (Is(mapping, ErrorLoggerProtocolNames.Error) || Is(mapping, ErrorLoggerProtocolNames.Info))
            {
                bool error = Is(mapping, ErrorLoggerProtocolNames.Error);
                tag = Is(tag, ErrorLoggerFunctions.WarningMsg) ? A(error ? ErrorLoggerProtocolNames.Error : ErrorLoggerFunctions.InfoMsg)
                    : Is(tag, ErrorLoggerFunctions.WarningReport) ? A(error ? ErrorLoggerFunctions.ErrorReport : ErrorLoggerFunctions.InfoReport) : tag;
                formatOrType = Is(formatOrType, ErrorLoggerProtocolNames.StdWarning) ? A(error ? ErrorLoggerProtocolNames.StdError : ErrorLoggerProtocolNames.StdInfo) : formatOrType;
            }
        }
        if (Field(legacy, ErrorLoggerMetadataFields.Emulator, out var emulator) && Is(emulator, ErrorLoggerProtocolNames.True))
        {
            pid = A(ErrorLoggerProtocolNames.Emulator);
        }

        return await Call(
            context,
            ErrorLoggerDependencyModules.GenEvent,
            ErrorLoggerFunctions.Notify,
            A(Name),
            Term.Tuple(tag, leader, Term.Tuple(pid, formatOrType, payload))
        );
    }
}
