using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Erlang;

// Pre-release migration: all public snapshots detach after the dictionary gate
// is released. Explicit borrow roots protect values across that preparation.
public sealed partial class ProcessDictionary : IDictionary<Term, Term>, IReadOnlyDictionary<Term, Term>
{
    private readonly ProcessContext context;
    private readonly object gate = new();
    private Dictionary<Term, Term> data = new();
    private Dictionary<Term, (int Key, int Value)> slots = new();
    private ProcessTermFrame506? frame;
    private bool closed;

    internal ProcessDictionary(ProcessContext context)
    {
        this.context = context;
        if (context.VmStorageBackingEnabled)
            frame = context.TermHeap506.Enter();
    }

    internal void ActivateGenuineVmExecution683()
    {
        while (true)
        {
            context.Cancellation.ThrowIfCancellationRequested();
            KeyValuePair<Term, Term>[] snapshot;
            lock (gate)
            {
                CheckOpen();
                if (frame is not null)
                    return;
                snapshot = data.ToArray();
            }
            ProcessTermFrame506? selected = context.TermHeap506.Enter();
            try
            {
                var copied = new Dictionary<Term, Term>();
                var copiedSlots = new Dictionary<Term, (int Key, int Value)>();
                // Actual import, map planning and typed capture are outside D.
                foreach (var pair in snapshot)
                {
                    int keySlot = selected.Reserve(), valueSlot = selected.Reserve();
                    selected.SetPrepared(keySlot, pair.Key);
                    selected.SetPrepared(valueSlot, pair.Value);
                    Term key = selected.Read(keySlot);
                    copied.Add(key, selected.Read(valueSlot));
                    copiedSlots.Add(key, (keySlot, valueSlot));
                }
                lock (gate)
                {
                    CheckOpen();
                    if (frame is not null)
                        return;
                    if (data.Count != snapshot.Length || snapshot.Any(pair =>
                        !data.TryGetValue(pair.Key, out Term? current) || !ReferenceEquals(current, pair.Value)))
                        continue;
                    // Assignments only: every failed attempt preserves old data.
                    data = copied;
                    slots = copiedSlots;
                    frame = selected;
                    selected = null;

                    return;
                }
            }
            finally
            {
                selected?.Dispose();
            }
        }
    }

    public Term this[Term key]
    {
        get => TryGetValue(key, out Term? value) ? value : throw new KeyNotFoundException();
        set => Set(
            key,
            value,
            false,
            false
        );
    }

    public int Count
    {
        get
        {
            lock (gate)
            {
                CheckOpen();

                return data.Count;
            }
        }
    }
    public bool IsReadOnly => false;
    public ICollection<Term> Keys => Snapshot().Select(pair => pair.Key).ToArray();
    public ICollection<Term> Values => Snapshot().Select(pair => pair.Value).ToArray();
    IEnumerable<Term> IReadOnlyDictionary<Term, Term>.Keys => Keys;
    IEnumerable<Term> IReadOnlyDictionary<Term, Term>.Values => Values;

    private Term Detach(Term value)
    {
        using PreparedTermTransfer506? prepared = PrepareDetachedInput586(value, out Term portable);

        return portable;
    }

    private PreparedTermTransfer506? PrepareDetachedInput586(Term value, out Term portable)
    {
        ProcessTermBorrow506 borrow;
        lock (gate)
        {
            CheckOpen();
            // A concurrent public writer during activation still uses its
            // original facade; the migration snapshot will retry if changed.
            if (frame is null)
            {
                portable = value;

                return null;
            }
            borrow = context.TermHeap506.Borrow(frame, value);
        }
        PreparedTermTransfer506 prepared = context.TermHeap506.PrepareBorrowAtomics586(borrow, false);
        portable = prepared.Portable;

        return prepared;
    }

    public bool ContainsKey(Term key)
    {
        Term portable = Detach(key);
        lock (gate)
        {
            CheckOpen();

            return data.ContainsKey(portable);
        }
    }

    public bool TryGetValue(Term key, [MaybeNullWhen(false)] out Term value)
    {
        Term portable = Detach(key);
        ProcessTermBorrow506? borrow = null;
        lock (gate)
        {
            CheckOpen();
            if (!data.TryGetValue(portable, out Term? found))
            {
                value = null!;

                return false;
            }
            if (frame is null)
            {
                value = found;

                return true;
            }
            borrow = context.TermHeap506.Borrow(frame, found);
        }
        using PreparedTermTransfer506 prepared = context.TermHeap506.PrepareBorrowAtomics586(borrow, true);
        value = prepared.Portable;

        return true;
    }

