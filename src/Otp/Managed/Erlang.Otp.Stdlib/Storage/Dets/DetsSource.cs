namespace Erlang;

internal static class DetsSource
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<(string Function, int Line), object> callbackContinuations = new();

    internal static IDisposable Scope(
        ProcessContext context,
        string function,
        int arity,
        int line
    )
        => context.EnterGeneratedFunction(
            DetsModule.Name,
            function,
            arity,
            Term.List(
                Term.Tuple(Term.A(ErlangSourceLocationNames.File), Term.String(DetsFileNames.Source)),
                Term.Tuple(Term.A(ErlangSourceLocationNames.Line), Term.I(line))
            ),
            callbackContinuations.GetOrAdd((function, line), static _ => new object())
        );

    internal static ErlangException BadArgument(
        ProcessContext context,
        string function,
        IReadOnlyList<Term> arguments,
        int? sourceLine = null
    )
    {
        int line = sourceLine ?? DetsSourceLines.InlineBadArgument;
        if (function == DetsFunctions.OpenFile && sourceLine is null)
        {
            line = arguments.Count == 1 ? DetsSourceLines.InvalidOpenName : DetsSourceLines.InvalidOpenOptions;
        }
        if (function == DetsFunctions.SafeFixtable && arguments[1] is not Atom { Name: DetsNames.True or DetsNames.False })
        {
            line = DetsSourceLines.InvalidFixBoolean;
        }
        if (function == DetsFunctions.RepairContinuation)
        {
            line = DetsSourceLines.InvalidRepairContinuation;
        }
        if (function is DetsFunctions.First or DetsFunctions.Next)
        {
            line = DetsSourceLines.ExitBadArgument;
        }
        if (function == DetsFunctions.DeleteAllObjects)
        {
            line = DetsSourceLines.DeleteAllBadArgument;
        }
        if (function == DetsFunctions.Slot && (arguments[1] is not Integer slot || slot.Value < 0))
        {
            line = DetsSourceLines.InvalidSlot;
        }
        if (function == DetsFunctions.Bchunk && sourceLine is null)
        {
            line = DetsSourceLines.InvalidBchunk;
        }
        if (function == DetsFunctions.FromEts && sourceLine is null)
        {
            line = DetsSourceLines.InvalidEtsSource;
        }
        if (function == DetsFunctions.ToEts)
        {
            line = DetsSourceLines.InvalidEtsTarget;
        }
        if (function == DetsFunctions.Table)
        {
            line = DetsSourceLines.InvalidTableOptions;
        }
        if (arguments.Count == 1 && function is DetsFunctions.Match or DetsFunctions.MatchObject or DetsFunctions.Select)
        {
            string what = function == DetsFunctions.Match ? DetsNames.Bindings : function == DetsFunctions.MatchObject ? DetsNames.Object : DetsNames.Select;
            if (arguments[0] is not TupleTerm { Items.Count: DetsContinuationFields.Count } continuation
                || continuation.Items[0] is not Atom { Name: DetsNames.Continuation }
                || continuation.Items[DetsContinuationFields.Kind] is not Atom kind || kind.Name != what)
            {
                line = function == DetsFunctions.Match ? DetsSourceLines.InvalidBindingsContinuation
                    : function == DetsFunctions.MatchObject ? DetsSourceLines.InvalidObjectContinuation : DetsSourceLines.InvalidSelectContinuation;
            }
        }
        var frames = new List<Term>
        {
            Term.Tuple(
                Term.A(DetsModule.Name),
                Term.A(function),
                Cons.From(arguments),
                Term.List(
                    Term.Tuple(Term.A(ErlangSourceLocationNames.File), Term.String(DetsFileNames.Source)),
                    Term.Tuple(Term.A(ErlangSourceLocationNames.Line), Term.I(line))
                )
            )
        };
        ErlangExecutionFrame? caller = context.CaptureExecutionChain();
        object? previous = null;
        while (caller is not null && frames.Count < Math.Max(1, context.Runtime.BacktraceDepth))
        {
            if (caller.ContinuationIdentity is { } identity && ReferenceEquals(previous, identity))
            {
                caller = caller.Parent;
                continue;
            }
            frames.Add(Term.Tuple(
                Term.A(caller.Module),
                Term.A(caller.Function),
                caller.HostHandlerArguments is { } args ? Cons.From(args) : Term.I(caller.Arity),
                caller.HostHandlerArguments is not null ? Nil.Value : caller.SourceLocation ?? Nil.Value
            ));
            previous = caller.ContinuationIdentity;
            caller = caller.Parent;
        }

        return new ErlangException(ErlangErrorReasons.BadArgument).WithCapturedStack(Cons.From(frames));
    }
}
