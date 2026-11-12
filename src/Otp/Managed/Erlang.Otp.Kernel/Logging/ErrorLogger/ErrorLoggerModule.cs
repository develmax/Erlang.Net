// Ported from lib/kernel/src/error_logger.erl, OTP 29.1.1 (Apache-2.0).
// Copyright Ericsson AB 1996-2026. Original notices are retained in Otp/Original.
using Erlang.Otp;

namespace Erlang;

public static partial class ErrorLoggerModule
{
    public const string Name = "error_logger";

    public static IReadOnlyList<(string Name, int Arity)> Exports { get; } = Array.AsReadOnly(new (string, int)[]
    {
        (ErrorLoggerFunctions.Start, 0), (ErrorLoggerFunctions.StartLink, 0), (ErrorLoggerFunctions.Stop, 0),
        (ErrorLoggerFunctions.Format, 2), (ErrorLoggerFunctions.ErrorMsg, 1), (ErrorLoggerFunctions.ErrorMsg, 2),
        (ErrorLoggerFunctions.ErrorReport, 1), (ErrorLoggerFunctions.ErrorReport, 2),
        (ErrorLoggerFunctions.InfoReport, 1), (ErrorLoggerFunctions.InfoReport, 2),
        (ErrorLoggerFunctions.WarningReport, 1), (ErrorLoggerFunctions.WarningReport, 2), (ErrorLoggerFunctions.ErrorInfo, 1),
        (ErrorLoggerFunctions.InfoMsg, 1), (ErrorLoggerFunctions.InfoMsg, 2), (ErrorLoggerFunctions.WarningMsg, 1), (ErrorLoggerFunctions.WarningMsg, 2),
        (ErrorLoggerFunctions.Logfile, 1), (ErrorLoggerFunctions.Tty, 1), (ErrorLoggerFunctions.AddReportHandler, 1), (ErrorLoggerFunctions.AddReportHandler, 2),
        (ErrorLoggerFunctions.DeleteReportHandler, 1), (ErrorLoggerFunctions.WhichReportHandlers, 0),
        (ErrorLoggerFunctions.AddingHandler, 1), (ErrorLoggerFunctions.RemovingHandler, 1), (ErrorLoggerFunctions.Log, 2),
        (ErrorLoggerFunctions.GetFormatDepth, 0), (ErrorLoggerFunctions.LimitTerm, 1), (ErrorLoggerFunctions.WarningMap, 0)
    });

