namespace Erlang.Otp;

internal static class OtpLoggerDefaultOutput
{
    public static async ValueTask Write(ProcessContext context, MapTerm log, MapTerm config)
    {
        var formatter = (TupleTerm)OtpLoggerTerms.Get(config, OtpLoggerNames.Formatter);
        if (formatter.Items[0] is Atom { Name: not OtpLoggerNames.LoggerFormatter } custom)
        {
            Term rendered;
            try
            {
                rendered = await context.Runtime.Modules.Call(
                    context,
                    custom.Name,
                    OtpLoggerNames.Format,
                    log,
                    formatter.Items[1]
                );
            }
            catch (ErlangException)
            {
                var printer = new ProcLibReportPrettyPrinter(context.Runtime);
                var controls = new ProcLibReportFormatControls(printer);
                rendered = controls.Format(OtpLoggerNames.FormatErrorControl,
                    [OtpLoggerTerms.Get(log, OtpLoggerNames.Msg)], Term.A(OtpLoggerNames.Unlimited));
            }
            WriteChars(context, config, rendered);

            return;
        }
        var message = (TupleTerm)OtpLoggerTerms.Get(log, OtpLoggerNames.Msg);
        var metadata = (MapTerm)OtpLoggerTerms.Get(log, OtpLoggerNames.Meta);
        Term output;
        if (message.Items[0] is Atom { Name: OtpLoggerNames.String })
            output = message.Items[1];
        else if (message.Items[0] is Atom { Name: OtpLoggerNames.Report } &&
            metadata.TryGet(Term.A(OtpLoggerNames.ReportCb), out var callback))
        {
            var options = OtpLoggerTerms.Map(
                (OtpLoggerNames.Depth, await context.Runtime.Modules.Call(
                    context, OtpLoggerNames.ErrorLogger, OtpLoggerNames.GetFormatDepth
                )),
                (OtpLoggerNames.CharsLimit, Term.A(OtpLoggerNames.Unlimited)),
                (OtpLoggerNames.SingleLine, Term.A(OtpLoggerNames.True))
            );
            if (formatter.Items[1] is MapTerm formatterConfig)
            {
                foreach (string key in new[] { OtpLoggerNames.Depth, OtpLoggerNames.CharsLimit, OtpLoggerNames.SingleLine })
                    if (formatterConfig.TryGet(Term.A(key), out var value))
                        options = OtpLoggerTerms.Put(options, key, value!);
            }
            try
            {
                output = callback switch
                {
                    FunctionTerm { Arity: 2 } function => await function.Invoke(context, [message.Items[1], options]),
                    _ => throw new ErlangException(ErlangErrorReasons.BadArgument)
                };
                _ = ProcLibReportCharacters.Measure(output, Term.A(OtpLoggerNames.Unicode));
            }
            catch (ErlangException failure)
            {
                var controls = new ProcLibReportFormatControls(new ProcLibReportPrettyPrinter(context.Runtime));
                output = controls.Format(
                    OtpLoggerNames.ReportCallbackCrash,
                    [message.Items[1], Term.Tuple(Term.A(failure.ExceptionClass), failure.Reason, failure.StackTraceTerm)],
                    Term.A(OtpLoggerNames.Unlimited)
                );
            }
        }
        else
        {
            var printer = new ProcLibReportPrettyPrinter(context.Runtime);
            if (message.Items[0] is Atom { Name: OtpLoggerNames.Report })
                output = printer.Render(
                    message.Items[1],
                    Term.I(-1),
                    Term.A(OtpLoggerNames.Unicode),
                    Term.I(-1),
                    80,
                    1
                );
            else
                output = new ProcLibReportFormatControls(printer).Format(
                    message.Items[0] is Atom atom ? atom.Name : Term.Text(message.Items[0]),
                    Cons.Items(message.Items[1]).ToArray(),
                    Term.A(OtpLoggerNames.Unlimited)
                );
        }
        WriteChars(context, config, Term.List(output, Term.String(OtpLoggerNames.Newline)));
    }

    private static void WriteChars(ProcessContext context, MapTerm config, Term chars)
    {
        string device = OtpLoggerNames.StandardOutput;
        if (config.TryGet(Term.A(OtpLoggerNames.Config), out var handler) && handler is MapTerm handlerConfig &&
            handlerConfig.TryGet(Term.A(OtpLoggerNames.Type), out var type) &&
            type is Atom { Name: OtpLoggerNames.StandardError })
            device = OtpLoggerNames.StandardError;
        try
        {
            context.Runtime.StandardStreams.Write(device, chars);
        }
        catch (ErlangException)
        {
            context.Runtime.StandardStreams.Write(device, Term.String(OtpLoggerNames.FormatterError));
        }
    }
}
