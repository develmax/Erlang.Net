using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang;

public static class TermOrder
{
    private static int Rank(Term t) => t switch
    {
        Integer or FloatTerm => 0,
        Atom => 1,
        ReferenceTerm => 2,
        FunctionTerm => 3,
        PortTerm => 4,
        Pid => 5,
        TupleTerm => 6,
        NativeRecordTerm => 7,
        MapTerm or HamtBitmapTerm429 => 8,
        Nil => 9,
        Cons => 10,
        BitString => 11,
        _ => throw new NotSupportedException()
    };

    public static int Compare(Term a, Term b, bool exact = false)
    {
        NodeComparisonContext context = default;

        return CompareSnapshot(
            a,
            b,
            exact,
            ref context
        );
    }

    private static int CompareCurrent(
        Term a,
        Term b,
        bool exact,
        ref NodeComparisonContext context,
        Stack<IEnumerator<TermComparisonStep>> pending,
        TermComparisonPurpose purpose,
        Func<ITermMapComparisonLayout>? layoutFactory,
        ref ITermMapComparisonLayout? mapLayout
    )
    {
        int rank = Rank(a).CompareTo(Rank(b));
        if (rank != 0)
            return rank;
        if (ReferenceEquals(a, b))
            return 0;
        switch (a, b)
        {
            case (Integer x, Integer y):
                return x.Value.CompareTo(y.Value);
            case (FloatTerm x, FloatTerm y):
                {
                    int c = x.Value.CompareTo(y.Value);

                    return c != 0 || !exact ? c : BitConverter.DoubleToInt64Bits(x.Value).CompareTo(BitConverter.DoubleToInt64Bits(y.Value));
                }
            case (Integer x, FloatTerm y):
                return exact ? -1 : IntegerFloat(x.Value, y.Value);
            case (FloatTerm x, Integer y):
                return exact ? 1 : -IntegerFloat(y.Value, x.Value);
            case (Atom x, Atom y):
                return Codepoints(x.Name, y.Name);
            case (Nil, Nil):
                return 0;
            case (Pid x, Pid y):
                {
                    var leftNode = context.Read(x.LocalIdentity, x.WireIdentity);
                    var rightNode = context.Read(y.LocalIdentity, y.WireIdentity);

                    return PidIdentity(
                        leftNode.Node,
                        leftNode.Creation,
                        x.Id,
                        rightNode.Node,
                        rightNode.Creation,
                        y.Id
                    );
                }
            case (ReferenceTerm x, ReferenceTerm y):
                {
                    var leftNode = context.Read(x.LocalIdentity, x.WireIdentity);
                    var rightNode = context.Read(y.LocalIdentity, y.WireIdentity);
                    int node = Codepoints(leftNode.Node, rightNode.Node);
                    if (node != 0)
                        return node;
                    int creation = leftNode.Creation.CompareTo(rightNode.Creation);

                    return creation != 0 ? creation : x.FullId.CompareTo(y.FullId);
                }
            case (PortTerm x, PortTerm y):
                {
                    var leftNode = context.Read(x.LocalIdentity, x.WireIdentity);
                    var rightNode = context.Read(y.LocalIdentity, y.WireIdentity);

                    return PortIdentity(
                        leftNode.Node,
                        leftNode.Creation,
                        x.Id,
                        rightNode.Node,
                        rightNode.Creation,
                        y.Id
                    );
                }
            case (FunctionTerm x, FunctionTerm y):
                if (x.ExternalIdentity is { } left && y.ExternalIdentity is { } right)
                {
                    pending.Push(ExternalFunctionSteps(left, right).GetEnumerator());

                    return 0;
                }
                if (x.ExternalIdentity is not null || y.ExternalIdentity is not null)
                    return (x.ExternalIdentity is not null).CompareTo(y.ExternalIdentity is not null);
                if (x.LocalIdentity is { } localLeft && y.LocalIdentity is { } localRight)
                {
                    pending.Push(LocalFunctionSteps(
                        x,
                        y,
                        localLeft,
                        localRight,
                        exact,
                        purpose
                    ).GetEnumerator());

                    return 0;
                }
                if (x.LocalIdentity is not null || y.LocalIdentity is not null)
                    return (x.LocalIdentity is not null).CompareTo(y.LocalIdentity is not null);

                return x.Identity.CompareTo(y.Identity);
            case (TupleTerm x, TupleTerm y):
                {
                    int count = x.Items.Count.CompareTo(y.Items.Count);
                    if (count != 0)
                        return count;
                    pending.Push(SequenceSteps(x.Items, y.Items, exact).GetEnumerator());

                    return 0;
                }
            case (NativeRecordTerm x, NativeRecordTerm y):
                pending.Push(RecordSteps(x, y, exact).GetEnumerator());

                return 0;
            case (HamtBitmapTerm429 x, HamtBitmapTerm429 y):
                {
                    int bitmap = x.Bitmap.CompareTo(y.Bitmap);
                    if (bitmap != 0) return bitmap;
                    pending.Push(SequenceSteps(x.Children, y.Children, exact).GetEnumerator());

                    return 0;
                }
            // Interior source nodes have no complete-map size/key array. Keep
            // their own protocol ordering distinct; never read fake map fields.
            case (HamtBitmapTerm429, MapTerm): return -1;
            case (MapTerm, HamtBitmapTerm429): return 1;
            case (MapTerm x, MapTerm y):
                {
                    int count = x.Entries.Count.CompareTo(y.Entries.Count);
                    if (count != 0)
                        return count;
                    if (purpose == TermComparisonPurpose.NumericEquality
                        && x.Entries.Count > TermMapOrderParameters.FlatMapMaximumEntries
                        && layoutFactory is not null)
                    {
                        mapLayout ??= layoutFactory();
                        pending.Push(HashMapEqualitySteps(
                            x,
                            y,
                            exact,
                            mapLayout
                        ).GetEnumerator());

                        return 0;
                    }
                    pending.Push(((purpose == TermComparisonPurpose.Ordering && !exact
                        || purpose == TermComparisonPurpose.NumericEquality)
                        && count == 0 && x.Entries.Count > 1
                        && x.Entries.Count <= TermMapOrderParameters.FlatMapMaximumEntries
                        ? TermMapOrdering.Steps(x, y, exact)
                        : MapSteps(x, y, exact)).GetEnumerator());

                    return 0;
                }
            case (Cons, Cons):
                pending.Push(ListSteps(a, b, exact).GetEnumerator());

                return 0;
            case (BitString x, BitString y):
                for (int i = 0; i < Math.Min(x.BitLength, y.BitLength); i++)
                {
                    int c = x.Bit(i).CompareTo(y.Bit(i));
                    if (c != 0)
                        return c;
                }

                return x.BitLength.CompareTo(y.BitLength);
            default:
                throw new NotSupportedException();
        }
    }

