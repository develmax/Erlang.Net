// Derived from Erlang/OTP 29.1.1 dets.erl and dets_v9.erl (Apache-2.0).
// Copyright Ericsson AB 1996-2026. Upstream notices are retained in the Erlang sources.
namespace Erlang;

/// <summary>Server-owned object semantics. Synchronization and persistence belong to the server.</summary>
internal sealed class DetsObjectStore
{
    private readonly Dictionary<Term, List<Term>> objects = new();

    internal DetsObjectStore(DetsFileType type, int keyPosition)
    {
        if (type is not DetsFileType.Set and not DetsFileType.Bag and not DetsFileType.DuplicateBag || keyPosition <= 0)
        {
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
        Type = type;
        KeyPosition = keyPosition;
    }

    internal DetsFileType Type { get; }
    internal int KeyPosition { get; }
    internal long Count { get; private set; }
    internal int KeyCount => objects.Count;
    internal IEnumerable<Term> Keys => objects.Keys;

    internal Term Key(Term value)
    {
        if (value is not TupleTerm tuple || tuple.Items.Count < KeyPosition)
        {
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }

        return tuple.Items[KeyPosition - 1];
    }

    internal IReadOnlyList<Term> Lookup(Term key)
        => objects.TryGetValue(key, out List<Term>? values) ? Array.AsReadOnly(values.ToArray()) : Array.Empty<Term>();

    internal bool Member(Term key) => objects.ContainsKey(key);

    internal bool Insert(IReadOnlyList<Term> values, bool onlyNew)
    {
        // A malformed object or a single existing key must not admit a prefix.
        Term[] keys = values.Select(Key).ToArray();
        if (onlyNew && keys.Any(objects.ContainsKey))
        {
            return false;
        }
        for (int index = 0; index < values.Count; index++)
        {
            Term value = values[index];
            Term key = keys[index];
            if (!objects.TryGetValue(key, out List<Term>? group))
            {
                group = new List<Term>();
                objects.Add(key, group);
            }
            if (Type == DetsFileType.Set)
            {
                Count -= group.Count;
                group.Clear();
            }
            if (Type != DetsFileType.Bag || !group.Contains(value))
            {
                group.Add(value);
                Count++;
            }
        }

        return true;
    }

    internal void Delete(Term key)
    {
        if (objects.Remove(key, out List<Term>? removed))
        {
            Count -= removed.Count;
        }
    }

    internal void DeleteObject(Term value)
    {
        Term key = Key(value);
        if (!objects.TryGetValue(key, out List<Term>? group))
        {
            return;
        }
        Count -= group.RemoveAll(existing => existing.Equals(value));
        if (group.Count == 0)
        {
            objects.Remove(key);
        }
    }

    internal void Clear()
    {
        objects.Clear();
        Count = 0;
    }

    internal Term UpdateCounter(Term key, Term increment)
    {
        if (Type != DetsFileType.Set || !objects.TryGetValue(key, out List<Term>? group) || group.Count != 1)
        {
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
        int position = KeyPosition + 1;
        Term amount = increment;
        if (increment is TupleTerm { Items.Count: 2 } pair)
        {
            if (pair.Items[0] is not Integer index || index.Value < 1 || index.Value > int.MaxValue)
            {
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
            position = (int)index.Value;
            amount = pair.Items[1];
        }
        var tuple = (TupleTerm)group[0];
        if (position > tuple.Items.Count)
        {
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
        Term updated;
        try
        {
            updated = CoreModules.Arithmetic(ErlangOperators.Plus, tuple.Items[position - 1], amount);
        }
        catch (ErlangException)
        {
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
        Term[] items = tuple.Items.ToArray();
        items[position - 1] = updated;
        // OTP uses finsert here, including when Pos addresses the key itself.
        Insert(new Term[] { new TupleTerm(items) }, onlyNew: false);

        return updated;
    }

    internal IReadOnlyList<Term> Snapshot()
        => Array.AsReadOnly(objects.Values.SelectMany(values => values).ToArray());

    internal IReadOnlyList<Term> Select(ProcessContext context, MatchSpecProgram program)
    {
        var selected = new List<Term>();
        foreach (Term value in Snapshot())
        {
            context.Cancellation.ThrowIfCancellationRequested();
            MatchSpecResult result = program.Run(context, value, MatchSpecRunMode.Production);
            if (result.Matched)
            {
                selected.Add(result.Value);
            }
        }

        return selected.AsReadOnly();
    }

    internal long SelectDelete(ProcessContext context, MatchSpecProgram program)
    {
        long deleted = 0;
        foreach (Term value in Snapshot())
        {
            context.Cancellation.ThrowIfCancellationRequested();
            MatchSpecResult result = program.Run(context, value, MatchSpecRunMode.Production);
            if (result.Matched && result.Value is Atom { Name: DetsNames.True })
            {
                long before = Count;
                DeleteObject(value);
                deleted += before - Count;
            }
        }

        return deleted;
    }
}