    private bool Set(
        Term key,
        Term value,
        bool addOnly,
        bool duplicateReturnsFalse
    )
    {
        using PreparedTermTransfer506? prepared = PrepareDetachedInput586(Term.Tuple(key, value), out Term portable);
        var pair = (TupleTerm)portable;
        Term portableKey = pair.Items[0], portableValue = pair.Items[1];
        while (true)
        {
            ProcessTermFrame506? target;
            lock (gate)
            {
                CheckOpen();
                if (addOnly && data.ContainsKey(portableKey))
                {
                    if (duplicateReturnsFalse) return false;
                    throw new ArgumentException(nameof(key));
                }
                target = frame;
            }
            // All callable construction/rebinding over actual copied children
            // completes before D; these are temporary REAL process roots.
            using PreparedHeapImport? keyImport = target?.PrepareImport(portableKey);
            using PreparedHeapImport? valueImport = target?.PrepareImport(portableValue);
            lock (gate)
            {
                CheckOpen();
                if (!ReferenceEquals(frame, target)) continue;
                bool exists = data.ContainsKey(portableKey);
                if (addOnly && exists)
                {
                    if (duplicateReturnsFalse) return false;
                    throw new ArgumentException(nameof(key));
                }
                if (target is null)
                {
                    data[portableKey] = portableValue;

                    return true;
                }
                if (slots.TryGetValue(portableKey, out var existing))
                {
                    target.SetLocalGraph507(existing.Value, valueImport!.Value);
                    data[portableKey] = target.Read(existing.Value);

                    return true;
                }
                int keySlot = target.Reserve(), valueSlot = target.Reserve();
                try
                {
                    target.SetLocalGraph507(keySlot, keyImport!.Value);
                    target.SetLocalGraph507(valueSlot, valueImport!.Value);
                    Term storedKey = target.Read(keySlot);
                    data.Add(storedKey, target.Read(valueSlot));
                    try
                    {
                        slots.Add(storedKey, (keySlot, valueSlot));
                    }
                    catch
                    {
                        data.Remove(storedKey);
                        throw;
                    }
                }
                catch
                {
                    target.Clear(keySlot);
                    target.Clear(valueSlot);
                    throw;
                }

                return true;
            }
        }
    }

    public void Add(Term key, Term value) => Set(
        key,
        value,
        true,
        false
    );

    public bool TryAdd(Term key, Term value) => Set(
        key,
        value,
        true,
        true
    );

    public bool Remove(Term key)
    {
        Term portable = Detach(key);
        lock (gate)
        {
            CheckOpen();

            return RemoveUnderGate(portable);
        }
    }

    private bool RemoveUnderGate(Term key)
    {
        if (!data.Remove(key))
            return false;
        if (slots.Remove(key, out var removed))
        {
            frame!.Clear(removed.Key);
            frame.Clear(removed.Value);
            reusableDictionarySlots507.Push(removed);
        }

        return true;
    }

    public void Clear()
    {
        lock (gate)
        {
            CheckOpen();
            data.Clear();
            slots.Clear();
            reusableDictionarySlots507.Clear();
            if (frame is not null)
            {
                frame.Dispose();
                frame = context.TermHeap506.Enter();
            }
        }
    }

    public void Add(KeyValuePair<Term, Term> item) => Add(item.Key, item.Value);

    public bool Contains(KeyValuePair<Term, Term> item) => TryGetValue(item.Key, out Term? value) && value.Equals(item.Value);

    public bool Remove(KeyValuePair<Term, Term> item)
    {
        var pair = (TupleTerm)Detach(Term.Tuple(item.Key, item.Value));
        lock (gate)
        {
            CheckOpen();

            return data.TryGetValue(pair.Items[0], out Term? found) && found.Equals(pair.Items[1])
                && RemoveUnderGate(pair.Items[0]);
        }
    }

    public void CopyTo(KeyValuePair<Term, Term>[] array, int arrayIndex) => Snapshot().CopyTo(array, arrayIndex);

    public IEnumerator<KeyValuePair<Term, Term>> GetEnumerator() => ((IEnumerable<KeyValuePair<Term, Term>>)Snapshot()).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private KeyValuePair<Term, Term>[] Snapshot()
    {
        ProcessTermBorrow506? borrow;
        lock (gate)
        {
            CheckOpen();
            if (frame is null)
                return data.ToArray();
            // Only capture/register roots here. No external discovery, recipe
            // factory or detached-copy callback runs under the dictionary gate.
            borrow = context.TermHeap506.Borrow(frame, Term.Tuple(data.SelectMany(pair => new[] { pair.Key, pair.Value }).ToArray()));
        }
        using PreparedTermTransfer506 prepared = context.TermHeap506.PrepareBorrowAtomics586(borrow, true);
        var copied = (TupleTerm)prepared.Portable;

        return Enumerable.Range(0, copied.Items.Count / 2).Select(index =>
            new KeyValuePair<Term, Term>(copied.Items[index * 2], copied.Items[index * 2 + 1])).ToArray();
    }

    private void CheckOpen() => ObjectDisposedException.ThrowIf(closed, this);

    internal void Close506()
    {
        lock (gate)
        {
            if (closed)
                return;
            closed = true;
            data.Clear();
            slots.Clear();
            reusableDictionarySlots507.Clear();
            frame?.Dispose();
            frame = null;
        }
    }
}