    private static int PidIdentity(
        string an,
        uint ac,
        ulong ai,
        string bn,
        uint bc,
        ulong bi
    )
    {
        int c = ai.CompareTo(bi);
        if (c != 0)
            return c;
        c = Codepoints(an, bn);

        return c != 0 ? c : ac.CompareTo(bc);
    }

    private static int PortIdentity(
        string an,
        uint ac,
        ulong ai,
        string bn,
        uint bc,
        ulong bi
    )
    {
        int c = Codepoints(an, bn);
        if (c != 0)
            return c;
        c = ac.CompareTo(bc);

        return c != 0 ? c : ai.CompareTo(bi);
    }

    private static int Codepoints(string a, string b)
    {
        var x = a.EnumerateRunes().GetEnumerator();
        var y = b.EnumerateRunes().GetEnumerator();
        while (true)
        {
            bool ax = x.MoveNext(), by = y.MoveNext();
            if (!ax || !by)
                return ax.CompareTo(by);
            int c = x.Current.Value.CompareTo(y.Current.Value);
            if (c != 0)
                return c;
        }
    }

    internal static int CompareSnapshot(
        Term a,
        Term b,
        bool exact,
        ref NodeComparisonContext context
    ) => ComparePurposeSnapshot(
        a,
        b,
        exact,
        ref context,
        TermComparisonPurpose.Ordering
    );

