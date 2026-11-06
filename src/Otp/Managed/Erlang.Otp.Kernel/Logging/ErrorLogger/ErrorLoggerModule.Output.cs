namespace Erlang;

public static partial class ErrorLoggerModule
{
    private static async ValueTask<Term> Logfile(ProcessContext context, Term request)
    {
        switch (request)
        {
            case TupleTerm { Items: [Atom { Name: ErrorLoggerProtocolNames.Open }, var file] }:
                return Cons.Items(await Handlers(context)).Any(handler => Is(handler, ErrorLoggerProtocolNames.ErrorLoggerFileH))
                    ? Term.Tuple(A(ErrorLoggerProtocolNames.Error), A(ErrorLoggerProtocolNames.AllreadyHaveLogfile))
                    : await Self(
                        context,
                        ErrorLoggerFunctions.AddReportHandler,
                        A(ErrorLoggerProtocolNames.ErrorLoggerFileH),
                        file
                    );
            case Atom { Name: ErrorLoggerProtocolNames.Close }:
                if (await Where(context) is not Pid)
                {
                    return Term.Tuple(A(ErrorLoggerProtocolNames.Error), A(ErrorLoggerProtocolNames.ModuleNotFound));
                }
                Term deleted = await CallAt(
                    context,
                    ErrorLoggerFunctions.Logfile,
                    1,
                    ErrorLoggerSourceLines.CloseLogfile,
                    ErrorLoggerDependencyModules.GenEvent,
                    ErrorLoggerFunctions.DeleteHandler,
                    A(Name),
                    A(ErrorLoggerProtocolNames.ErrorLoggerFileH),
                    A(ErrorLoggerProtocolNames.Normal)
                );

                return deleted is TupleTerm { Items: [Atom { Name: ErrorLoggerProtocolNames.Error }, _] } ? deleted : A(ErrorLoggerProtocolNames.Ok);
            case Atom { Name: ErrorLoggerProtocolNames.Filename }:
                if (await Where(context) is Pid)
                {
                    Term filename = await CallAt(
                        context,
                        ErrorLoggerFunctions.Logfile,
                        1,
                        ErrorLoggerSourceLines.LogfileName,
                        ErrorLoggerDependencyModules.GenEvent,
                        ErrorLoggerFunctions.Call,
                        A(Name),
                        A(ErrorLoggerProtocolNames.ErrorLoggerFileH),
                        A(ErrorLoggerProtocolNames.Filename)
                    );
                    if (filename is not TupleTerm { Items: [Atom { Name: ErrorLoggerProtocolNames.Error }, _] })
                    {
                        return filename;
                    }
                }

                return Term.Tuple(A(ErrorLoggerProtocolNames.Error), A(ErrorLoggerProtocolNames.NoLogFile));
            default:
                throw new ErlangException(ErlangErrorReasons.FunctionClause);
        }
    }

    private static bool DefaultTerminal(Term result)
        => result is TupleTerm { Items: [Atom { Name: ErrorLoggerProtocolNames.Ok }, var config] }
            && Field(config, ErrorLoggerMetadataFields.Module, out var module) && Is(module, ErrorLoggerProtocolNames.LoggerStdH)
            && Field(config, ErrorLoggerMetadataFields.Config, out var output) && Field(output, ErrorLoggerMetadataFields.Type, out var type) && Is(type, ErrorLoggerProtocolNames.StandardIo);

    private static FunctionTerm External(
        ProcessContext context,
        string module,
        string function,
        int arity
    )
        => context.Runtime.Functions.External(context.Runtime, new ExternalFunctionIdentity(A(module), A(function), arity));

