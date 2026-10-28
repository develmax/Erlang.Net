namespace Erlang;

internal sealed class OpaquePatternInvocation507 : IDisposable
{
    private readonly OwnedTermInvocation506 owned;
    internal Term Value { get; }
    internal Dictionary<string, Term> Bindings { get; }
    internal Dictionary<string, Term> KeyScope { get; }

    internal OpaquePatternInvocation507(
        ProcessContext context,
        Term value,
        Dictionary<string, Term> bindings,
        Dictionary<string, Term>? keyScope
    )
    {
        KeyValuePair<string, Term>[] values = bindings.ToArray();
        KeyValuePair<string, Term>[] keys = (keyScope ?? bindings).ToArray();
        Term input = Term.Tuple(value, Term.Tuple(values.Select(pair => pair.Value).ToArray()),
            Term.Tuple(keys.Select(pair => pair.Value).ToArray()));
        bool guestCaller = RuntimeActorExecutionTurn.IsOwnedBy(context)
            && context.CurrentOwned506 is { IsDisposed: false, CanBorrow507: true }
                current && current.BindingKind507 is OwnedBindingKind507.CompilerGuest or OwnedBindingKind507.PreparedGuest;
        owned = context.EnterOwnedInvocation506(new[] { input }, hostBoundary: !guestCaller)!;
        owned.BindingKind507 = OwnedBindingKind507.Opaque;
        try
        {
            var portable = (TupleTerm)owned.Export(owned.Arguments[0]);
            Value = portable.Items[PatternBoundary507Names.Value];
            Bindings = Restore(values, ((TupleTerm)portable.Items[PatternBoundary507Names.Bindings]).Items);
            KeyScope = Restore(keys, ((TupleTerm)portable.Items[PatternBoundary507Names.KeyScope]).Items);
        }
        catch
        {
            owned.Dispose();
            throw;
        }
    }

    private static Dictionary<string, Term> Restore(KeyValuePair<string, Term>[] names, IReadOnlyList<Term> values)
        => names.Select((pair, index) => new KeyValuePair<string, Term>(pair.Key, values[index]))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

    internal bool Complete507(
        bool matched,
        Dictionary<string, Term> destination,
        Dictionary<string, Term>? keyDestination
    )
    {
        if (!matched)
            return false;
        KeyValuePair<string, Term>[] outputs = Bindings.ToArray();
        KeyValuePair<string, Term>[] keyOutputs = KeyScope.ToArray();
        // Import both successful mutable environments together, then retain a
        // caller root beyond later guard calls that reuse its result slot.
        owned.Frame.Set(
            owned.ResultSlot507,
            Term.Tuple(
            Term.Tuple(outputs.Select(pair => pair.Value).ToArray()),
            Term.Tuple(keyOutputs.Select(pair => pair.Value).ToArray()))
        );
        var result = (TupleTerm)owned.Preserve(owned.Frame.Read(owned.ResultSlot507));
        var replacement = Restore(outputs, ((TupleTerm)result.Items[0]).Items);
        var replacementKeys = Restore(keyOutputs, ((TupleTerm)result.Items[1]).Items);
        Replace507(destination, replacement);
        if (keyDestination is not null)
            Replace507(keyDestination, replacementKeys);

        return true;
    }

    private static void Replace507(Dictionary<string, Term> destination, Dictionary<string, Term> replacement)
    {
        destination.Clear();
        foreach (var pair in replacement)
            destination.Add(pair.Key, pair.Value);
    }

    internal ErlangException Transfer507(ErlangException error) => owned.Transfer(error);

    internal Exception TransferControl507(Exception control) => owned.TransferControl506(control);

    public void Dispose() => owned.Dispose();
}
