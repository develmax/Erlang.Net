namespace Erlang;
// This is the actual lookup/traversal/slot owner, not a report over ObjectOrder offsets.
internal sealed class EtsPrimaryIndex420
{
    internal EtsCoreTableKind407 Kind { get; }
    internal int BucketCount { get; }
    internal int Mask { get; }
    internal EtsKeyGroup420[][] Buckets { get; }
    internal EtsKeyGroup420[] Ordered { get; }
    internal RuntimeEtsObjectSlot[] LiveRows { get; }
    internal LinkedList<RuntimeEtsObjectSlot> PhysicalRows { get; }
    internal Dictionary<long, RuntimeEtsObjectSlot> LiveObjects { get; }
    internal EtsKeyGroup420[] Groups { get; }

    private EtsPrimaryIndex420(
        EtsCoreTableKind407 kind,
        int buckets,
        int mask,
        EtsKeyGroup420[][] directory,
        EtsKeyGroup420[] ordered,
        EtsKeyGroup420[] groups,
        RuntimeEtsObjectSlot[] live,
        LinkedList<RuntimeEtsObjectSlot> physical,
        Dictionary<long, RuntimeEtsObjectSlot> objects
    )
    {
        Kind = kind;
        BucketCount = buckets;
        Mask = mask;
        Buckets = directory;
        Ordered = ordered;
        Groups = groups;
        LiveRows = live;
        PhysicalRows = physical;
        LiveObjects = objects;
    }

    internal int Slot(ulong hash)
    {
        int candidate = (int)(hash & (ulong)Mask);

        return candidate >= BucketCount ? candidate & (Mask >> 1) : candidate;
    }

    internal EtsKeyGroup420? Find(Term key, ulong hash, Func<EtsKeyGroup420, Term> readKey)
    {
        if (Kind == EtsCoreTableKind407.OrderedSet)
        {
            int first = 0, last = Ordered.Length - 1;
            while (first <= last)
            {
                int middle = first + ((last - first) >> 1);
                int compare = TermOrder.Compare(key, readKey(Ordered[middle]));
                if (compare == 0) return Ordered[middle];
                if (compare < 0) last = middle - 1; else first = middle + 1;
            }

            return null;
        }
        foreach (var group in Buckets[Slot(hash)]) if (group.Hash == hash && readKey(group).Equals(key)) return group;

        return null;
    }

    internal static EtsPrimaryIndex420 Build(
        EtsPrimaryIndex420? previous,
        IEnumerable<RuntimeEtsObjectSlot> source,
        RuntimeEtsCoreOptions407 options,
        bool fixedTable,
        Func<RuntimeEtsStoredObject, TupleTerm> read,
        Func<Term, ulong> hash,
        CancellationToken cancellation
    )
    {
        var builders = new List<EtsKeyGroupBuilder420>();
        foreach (var slot in source)
        {
            cancellation.ThrowIfCancellationRequested();
            Term key = EtsCorePlanner407.Key(read(slot.Payload), options.KeyPosition);
            ulong code = options.Kind == EtsCoreTableKind407.OrderedSet ? 0 : hash(key);
            EtsKeyGroupBuilder420? selected = null;
            foreach (var candidate in builders) if ((options.Kind == EtsCoreTableKind407.OrderedSet || candidate.Hash == code) && EtsCorePlanner407.SameKey(candidate.Key, key, options.Kind))
            {
                selected = candidate;
                break;
            }
            if (selected is null)
            {
                selected = new(key, code);
                builders.Add(selected);
            }
            selected.Rows.Add(slot);
        }
        var selectedGroups = new List<EtsKeyGroup420>();
        var consumed = new HashSet<EtsKeyGroupBuilder420>(ReferenceEqualityComparer.Instance);
        if (previous is not null)
            foreach (var old in previous.Groups)
            {
                cancellation.ThrowIfCancellationRequested();
                Term oldKey = EtsCorePlanner407.Key(read(old.KeyPayload), options.KeyPosition);
                EtsKeyGroupBuilder420? next = null;
                foreach (var candidate in builders) if (EtsCorePlanner407.SameKey(oldKey, candidate.Key, options.Kind))
                {
                    next = candidate;
                    break;
                }
                if (next is not null)
                {
                    selectedGroups.Add(new(next.Hash, next.Rows.ToArray(), null));
                    consumed.Add(next);
                }
                else if (fixedTable && options.Kind != EtsCoreTableKind407.OrderedSet)
                {
                    var actual = old.Tombstone ?? old.Rows.FirstOrDefault();
                    if (actual is not null) selectedGroups.Add(new(old.Hash, Array.Empty<RuntimeEtsObjectSlot>(), actual));
                }
            }
        foreach (var created in builders) if (!consumed.Contains(created)) selectedGroups.Insert(0, new(created.Hash, created.Rows.ToArray(), null));
        int rows = builders.Sum(group => group.Rows.Count);
        int count = previous?.BucketCount ?? EtsIndexNames420.InitialBuckets;
        if (!fixedTable)
        {
            while (count < rows) count = checked(count + 1);
            while (count > EtsIndexNames420.InitialBuckets && rows < count / 2) count--;
        }
        int mask = EtsIndexNames420.InitialBuckets - 1;
        while (mask < count - 1) mask = checked((mask << 1) | 1);
        var buckets = Enumerable.Range(0, count).Select(_ => new List<EtsKeyGroup420>()).ToArray();
        var physical = new LinkedList<RuntimeEtsObjectSlot>();
        var objects = new Dictionary<long, RuntimeEtsObjectSlot>();
        var all = new List<EtsKeyGroup420>();
        foreach (var group in selectedGroups)
        {
            cancellation.ThrowIfCancellationRequested();
            var live = new RuntimeEtsObjectSlot[group.Rows.Length];
            for (int i = 0; i < live.Length; i++)
            {
                var old = group.Rows[i];
                var slot = new RuntimeEtsObjectSlot(old.Id, old.Payload);
                slot.Link = physical.AddLast(slot);
                objects.Add(slot.Id, slot);
                live[i] = slot;
            }
            RuntimeEtsObjectSlot? tombstone = null;
            if (group.Tombstone is { } dead)
            {
                tombstone = new(dead.Id, dead.Payload);
                tombstone.Link = physical.AddLast(tombstone);
            }
            var owned = new EtsKeyGroup420(group.Hash, live, tombstone);
            all.Add(owned);
            if (options.Kind != EtsCoreTableKind407.OrderedSet)
            {
                int bucket = (int)(owned.Hash & (ulong)mask);
                if (bucket >= count) bucket &= mask >> 1;
                buckets[bucket].Add(owned);
            }
        }
        EtsKeyGroup420[] ordered = options.Kind == EtsCoreTableKind407.OrderedSet ? all.OrderBy(
            group => EtsCorePlanner407.Key(read(group.KeyPayload), options.KeyPosition),
            Comparer<Term>.Create((left, right) => TermOrder.Compare(left, right))
        ).ToArray() : Array.Empty<EtsKeyGroup420>();
        EtsKeyGroup420[][] directory = buckets.Select(bucket => bucket.ToArray()).ToArray();
        EtsKeyGroup420[] groups = options.Kind == EtsCoreTableKind407.OrderedSet ? ordered : directory.SelectMany(bucket => bucket).ToArray();
        cancellation.ThrowIfCancellationRequested();

        return new(
            options.Kind,
            count,
            mask,
            directory,
            ordered,
            groups,
            groups.SelectMany(group => group.Rows).ToArray(),
            physical,
            objects
        );
    }
}
