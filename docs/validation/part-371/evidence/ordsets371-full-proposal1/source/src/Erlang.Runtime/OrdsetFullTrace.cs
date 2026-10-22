using System.Collections.Concurrent;

namespace Erlang;

internal static class OrdsetFullTrace
{
    private static readonly ConcurrentDictionary<OrdsetFullFrame, object> identities = new();

    internal static Term Location(string module, int line) => Term.List(
        Term.Tuple(Term.A(OrdsetCoreNames.File), Term.String(module == ListModuleNames.Module ? OrdsetFullNames.ListsFile : OrdsetCoreNames.FileName)),
        Term.Tuple(Term.A(OrdsetCoreNames.Line), Term.I(line))
    );

    internal static IDisposable Enter(ProcessContext context, OrdsetFullFrame frame)
        => context.EnterGeneratedFunction(
            frame.Module,
            frame.Function,
            frame.Arity,
            Location(frame.Module, frame.Line),
            identities.GetOrAdd(frame, _ => new object())
        );

    internal static ErlangException Clause(
        ProcessContext context,
        string module,
        string function,
        IReadOnlyList<Term> arguments,
        int line,
        IReadOnlyList<OrdsetFullFrame> pending
    )
    {
        using var scope = new OrdsetFullScope(context, pending);

        return RuntimeExceptionCapture.Call(
            context,
            new ErlangException(ErlangErrorReasons.FunctionClause),
            context.CaptureExecutionChain(),
            module,
            function,
            arguments,
            false,
            Location(module, line)
        );
    }

    internal static ErlangException Error(
        ProcessContext context,
        OrdsetFullFrame frame,
        Term reason,
        IReadOnlyList<OrdsetFullFrame> pending
    )
    {
        using var scope = new OrdsetFullScope(context, pending);
        using var current = Enter(context, frame);

        return context.CaptureSourceFailure(new ErlangException(reason));
    }

    internal static async ValueTask<Term> Invoke(
        ProcessContext context,
        FunctionTerm callback,
        IReadOnlyList<Term> arguments,
        OrdsetFullFrame frame,
        IReadOnlyList<OrdsetFullFrame> pending
    )
    {
        using var scope = new OrdsetFullScope(context, pending);
        using var current = Enter(context, frame);
        try
        {
            return await callback.Invoke(context, arguments);
        }
        catch (ErlangException error)
        {
            throw context.CaptureSourceFailure(error);
        }
    }

    internal static void Push(List<OrdsetFullFrame> pending, OrdsetFullFrame frame)
    {
        if (pending.Count != 0 && pending[^1] == frame)
            return;
        pending.Add(frame);
        if (pending.Count > BacktraceDepthParameters.MaximumDepth)
            pending.RemoveAt(0);
    }
}