    public static void Register(ModuleRegistry registry)
        => registry.InstallPreloaded(target =>
        {
            foreach (var export in Exports.Where(export => export.Name != ErrorLoggerFunctions.WarningMap))
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

    private static Atom A(string value) => Term.A(value);

    private static MapTerm Map(params (string Key, Term Value)[] entries) => OtpLoggerTerms.Map(entries);

    private static bool Is(Term value, string name) => value is Atom atom && atom.Name == name;

    private static bool Field(Term value, string key, out Term result)
    {
        if (value is MapTerm map && map.TryGet(A(key), out var found))
        {
            result = found!;

            return true;
        }
        result = Nil.Value;

        return false;
    }

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

    private static async ValueTask<Term> CallAt(
        ProcessContext context,
        string owner,
        int arity,
        int line,
        string module,
        string function,
        params Term[] arguments
    )
    {
        using var frame = ErrorLoggerSource.Frame(
            context,
            owner,
            arity,
            line
        );

        return await Call(
            context,
            module,
            function,
            arguments
        );
    }

    private static ValueTask<Term> Self(ProcessContext context, string function, params Term[] arguments)
        => Invoke(context, function, arguments);

    public static async ValueTask<Term> Invoke(ProcessContext context, string function, IReadOnlyList<Term> arguments)
    {
        using var tail = context.EnterSourceTailCall(Name, function);
        try
        {
            return await Dispatch(context, function, arguments);
        }
        catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
            && !error.AutomaticStackTraceCaptured && !error.ExplicitStackTrace
            && Is(error.Reason, ErlangErrorReasons.FunctionClause))
        {
            int line = function switch
            {
                ErrorLoggerFunctions.AddingHandler => ErrorLoggerSourceLines.AddingHandler,
                ErrorLoggerFunctions.RemovingHandler => ErrorLoggerSourceLines.RemovingHandler,
                ErrorLoggerFunctions.Log => ErrorLoggerSourceLines.Log,
                ErrorLoggerFunctions.AddReportHandler when arguments.Count == 1 => ErrorLoggerSourceLines.AddHandlerDefault,
                ErrorLoggerFunctions.AddReportHandler => ErrorLoggerSourceLines.AddHandler,
                ErrorLoggerFunctions.DeleteReportHandler => ErrorLoggerSourceLines.DeleteHandler,
                ErrorLoggerFunctions.Logfile => ErrorLoggerSourceLines.Logfile,
                ErrorLoggerFunctions.Tty => ErrorLoggerSourceLines.Tty,
                _ => 0
            };
            if (line == 0)
            {
                throw;
            }
            throw ErrorLoggerSource.Error(
                context,
                function,
                arguments,
                ErlangErrorReasons.FunctionClause,
                line
            );
        }
    }

    private static async ValueTask<Term> Dispatch(ProcessContext context, string function, IReadOnlyList<Term> arguments)
    {
        switch (function, arguments.Count)
        {
            case (ErrorLoggerFunctions.Start, 0):
                return await Start(context);
            case (ErrorLoggerFunctions.StartLink, 0):
                return await Call(
                    context,
                    ErrorLoggerDependencyModules.GenEvent,
                    ErrorLoggerFunctions.StartLink,
                    Term.Tuple(A(ErrorLoggerProtocolNames.Local), A(Name)),
                    Term.List(Term.Tuple(
                        A(ErrorLoggerProtocolNames.SpawnOpt),
                        Term.List(Term.Tuple(A(ErrorLoggerProtocolNames.MessageQueueData), A(ErrorLoggerProtocolNames.OffHeap)))
                    ))
                );
            case (ErrorLoggerFunctions.Stop, 0):
                if (await Where(context) is not Atom { Name: ErrorLoggerProtocolNames.Undefined })
                {
                    await CallAt(
                        context,
                        ErrorLoggerFunctions.Stop,
                        0,
                        ErrorLoggerSourceLines.StopManager,
                        ErrorLoggerDependencyModules.GenEvent,
                        ErrorLoggerFunctions.Stop,
                        A(Name),
                        Term.Tuple(A(ErrorLoggerProtocolNames.Shutdown), A(ErrorLoggerProtocolNames.Stopped)),
                        A(ErrorLoggerProtocolNames.Infinity)
                    );
                    await CallAt(
                        context,
                        ErrorLoggerFunctions.Stop,
                        0,
                        ErrorLoggerSourceLines.DeleteChild,
                        ErrorLoggerDependencyModules.Supervisor,
                        ErrorLoggerFunctions.DeleteChild,
                        A(ErrorLoggerProtocolNames.LoggerSup),
                        A(Name)
                    );
                }

                return A(ErrorLoggerProtocolNames.Ok);
            case (ErrorLoggerFunctions.AddingHandler, 1) when Field(arguments[0], ErrorLoggerMetadataFields.Id, out var id) && Is(id, Name):
                Term result;
                using (ErrorLoggerSource.Frame(
                    context,
                    ErrorLoggerFunctions.AddingHandler,
                    1,
                    ErrorLoggerSourceLines.AddingHandlerStart
                ))
                {
                    result = await Start(context);
                }

                return Is(result, ErrorLoggerProtocolNames.Ok) ? Term.Tuple(result, arguments[0]) : result;
            case (ErrorLoggerFunctions.RemovingHandler, 1) when Field(arguments[0], ErrorLoggerMetadataFields.Id, out var id) && Is(id, Name):
                await Self(context, ErrorLoggerFunctions.Stop);

                return A(ErrorLoggerProtocolNames.Ok);
            case (ErrorLoggerFunctions.Log, 2) when Field(arguments[0], ErrorLoggerMetadataFields.Level, out var level)
                && Field(arguments[0], ErrorLoggerMetadataFields.Msg, out var message) && Field(arguments[0], ErrorLoggerMetadataFields.Meta, out var metadata):
                return await Log(
                    context,
                    level,
                    message,
                    metadata
                );
            case (ErrorLoggerFunctions.Format, 2):
                return await Message(
                    context,
                    ErrorLoggerFunctions.ErrorMsg,
                    arguments[0],
                    arguments[1]
                );
            case (ErrorLoggerFunctions.ErrorMsg or ErrorLoggerFunctions.WarningMsg or ErrorLoggerFunctions.InfoMsg, 1):
                return await Message(
                    context,
                    function,
                    arguments[0],
                    Nil.Value
                );
            case (ErrorLoggerFunctions.ErrorMsg or ErrorLoggerFunctions.WarningMsg or ErrorLoggerFunctions.InfoMsg, 2):
                return await Message(
                    context,
                    function,
                    arguments[0],
                    arguments[1]
                );
            case (ErrorLoggerFunctions.ErrorReport or ErrorLoggerFunctions.WarningReport or ErrorLoggerFunctions.InfoReport, 1):
                return await Report(
                    context,
                    function,
                    A(function == ErrorLoggerFunctions.ErrorReport ? ErrorLoggerProtocolNames.StdError : function == ErrorLoggerFunctions.WarningReport ? ErrorLoggerProtocolNames.StdWarning : ErrorLoggerProtocolNames.StdInfo),
                    arguments[0]
                );
            case (ErrorLoggerFunctions.ErrorReport or ErrorLoggerFunctions.WarningReport or ErrorLoggerFunctions.InfoReport, 2):
                return await Report(
                    context,
                    function,
                    arguments[0],
                    arguments[1]
                );
            case (ErrorLoggerFunctions.ErrorInfo, 1):
                return await ErrorInfo(context, arguments[0]);
            case (ErrorLoggerFunctions.AddReportHandler, 1) when arguments[0] is Atom:
                return await Self(
                    context,
                    function,
                    arguments[0],
                    Nil.Value
                );
            case (ErrorLoggerFunctions.AddReportHandler, 2) when arguments[0] is Atom:
                await CallAt(
                    context,
                    ErrorLoggerFunctions.AddReportHandler,
                    2,
                    ErrorLoggerSourceLines.AddLoggerHandler,
                    ErrorLoggerDependencyModules.Logger,
                    ErrorLoggerFunctions.AddHandler,
                    A(Name),
                    A(Name),
                    Map(
                        (ErrorLoggerMetadataFields.Level, A(ErrorLoggerProtocolNames.Info)),
                        (ErrorLoggerMetadataFields.FilterDefault, A(ErrorLoggerProtocolNames.Log))
                    )
                );

                return await Call(
                    context,
                    ErrorLoggerDependencyModules.GenEvent,
                    ErrorLoggerFunctions.AddHandler,
                    A(Name),
                    arguments[0],
                    arguments[1]
                );
            case (ErrorLoggerFunctions.DeleteReportHandler, 1) when arguments[0] is Atom:
                if (await Where(context) is Pid)
                {
                    Term deleted = await CallAt(
                        context,
                        ErrorLoggerFunctions.DeleteReportHandler,
                        1,
                        ErrorLoggerSourceLines.DeleteEventHandler,
                        ErrorLoggerDependencyModules.GenEvent,
                        ErrorLoggerFunctions.DeleteHandler,
                        A(Name),
                        arguments[0],
                        Nil.Value
                    );
                    Term remaining;
                    using (ErrorLoggerSource.Frame(
                        context,
                        ErrorLoggerFunctions.DeleteReportHandler,
                        1,
                        ErrorLoggerSourceLines.RemainingHandlers
                    ))
                    {
                        remaining = await Handlers(context);
                    }
                    if (remaining is Nil)
                    {
                        await CallAt(
                            context,
                            ErrorLoggerFunctions.DeleteReportHandler,
                            1,
                            ErrorLoggerSourceLines.RemoveLoggerHandler,
                            ErrorLoggerDependencyModules.Logger,
                            ErrorLoggerFunctions.RemoveHandler,
                            A(Name)
                        );
                    }

                    return deleted;
                }

                return A(ErrorLoggerProtocolNames.Ok);
            case (ErrorLoggerFunctions.WhichReportHandlers, 0):
                return await Handlers(context);
            case (ErrorLoggerFunctions.Logfile, 1):
                return await Logfile(context, arguments[0]);
            case (ErrorLoggerFunctions.Tty, 1) when arguments[0] is Atom { Name: ErrorLoggerProtocolNames.True or ErrorLoggerProtocolNames.False }:
                return await Tty(context, Is(arguments[0], ErrorLoggerProtocolNames.True));
            case (ErrorLoggerFunctions.GetFormatDepth, 0):
                Term configured = await CallAt(
                    context,
                    ErrorLoggerFunctions.GetFormatDepth,
                    0,
                    ErrorLoggerSourceLines.FormatDepthCase,
                    ErrorLoggerDependencyModules.Application,
                    ErrorLoggerFunctions.GetEnv,
                    A(ErrorLoggerProtocolNames.Kernel),
                    A(ErrorLoggerProtocolNames.ErrorLoggerFormatDepth),
                    A(ErrorLoggerProtocolNames.Unlimited)
                );
                if (configured is not Integer && !Is(configured, ErrorLoggerProtocolNames.Unlimited))
                {
                    throw ErrorLoggerSource.Capture(
                        context,
                        Term.Tuple(A(ErrorLoggerProtocolNames.CaseClause), configured),
                        [Term.Tuple(
                            A(Name),
                            A(ErrorLoggerFunctions.GetFormatDepth),
                            Term.I(0),
                            ErrorLoggerSource.Location(ErrorLoggerSourceLines.FormatDepthCase)
                        )]
                    );
                }

                return ErrorLoggerTermLimiter.NormalizeDepth(configured);
            case (ErrorLoggerFunctions.LimitTerm, 1):
                Term depth;
                using (ErrorLoggerSource.Frame(
                    context,
                    ErrorLoggerFunctions.LimitTerm,
                    1,
                    ErrorLoggerSourceLines.LimitDepth
                ))
                {
                    depth = await Self(context, ErrorLoggerFunctions.GetFormatDepth);
                }

                return Is(depth, ErrorLoggerProtocolNames.Unlimited) ? arguments[0] : await Call(
                    context,
                    ErrorLoggerDependencyModules.IoLib,
                    ErrorLoggerFunctions.LimitTerm,
                    arguments[0],
                    depth
                );
            default:
                throw new ErlangException(ErlangErrorReasons.FunctionClause);
        }
    }

    private static ValueTask<Term> Where(ProcessContext context) => Call(
        context,
        ErrorLoggerDependencyModules.Erlang,
        ErrorLoggerFunctions.Whereis,
        A(Name)
    );

    private static async ValueTask<Term> Handlers(ProcessContext context)
        => await Where(context) is Pid ? await Call(
            context,
            ErrorLoggerDependencyModules.GenEvent,
            ErrorLoggerFunctions.WhichHandlers,
            A(Name)
        ) : Nil.Value;

    private static async ValueTask<Term> Start(ProcessContext context)
    {
        if (!Is(await Where(context), ErrorLoggerProtocolNames.Undefined))
        {
            return A(ErrorLoggerProtocolNames.Ok);
        }
        Term specification = Map(
            (ErrorLoggerChildFields.Id, A(Name)),
            (ErrorLoggerChildFields.Start, Term.Tuple(A(Name), A(ErrorLoggerFunctions.StartLink), Nil.Value)),
            (ErrorLoggerChildFields.Restart, A(ErrorLoggerProtocolNames.Transient)),
            (ErrorLoggerChildFields.Shutdown, Term.I(ErrorLoggerSupervision.ShutdownMilliseconds)),
            (ErrorLoggerChildFields.Type, A(ErrorLoggerProtocolNames.Worker)),
            (ErrorLoggerChildFields.Modules, A(ErrorLoggerProtocolNames.Dynamic))
        );
        Term result = await CallAt(
            context,
            ErrorLoggerFunctions.Start,
            0,
            ErrorLoggerSourceLines.StartChild,
            ErrorLoggerDependencyModules.Supervisor,
            ErrorLoggerFunctions.StartChild,
            A(ErrorLoggerProtocolNames.LoggerSup),
            specification
        );

        return result is TupleTerm { Items: [Atom { Name: ErrorLoggerProtocolNames.Ok }, var pid] }
            ? await Call(
                context,
                ErrorLoggerDependencyModules.LoggerHandlerWatcher,
                ErrorLoggerFunctions.RegisterHandler,
                A(Name),
                pid
            ) : result;
    }
}
