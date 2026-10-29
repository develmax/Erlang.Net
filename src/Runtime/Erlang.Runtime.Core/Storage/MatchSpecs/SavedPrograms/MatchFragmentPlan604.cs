using System.Numerics;

namespace Erlang;

// Temporary validated construction plan. It is never retained by the native owner.
internal sealed class MatchFragmentPlan604
{
    internal readonly record struct Node(
        Term Value,
        Term[] Children,
        int Offset,
        ulong Immediate,
        bool IsImmediate,
        ulong[]? Raw = null
    );

    internal List<Node> Nodes { get; } = new();
    internal Dictionary<Term, int> Indices { get; } = new(ReferenceEqualityComparer.Instance);
    internal int Words { get; private set; }
    internal int Root { get; private set; }

    internal static bool TryCreate(
        ProcessRuntime runtime,
        Term value,
        CancellationToken cancellation,
        out MatchFragmentPlan604? result
    )
    {
        result = null;
        var plan = new MatchFragmentPlan604();
        var active = new HashSet<Term>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<(Term Value, Term[]? Children)>();
        pending.Push((value, null));
        while (pending.TryPop(out var work))
        {
            cancellation.ThrowIfCancellationRequested();
            // Reject a foreign logical VM before reading any owned payload.
            if (ProcessTermHeap506.Storage(work.Value) is { } storage
                && (storage.Owner is not ProcessTermHeap506 heap || !ReferenceEquals(heap.Runtime524, runtime))) return false;
            if (plan.Indices.ContainsKey(work.Value)) continue;
            if (work.Children is null)
            {
                if (plan.Nodes.Count + active.Count >= MatchFragmentNames604.MaximumNodes || !active.Add(work.Value))
                    return false;

                Term[] children;
                bool immediate = false;
                ulong word = 0;
                ulong[]? raw = null;
                int words;
                switch (work.Value)
                {
                    case Nil:
                        children = [];
                        immediate = true;
                        word = MatchFragmentNames604.Nil;
                        words = 0;
                        break;
                    case Atom atom:
                        children = [];
                        immediate = true;
                        word = checked((ulong)runtime.AtomNames.IndexOf(atom.Name) << MatchFragmentNames604.AtomShift) | MatchFragmentNames604.AtomTag;
                        words = 0;
                        break;
                    case Integer integer when integer.Value >= MatchFragmentNames604.MinimumSmall && integer.Value <= MatchFragmentNames604.MaximumSmall:
                        children = [];
                        immediate = true;
                        word = unchecked((ulong)(long)integer.Value << MatchFragmentNames604.SmallShift) | MatchFragmentNames604.SmallTag;
                        words = 0;
                        break;
                    case FloatTerm floating:
                        {
                            double number = floating.Value;
                            if (!double.IsFinite(number)) return false;
                            raw = [(ulong)BoxedFloat556Names.Header, unchecked((ulong)BitConverter.DoubleToInt64Bits(number))];
                            children = [];
                            words = raw.Length;
                            break;
                        }
                    case Integer boxed:
                        {
                            BigInteger number = boxed.Value;
                            BigInteger magnitude = BigInteger.Abs(number);
                            long digits = checked((magnitude.GetBitLength() + BoxedInteger559Names.DigitBits - 1) / BoxedInteger559Names.DigitBits);
                            if (digits <= 0 || digits > BoxedInteger559Names.MaximumDigits) return false;
                            raw = new ulong[checked((int)digits + BoxedInteger559Names.HeaderWords)];
                            raw[0] = (ulong)BoxedInteger559Names.Header((int)digits, number.Sign < 0);
                            byte[] bytes = magnitude.ToByteArray(isUnsigned: true, isBigEndian: false);
                            for (int index = 0; index < bytes.Length; index++)
                                raw[BoxedInteger559Names.HeaderWords + index / BoxedInteger559Names.DigitBytes] |=
                                    (ulong)bytes[index] << ((index % BoxedInteger559Names.DigitBytes) * BoxedInteger559Names.ByteBits);
                            children = [];
                            words = raw.Length;
                            break;
                        }
                    case Cons cons:
                        children = [cons.Head, cons.Tail];
                        words = 2;
                        break;
                    case TupleTerm tuple:
                        // Pinned empty tuples use an existing literal instead of
                        // a saved fragment. Until that own-VM literal owner exists,
                        // reject empty tuples at every visited graph depth.
                        if (tuple.Items.Count == 0)
                            return false;
                        children = tuple.Items.ToArray();
                        words = checked(children.Length + 1);
                        break;
                    default:
                        return false;
                }

                if (immediate || raw is not null)
                {
                    active.Remove(work.Value);
                    plan.Indices.Add(work.Value, plan.Nodes.Count);
                    plan.Nodes.Add(new(
                        work.Value,
                        [],
                        0,
                        word,
                        immediate,
                        raw
                    ));
                    if (raw is not null) plan.Words = checked(plan.Words + raw.Length);
                }
                else
                {
                    // Reserve extent before pushing children. Offset is assigned at completion.
                    plan.Words = checked(plan.Words + words);
                    pending.Push((work.Value, children));
                    for (int index = children.Length - 1; index >= 0; index--)
                        pending.Push((children[index], null));
                }
            }
            else
            {
                active.Remove(work.Value);
                plan.Indices.Add(work.Value, plan.Nodes.Count);
                plan.Nodes.Add(new(
                    work.Value,
                    work.Children,
                    0,
                    0,
                    false
                ));
            }
        }

        int offset = 0;
        for (int index = 0; index < plan.Nodes.Count; index++)
        {
            Node node = plan.Nodes[index];
            if (node.IsImmediate) continue;
            plan.Nodes[index] = node with { Offset = offset };
            offset = checked(offset + (node.Raw?.Length ?? (node.Children.Length + (node.Value is TupleTerm ? 1 : 0))));
        }
        if (offset != plan.Words) throw new InvalidOperationException(MatchFragmentNames604.Invalid);
        plan.Root = plan.Indices[value];
        result = plan;

        return true;
    }
}
