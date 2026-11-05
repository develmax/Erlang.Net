using System.Collections.Concurrent;

namespace Erlang;

internal static class ListUniqueMergeTrace
{
    private static readonly ConcurrentDictionary<(ListUniqueMergeState State, int Line), object> identities = new();

    internal static IDisposable Enter(ProcessContext context, ListUniqueMergeContinuation continuation)
        => context.EnterGeneratedFunction(
            ListModuleNames.Module,
            ListUniqueMergeNames.Function(continuation.Caller),
            ListUniqueMergeNames.Arity(continuation.Caller),
            Location(continuation.Line),
            identities.GetOrAdd((continuation.Caller, continuation.Line), _ => new object())
        );

    internal static ErlangException Clause(
        ProcessContext context,
        ListUniqueMergeState state,
        IReadOnlyList<Term> arguments,
        IEnumerable<ListUniqueMergeContinuation> continuations
    ) => Clause(
        context,
        ListUniqueMergeNames.Function(state),
        arguments,
        ListUniqueMergeNames.Line(state),
        continuations
    );

    internal static ErlangException Clause(
        ProcessContext context,
        string function,
        IReadOnlyList<Term> arguments,
        int line,
        IEnumerable<ListUniqueMergeContinuation> continuations
    )
    {
        using var scope = new ListUniqueMergeScope(context, continuations);

        return RuntimeExceptionCapture.Call(
            context,
            new ErlangException(ErlangErrorReasons.FunctionClause),
            context.CaptureExecutionChain(),
            ListModuleNames.Module,
            function,
            arguments,
            false,
            Location(line)
        );
    }

    internal static async ValueTask<Term> Reverse(
        ProcessContext context,
        Term list,
        Term tail,
        IEnumerable<ListUniqueMergeContinuation> continuations,
        ListUniqueMergeState? caller = null,
        int line = 0
    )
    {
        using var scope = new ListUniqueMergeScope(context, continuations);

        // The registry selects the body and native marker under one gate.
        // A replacement after selection must not change this invocation's PC.
        return await context.Runtime.Modules.CallWithNativeCallerScope(
            context,
            ListModuleNames.Module,
            ListModuleNames.Reverse,
            [list, tail],
            caller is { } state ? current => current.EnterGeneratedFunction(
                ListModuleNames.Module,
                ListUniqueMergeNames.Function(state),
                ListUniqueMergeNames.Arity(state),
                Location(line),
                identities.GetOrAdd((state, line), _ => new object())
            ) : null
        );
    }

    private static Term Location(int line) => Term.List(
        Term.Tuple(Term.A(ListUniqueMergeNames.SourceFileKey), Term.String(ListUniqueMergeNames.File)),
        Term.Tuple(Term.A(ListUniqueMergeNames.SourceLineKey), Term.I(line))
    );
}
