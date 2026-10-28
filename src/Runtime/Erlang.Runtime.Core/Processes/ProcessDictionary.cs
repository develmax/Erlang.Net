using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Erlang;

// Pre-release migration: all public snapshots detach after the dictionary gate
// is released. Explicit borrow roots protect values across that preparation.
public sealed class ProcessDictionary : IDictionary<Term, Term>, IReadOnlyDictionary<Term, Term>
{
    private readonly ProcessContext context;
    private readonly object gate = new();
    private readonly Dictionary<Term, Term> data = new();
    private readonly Dictionary<Term, (int Key, int Value)> slots = new();
    private ProcessTermFrame506? frame;
    private bool closed;

    internal ProcessDictionary(ProcessContext context)
    {
        this.context = context;
        if (context.Runtime.OwnedTermsPilot506)
            frame = context.TermHeap506.Enter();
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
        if (!context.Runtime.OwnedTermsPilot506)
            return value;
        ProcessTermBorrow506 borrow;
        lock (gate)
        {
            CheckOpen();
            borrow = context.TermHeap506.Borrow(frame!, value);
        }
        using PreparedTermTransfer506 prepared = context.TermHeap506.PrepareBorrow(borrow);

        return prepared.Portable;
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
        using PreparedTermTransfer506 prepared = context.TermHeap506.PrepareBorrow(borrow);
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
        var pair = (TupleTerm)Detach(Term.Tuple(key, value));
        Term portableKey = pair.Items[0], portableValue = pair.Items[1];
        lock (gate)
        {
            CheckOpen();
            bool exists = data.ContainsKey(portableKey);
            if (addOnly && exists)
            {
                if (duplicateReturnsFalse)
                    return false;
                throw new ArgumentException(nameof(key));
            }
            if (frame is null)
            {
                data[portableKey] = portableValue;

                return true;
            }
            if (slots.TryGetValue(portableKey, out var existing))
            {
                frame.SetPrepared(existing.Value, portableValue);
                data[portableKey] = frame.Read(existing.Value);

                return true;
            }
            int keySlot = frame.Reserve(), valueSlot = frame.Reserve();
            try
            {
                frame.SetPrepared(keySlot, portableKey);
                frame.SetPrepared(valueSlot, portableValue);
                Term storedKey = frame.Read(keySlot);
                data.Add(storedKey, frame.Read(valueSlot));
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
                frame.Clear(keySlot);
                frame.Clear(valueSlot);
                throw;
            }

            return true;
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
        using PreparedTermTransfer506 prepared = context.TermHeap506.PrepareBorrow(borrow);
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
            frame?.Dispose();
            frame = null;
        }
    }
}
