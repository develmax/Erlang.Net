namespace Erlang.Tests;

internal static class QueueBeamHigherSmokeContracts
{
    internal static bool Owns(string module, string function, int arity)
        => module == QueueModuleNames.Module && (function, arity) is
            (QueueModuleNames.Member, 2)
            or (QueueModuleNames.Reverse, 1)
            or (QueueModuleNames.Join, 2)
            or (QueueModuleNames.Split, 2)
            or (QueueModuleNames.Filter, 2)
            or (QueueModuleNames.FilterMap, 2)
            or (QueueModuleNames.Fold, 3)
            or (QueueModuleNames.Any, 2)
            or (QueueModuleNames.All, 2)
            or (QueueModuleNames.Delete, 2)
            or (QueueModuleNames.DeleteReverse, 2)
            or (QueueModuleNames.DeleteWith, 2)
            or (QueueModuleNames.DeleteWithReverse, 2);

    internal static async Task Run(
        string function,
        int arity,
        bool beamBackend,
        bool originalLists = false,
        bool startupSelection = false
    )
    {
        if (!Owns(QueueModuleNames.Module, function, arity))
            throw new InvalidOperationException(QueueHigherHostNames.Failure);
        await using var runtime = startupSelection
            ? await ProcessRuntime.CreateAsync(LibrarySelectionHostData.Both())
            : new ProcessRuntime();
        if (!startupSelection)
            QueueBeamFullSmokeBackend.Select(runtime, beamBackend, originalLists);
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        await owner.InvokeAsync(async context =>
        {
            Term queue = Term.Tuple(Term.List(Term.I(3), Term.I(2)), Term.List(Term.I(1)));
            Term empty = Term.Tuple(Nil.Value, Nil.Value);
            var even = new FunctionTerm(
                1,
                (_, a) => ValueTask.FromResult<Term>(
                Term.A(((Integer)a[0]).Value.IsEven ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False))
            );
            var sum = new FunctionTerm(2, (_, a) => ValueTask.FromResult<Term>(
                new Integer(((Integer)a[0]).Value + ((Integer)a[1]).Value)));

            async ValueTask<Term> Call(params Term[] a)
                => await runtime.Modules.Call(
                    context,
                    QueueModuleNames.Module,
                    function,
                    a
                );
            Term actual;
            Term expected;
            switch (function)
            {
                case QueueModuleNames.Member:
                    actual = await Call(Term.I(2), queue);
                    expected = Term.A(ErlangBooleanAtoms.True);
                    break;
                case QueueModuleNames.Reverse:
                    actual = await Call(queue);
                    expected = Term.Tuple(Term.List(Term.I(1)), Term.List(Term.I(3), Term.I(2)));
                    break;
                case QueueModuleNames.Join:
                    actual = await Call(queue, empty);
                    expected = queue;
                    break;
                case QueueModuleNames.Split:
                    actual = await Call(Term.I(0), queue);
                    expected = Term.Tuple(empty, queue);
                    break;
                case QueueModuleNames.Filter:
                case QueueModuleNames.FilterMap:
                    actual = await Call(even, queue);
                    expected = Term.Tuple(Nil.Value, Term.List(Term.I(2)));
                    break;
                case QueueModuleNames.Fold:
                    actual = await Call(sum, Term.I(10), queue);
                    expected = Term.I(16);
                    break;
                case QueueModuleNames.Any:
                    actual = await Call(even, queue);
                    expected = Term.A(ErlangBooleanAtoms.True);
                    break;
                case QueueModuleNames.All:
                    actual = await Call(even, queue);
                    expected = Term.A(ErlangBooleanAtoms.False);
                    break;
                case QueueModuleNames.Delete:
                case QueueModuleNames.DeleteReverse:
                    actual = await Call(Term.I(2), queue);
                    expected = Term.Tuple(Term.List(Term.I(3)), Term.List(Term.I(1)));
                    break;
                default:
                    actual = await Call(even, queue);
                    expected = Term.Tuple(Term.List(Term.I(3)), Term.List(Term.I(1)));
                    break;
            }
            if (!actual.Equals(expected))
                throw new InvalidOperationException(QueueHigherHostNames.Failure);

            return Nil.Value;
        });
    }
}