    internal static int CompareCanonicalSnapshot(
        Term a,
        Term b,
        ref NodeComparisonContext context
    ) => ComparePurposeSnapshot(
        a,
        b,
        true,
        ref context,
        TermComparisonPurpose.CanonicalKeyStorage
    );

    internal static bool ExactEquals(Term a, Term b)
    {
        NodeComparisonContext context = default;

        return ComparePurposeSnapshot(
            a,
            b,
            true,
            ref context,
            TermComparisonPurpose.ExactEquality
        ) == 0;
    }

    internal static bool NumericEquals(Term a, Term b)
    {
        NodeComparisonContext context = default;

        return ComparePurposeSnapshot(
            a,
            b,
            false,
            ref context,
            TermComparisonPurpose.NumericEquality
        ) == 0;
    }

    private static int ComparePurposeSnapshot(
        Term a,
        Term b,
        bool exact,
        ref NodeComparisonContext context,
        TermComparisonPurpose purpose
    )
    {
        ITermMapComparisonLayout? mapLayout = null;
        Func<ITermMapComparisonLayout>? layoutFactory = purpose == TermComparisonPurpose.NumericEquality
            ? TermMapComparisonEnvironment.Factory : null;
        var scopes = new Stack<TermComparisonScope>();
        scopes.Push(new TermComparisonScope(
            a,
            b,
            exact,
            purpose
        ));
        try
        {
            while (true)
            {
                var scope = scopes.Peek();
                var step = scope.Current;
                if (step.Capture is { } capture)
                {
                    scopes.Push(new TermComparisonScope(
                        step.Left!,
                        step.Right!,
                        step.Exact,
                        step.Purpose ?? scope.Purpose,
                        capture
                    ));
                    continue;
                }
                int comparison = step.Left is null ? step.Result : CompareCurrent(
                    step.Left,
                    step.Right!,
                    step.Exact,
                    ref context,
                    scope.Pending,
                    scope.Purpose,
                    layoutFactory,
                    ref mapLayout
                );
                if (comparison == 0 && scope.MoveNext())
                    continue;
                while (true)
                {
                    var finished = scopes.Pop();
                    finished.Dispose();
                    if (scopes.Count == 0)
                        return comparison;
                    finished.Capture!.Value = comparison;
                    scope = scopes.Peek();
                    if (scope.MoveNext())
                        break;
                    comparison = 0;
                }
            }
        }
        finally
        {
            try
            {
                while (scopes.TryPop(out var scope))
                    scope.Dispose();
            }
            finally
            {
                mapLayout?.Dispose();
            }
        }
    }

    private static IEnumerable<TermComparisonStep> HashMapEqualitySteps(
        MapTerm left,
        MapTerm right,
        bool exact,
        ITermMapComparisonLayout layout
    )
    {
        IReadOnlyList<KeyValuePair<Term, Term>> first = layout.ReadHashEntries(left);
        IReadOnlyList<KeyValuePair<Term, Term>> second = layout.ReadHashEntries(right);
        if (first.Count != left.Entries.Count || second.Count != right.Entries.Count)
            throw new InvalidOperationException(TermMapComparisonNames.InvalidLayout);
        for (int index = 0; index < first.Count; index++)
        {
            yield return TermComparisonStep.Pair(first[index].Key, second[index].Key, true);
            yield return TermComparisonStep.Pair(first[index].Value, second[index].Value, exact);
        }
    }

    private static IEnumerable<TermComparisonStep> SequenceSteps(
        IReadOnlyList<Term> a, IReadOnlyList<Term> b, bool exact)
    {
        for (int i = 0; i < a.Count; i++)
            yield return TermComparisonStep.Pair(a[i], b[i], exact);
    }

