using System.Collections.Concurrent;

namespace Erlang;

internal static class QueueHigherTrace
{
    private static readonly ConcurrentDictionary<(string Module, int Line), Term> locations = new();
    private static readonly ConcurrentDictionary<QueueHigherFrame, object> continuations = new();

    internal static Term Location(string module, int line) => locations.GetOrAdd((module, line), key => Term.List(
        Term.Tuple(Term.A(QueueCoreSource.File), Term.String(key.Module == QueueModuleNames.Module
            ? QueueCoreSource.FileName : QueueHigherNames.ListsFile)),
        Term.Tuple(Term.A(QueueCoreSource.Line), Term.I(key.Line))
    ));

    internal static object Continuation(QueueHigherFrame frame) => continuations.GetOrAdd(frame, _ => new object());

    internal static QueueHigherFrame[] Recursive(string helper, int line, bool pending)
        => pending ? [new(QueueModuleNames.Module, helper, 2, line)] : [];

    internal static async ValueTask<Term> Invoke(
        ProcessContext context,
        FunctionTerm callback,
        IReadOnlyList<Term> arguments,
        string module,
        string helper,
        int arity,
        int line,
        IReadOnlyList<QueueHigherFrame> pending
    )
    {
        using var history = new QueueHigherScope(context, pending);
        var frame = new QueueHigherFrame(module, helper, arity, line);
        using var current = context.EnterGeneratedFunction(module, helper, arity, Location(module, line), Continuation(frame));
        try
        {
            return await callback.Invoke(context, arguments);
        }
        catch (ErlangException error)
        {
            throw context.CaptureSourceFailure(error);
        }
    }

    internal static ErlangException Clause(
        ProcessContext context,
        string module,
        string helper,
        IReadOnlyList<Term> arguments,
        int line,
        IReadOnlyList<QueueHigherFrame> pending
    )
    {
        using var history = new QueueHigherScope(context, pending);

        return RuntimeExceptionCapture.Call(context, new ErlangException(ErlangErrorReasons.FunctionClause),
            context.CaptureExecutionChain(), module, helper, arguments, false, Location(module, line));
    }

    internal static ErlangException Case(
        ProcessContext context,
        string module,
        string helper,
        int arity,
        Term value,
        int line,
        IReadOnlyList<QueueHigherFrame> pending
    )
    {
        using var history = new QueueHigherScope(context, pending);
        var frame = new QueueHigherFrame(module, helper, arity, line);
        using var current = context.EnterGeneratedFunction(module, helper, arity, Location(module, line), Continuation(frame));

        return context.CaptureSourceFailure(new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), value)));
    }
}
