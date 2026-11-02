namespace Erlang;

// A narrow structural proof for deferred control metadata, not native lifetime
// authority. Existing Host/resource holders remain responsible for their backing.
// No graph lease, native retain, callback, import or ambient capability is added.
internal sealed class PortableControlTerm826
{
    internal Term Value { get; }

    private PortableControlTerm826(Term value) => Value = value;

    internal static PortableControlTerm826 Capture(Term source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var seen = new HashSet<Term>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<Term>();
        pending.Push(source);
        while (pending.TryPop(out Term? value))
        {
            if (!seen.Add(value))
                continue;
            // Metadata only, BEFORE any Items/Values/Environment/Children or
            // native numeric/binary/identifier getter could read heap cells.
            // Module literals/intrinsics also require a real holder, not this proof.
            if (ProcessTermHeap506.Storage(value) is not null)
                throw new InvalidOperationException(PortableControl826Names.NativeCells);
            switch (value)
            {
                case Nil or Atom or Integer or FloatTerm or Pid or PortTerm or ResourceReferenceTerm:
                    break;
                case ReferenceTerm reference when reference.GetType() == typeof(ReferenceTerm):
                    break;
                case Cons list:
                    pending.Push(list.Head);
                    pending.Push(list.Tail);
                    break;
                case TupleTerm tuple:
                    foreach (Term child in tuple.Items)
                        pending.Push(child);
                    break;
                case MapTerm map:
                    // Legacy native map views can hide provider/index state.
                    // Detached logical/preserved-entry DTOs have neither binding.
                    if (map.NativeEquality505 is not null || NativeMapMaterialization505.TryRead(map, null, out _))
                        throw new InvalidOperationException(PortableControl826Names.NativeMapView);
                    foreach (var pair in map.Entries)
                    {
                        pending.Push(pair.Key);
                        pending.Push(pair.Value);
                    }
                    foreach (var pair in map.ArithmeticKeyEntries)
                    {
                        pending.Push(pair.Key);
                        pending.Push(pair.Value);
                    }
                    if (map.FlatKeys426 is { } keys)
                        pending.Push(keys);
                    break;
                case HamtBitmapTerm429 bitmap:
                    foreach (Term child in bitmap.Children)
                        pending.Push(child);
                    break;
                case NativeRecordTerm record:
                    foreach (Term child in record.Values)
                        pending.Push(child);
                    break;
                case FunctionTerm function:
                    // Stored identity is metadata; never call a lazy identity provider.
                    if (function.StoredLocalIdentity is { } identity)
                        pending.Push(identity.Unique);
                    foreach (Term child in function.Environment)
                        pending.Push(child);
                    break;
                case BitString binary:
                    // Detached resource/ordinary Host views are legal. Do not read
                    // bytes, lengths, pointer identity or acquire another backing pin.
                    if (binary.ResourceSliceOrigin577 is { } origin)
                        pending.Push(origin);
                    break;
                default:
                    // Public Term extensions, including ReferenceTerm subclasses,
                    // cannot certify undisclosed children by merely reporting none.
                    throw new NotSupportedException(PortableControl826Names.UnknownTerm);
            }
        }

        return new(source);
    }
}