    private static IEnumerable<TermComparisonStep> ExternalFunctionSteps(
        ExternalFunctionIdentity a, ExternalFunctionIdentity b)
    {
        yield return TermComparisonStep.Pair(a.Module, b.Module, true);
        yield return TermComparisonStep.Pair(a.Function, b.Function, true);
        yield return TermComparisonStep.Scalar(a.Arity.CompareTo(b.Arity));
    }

    private static IEnumerable<TermComparisonStep> LocalFunctionSteps(
        FunctionTerm a,
        FunctionTerm b,
        LocalFunctionIdentity left,
        LocalFunctionIdentity right,
        bool exact,
        TermComparisonPurpose purpose
    )
    {
        yield return TermComparisonStep.Pair(left.Module, right.Module, true);
        yield return TermComparisonStep.Scalar(left.Index.CompareTo(right.Index));
        if (purpose is TermComparisonPurpose.ExactEquality or TermComparisonPurpose.CanonicalKeyStorage)
        {
            yield return TermComparisonStep.Pair(left.Unique, right.Unique, true);
            yield return TermComparisonStep.Scalar(a.Arity.CompareTo(b.Arity));
        }
        else
            yield return TermComparisonStep.Scalar(left.OldUnique.CompareTo(right.OldUnique));
        yield return TermComparisonStep.Scalar(a.Environment.Count.CompareTo(b.Environment.Count));
        for (int i = 0; i < a.Environment.Count; i++)
            yield return TermComparisonStep.Pair(a.Environment[i], b.Environment[i], exact);
    }

    private static IEnumerable<TermComparisonStep> RecordSteps(NativeRecordTerm a, NativeRecordTerm b, bool exact)
    {
        yield return TermComparisonStep.Pair(a.Module, b.Module, true);
        yield return TermComparisonStep.Pair(a.Name, b.Name, true);
        yield return TermComparisonStep.Scalar(a.IsExported.CompareTo(b.IsExported));
        yield return TermComparisonStep.Scalar(a.FieldNames.Count.CompareTo(b.FieldNames.Count));
        for (int i = 0; i < a.FieldNames.Count; i++)
            yield return TermComparisonStep.Pair(a.FieldNames[i], b.FieldNames[i], true);
        for (int i = 0; i < a.Values.Count; i++)
            yield return TermComparisonStep.Pair(a.Values[i], b.Values[i], exact);
    }

    private static IEnumerable<TermComparisonStep> MapSteps(MapTerm a, MapTerm b, bool exact)
    {
        for (int i = 0; i < a.Entries.Count; i++)
            yield return TermComparisonStep.Pair(a.Entries[i].Key, b.Entries[i].Key, true);
        for (int i = 0; i < a.Entries.Count; i++)
            yield return TermComparisonStep.Pair(a.Entries[i].Value, b.Entries[i].Value, exact);
    }

    private static IEnumerable<TermComparisonStep> ListSteps(Term a, Term b, bool exact)
    {
        while (a is Cons left && b is Cons right)
        {
            yield return TermComparisonStep.Pair(left.Head, right.Head, exact);
            a = left.Tail;
            b = right.Tail;
        }
        yield return TermComparisonStep.Pair(a, b, exact);
    }

    // Compare the exact binary rational represented by a double, without rounding a large integer.
    private static int IntegerFloat(BigInteger i, double f)
    {
        long bits = BitConverter.DoubleToInt64Bits(f);
        int exponent = (int)((bits >> 52) & 2047);
        long fraction = bits & 0xfffffffffffff;
        BigInteger numerator = exponent == 0 ? fraction : fraction | (1L << 52);
        int power = (exponent == 0 ? -1022 : exponent - 1023) - 52;
        if (bits < 0)
            numerator = -numerator;

        return power >= 0 ? i.CompareTo(numerator << power) : (i << -power).CompareTo(numerator);
    }