    private static async ValueTask<Term> Tty(ProcessContext context, bool enabled)
    {
        if (enabled)
        {
            if (Cons.Items(await Handlers(context)).Any(handler => Is(handler, ErrorLoggerProtocolNames.ErrorLoggerTtyH)))
            {
                return A(ErrorLoggerProtocolNames.Ok);
            }
            if (DefaultTerminal(await CallAt(
                context,
                ErrorLoggerFunctions.Tty,
                1,
                ErrorLoggerSourceLines.EnableTerminalConfig,
                ErrorLoggerDependencyModules.Logger,
                ErrorLoggerFunctions.GetHandlerConfig,
                A(ErrorLoggerProtocolNames.Default)
            )))
            {
                await CallAt(
                    context,
                    ErrorLoggerFunctions.Tty,
                    1,
                    ErrorLoggerSourceLines.EnableTerminalFilter,
                    ErrorLoggerDependencyModules.Logger,
                    ErrorLoggerFunctions.RemoveHandlerFilter,
                    A(ErrorLoggerProtocolNames.Default),
                    A(ErrorLoggerProtocolNames.ErrorLoggerTtyFalse)
                );
            }
            else
            {
                Term domain = External(
                    context,
                    ErrorLoggerDependencyModules.LoggerFilters,
                    ErrorLoggerFunctions.Domain,
                    2
                );
                Term filters = Term.List(
                    Term.Tuple(
                        A(ErrorLoggerProtocolNames.RemoteGl),
                        Term.Tuple(
                            External(
                                context,
                                ErrorLoggerDependencyModules.LoggerFilters,
                                ErrorLoggerFunctions.RemoteGl,
                                2
                            ),
                            A(ErrorLoggerProtocolNames.Stop)
                        )
                    ),
                    Term.Tuple(
                        A(ErrorLoggerProtocolNames.Domain),
                        Term.Tuple(
                            domain,
                            Term.Tuple(A(ErrorLoggerProtocolNames.Log), A(ErrorLoggerProtocolNames.Super), Term.List(A(ErrorLoggerProtocolNames.Otp)))
                        )
                    ),
                    Term.Tuple(
                        A(ErrorLoggerProtocolNames.NoDomain),
                        Term.Tuple(domain, Term.Tuple(A(ErrorLoggerProtocolNames.Log), A(ErrorLoggerProtocolNames.Undefined), Nil.Value))
                    )
                );
                await CallAt(
                    context,
                    ErrorLoggerFunctions.Tty,
                    1,
                    ErrorLoggerSourceLines.AddTerminalHandler,
                    ErrorLoggerDependencyModules.Logger,
                    ErrorLoggerFunctions.AddHandler,
                    A(ErrorLoggerProtocolNames.ErrorLoggerTtyTrue),
                    A(ErrorLoggerProtocolNames.LoggerStdH),
                    Map(
                        (ErrorLoggerMetadataFields.FilterDefault, A(ErrorLoggerProtocolNames.Stop)),
                        (ErrorLoggerMetadataFields.Filters, filters),
                        (ErrorLoggerMetadataFields.Formatter, Term.Tuple(
                            A(ErrorLoggerProtocolNames.LoggerFormatter),
                            Map(
                                (ErrorLoggerMetadataFields.LegacyHeader, A(ErrorLoggerProtocolNames.True)),
                                (ErrorLoggerMetadataFields.SingleLine, A(ErrorLoggerProtocolNames.False))
                            )
                        )),
                        (ErrorLoggerMetadataFields.Config, Map((ErrorLoggerMetadataFields.Type, A(ErrorLoggerProtocolNames.StandardIo))))
                    )
                );
            }
        }
        else
        {
            await Self(context, ErrorLoggerFunctions.DeleteReportHandler, A(ErrorLoggerProtocolNames.ErrorLoggerTtyH));
            await CallAt(
                context,
                ErrorLoggerFunctions.Tty,
                1,
                ErrorLoggerSourceLines.RemoveTerminalHandler,
                ErrorLoggerDependencyModules.Logger,
                ErrorLoggerFunctions.RemoveHandler,
                A(ErrorLoggerProtocolNames.ErrorLoggerTtyTrue)
            );
            if (DefaultTerminal(await CallAt(
                context,
                ErrorLoggerFunctions.Tty,
                1,
                ErrorLoggerSourceLines.DisableTerminalConfig,
                ErrorLoggerDependencyModules.Logger,
                ErrorLoggerFunctions.GetHandlerConfig,
                A(ErrorLoggerProtocolNames.Default)
            )))
            {
                Term filter = ErrorLoggerCallbackRecipe.Create(context, false);
                await CallAt(
                    context,
                    ErrorLoggerFunctions.Tty,
                    1,
                    ErrorLoggerSourceLines.AddTerminalHandler,
                    ErrorLoggerDependencyModules.Logger,
                    ErrorLoggerFunctions.AddHandlerFilter,
                    A(ErrorLoggerProtocolNames.Default),
                    A(ErrorLoggerProtocolNames.ErrorLoggerTtyFalse),
                    Term.Tuple(filter, A(ErrorLoggerProtocolNames.Ok))
                );
            }
        }

        return A(ErrorLoggerProtocolNames.Ok);
    }
}