    public static int Hash(Term t)
    {
        var pending = new Stack<TermHashFrame>();
        pending.Push(new TermHashFrame(HashSteps(t).GetEnumerator()));
        try
        {
            while (true)
            {
                var frame = pending.Peek();
                if (frame.Steps.MoveNext())
                {
                    var step = frame.Steps.Current;
                    if (step.Value is not null)
                    {
                        frame.PendingKind = step.Kind;
                        pending.Push(new TermHashFrame(HashSteps(step.Value).GetEnumerator()));
                    }
                    else
                    {
                        frame.Consume(step.Kind, step.Hash);
                    }
                    continue;
                }
                int hash = frame.Accumulator.ToHashCode();
                pending.Pop().Steps.Dispose();
                if (pending.Count == 0)
                    return hash;
                var parent = pending.Peek();
                parent.Consume(parent.PendingKind, hash);
            }
        }
        finally
        {
            foreach (var frame in pending)
                frame.Steps.Dispose();
        }
    }

    private static IEnumerable<TermHashStep> HashSteps(Term t)
    {
        yield return TermHashStep.Scalar(Rank(t));
        switch (t)
        {
            case Integer x:
                yield return TermHashStep.Scalar(0);
                yield return TermHashStep.Scalar(x.Value.GetHashCode());
                break;
            case FloatTerm x:
                yield return TermHashStep.Scalar(1);
                yield return TermHashStep.Scalar(BitConverter.DoubleToInt64Bits(x.Value).GetHashCode());
                break;
            case Atom x:
                yield return TermHashStep.Scalar(StringComparer.Ordinal.GetHashCode(x.Name));
                break;
            case Pid x:
                yield return TermHashStep.Scalar(x.Id.GetHashCode());
                break;
            case ReferenceTerm x:
                yield return TermHashStep.Scalar(x.FullId.GetHashCode());
                break;
            case PortTerm x:
                yield return TermHashStep.Scalar(x.Id.GetHashCode());
                break;
            case FunctionTerm x:
                if (x.ExternalIdentity is { } identity)
                {
                    yield return TermHashStep.Child(identity.Module);
                    yield return TermHashStep.Child(identity.Function);
                    yield return TermHashStep.Scalar(identity.Arity);
                }
                else if (x.LocalIdentity is { } local)
                {
                    yield return TermHashStep.Child(local.Module);
                    yield return TermHashStep.Scalar(local.Index);
                    yield return TermHashStep.Child(local.Unique);
                    yield return TermHashStep.Scalar(x.Arity);
                    foreach (var value in x.Environment)
                        yield return TermHashStep.Child(value);
                }
                else
                {
                    yield return TermHashStep.Scalar(x.Identity.GetHashCode());
                }
                break;
            case TupleTerm x:
                foreach (var item in x.Items)
                    yield return TermHashStep.Child(item);
                break;
            case NativeRecordTerm x:
                yield return TermHashStep.Child(x.Module);
                yield return TermHashStep.Child(x.Name);
                yield return TermHashStep.Scalar(x.IsExported.GetHashCode());
                foreach (var field in x.FieldNames)
                    yield return TermHashStep.Child(field);
                foreach (var value in x.Values)
                    yield return TermHashStep.Child(value);
                break;
            case HamtBitmapTerm429 x:
                yield return TermHashStep.Scalar(x.Bitmap);
                foreach (var child in x.Children) yield return TermHashStep.Child(child);
                break;
            case MapTerm x:
                foreach (var item in x.Entries)
                {
                    yield return TermHashStep.Child(item.Key, TermHashStepKind.MapKey);
                    yield return TermHashStep.Child(item.Value, TermHashStepKind.MapValue);
                }
                yield return TermHashStep.Scalar(x.Entries.Count);
                yield return new TermHashStep(null, 0, TermHashStepKind.MapPairs);
                break;
            case Cons:
                while (t is Cons c)
                {
                    yield return TermHashStep.Child(c.Head);
                    t = c.Tail;
                }
                yield return TermHashStep.Child(t);
                break;
            case BitString x:
                yield return TermHashStep.Scalar(x.BitLength);
                foreach (byte item in x.ToArray())
                    yield return TermHashStep.Scalar(item.GetHashCode());
                break;
        }
    }
}
