using System.Buffers.Binary;
using System.Numerics;

namespace Erlang;

internal sealed unsafe class MatchFragmentReader604(MatchSavedFragment604 owner) : IDisposable
{
    private readonly object gate = new();
    private MatchSavedFragment604? owned = owner;

    internal bool Owns604(MatchSavedFragment604 candidate) => ReferenceEquals(owned, candidate);

    internal Term Materialize604(CancellationToken cancellation)
    {
        lock (gate)
            return (owned ?? throw new ObjectDisposedException(MatchFragmentNames604.Closed)).Materialize604(this, cancellation);
    }

    public void Dispose()
    {
        MatchSavedFragment604? release;
        lock (gate)
        {
            release = owned;
            owned = null;
        }
        release?.ReleaseReader604();
    }

    internal static Term Decode604(
        ProcessRuntime runtime,
        nint memory,
        int extent,
        ulong root,
        CancellationToken cancellation
    )
    {
        using var atomScope = new AtomNameScope(runtime.AtomNames);
        var decoded = new Dictionary<ulong, Term>();
        var active = new HashSet<ulong>();
        var pending = new Stack<(ulong Word, ulong[]? Children, bool Tuple)>();
        pending.Push((root, null, false));
        nuint start = checked((nuint)memory + MatchFragmentNames604.PrefixBytes);
        nuint bytes = checked((nuint)extent * MatchFragmentNames604.WordBytes);

        ulong* Locate(ulong tagged, int count)
        {
            nuint pointer = (nuint)(tagged & ~(ulong)MatchFragmentNames604.PrimaryMask);
            if (pointer < start || (pointer - start) % MatchFragmentNames604.WordBytes != 0
                || (nuint)count > (nuint)extent || pointer - start > bytes - (nuint)count * MatchFragmentNames604.WordBytes)
                throw new InvalidOperationException(MatchFragmentNames604.Invalid);

            return (ulong*)pointer;
        }
        while (pending.TryPop(out var work))
        {
            cancellation.ThrowIfCancellationRequested();
            if (decoded.ContainsKey(work.Word)) continue;
            if (decoded.Count + active.Count >= MatchFragmentNames604.MaximumNodes)
                throw new InvalidOperationException(MatchFragmentNames604.Invalid);
            if (work.Children is not null)
            {
                Term[] children = work.Children.Select(word => decoded[word]).ToArray();
                decoded.Add(work.Word, work.Tuple ? new TupleTerm(children) : new Cons(children[0], children[1]));
                active.Remove(work.Word);
                continue;
            }
            if (work.Word == MatchFragmentNames604.Nil)
            {
                decoded.Add(work.Word, Nil.Value);
                continue;
            }
            if ((work.Word & MatchFragmentNames604.SmallMask) == MatchFragmentNames604.SmallTag)
            {
                decoded.Add(work.Word, Term.I(unchecked((long)work.Word) >> MatchFragmentNames604.SmallShift));
                continue;
            }
            if ((work.Word & MatchFragmentNames604.AtomMask) == MatchFragmentNames604.AtomTag)
            {
                ulong index = work.Word >> MatchFragmentNames604.AtomShift;
                if (index > int.MaxValue || !runtime.AtomNames.TryNameAt((int)index, out string name))
                    throw new InvalidOperationException(MatchFragmentNames604.Invalid);
                decoded.Add(work.Word, Term.A(name));
                continue;
            }
            bool tuple = (work.Word & MatchFragmentNames604.PrimaryMask) == MatchFragmentNames604.BoxedTag;
            bool list = (work.Word & MatchFragmentNames604.PrimaryMask) == MatchFragmentNames604.ListTag;
            if ((!tuple && !list) || !active.Add(work.Word)) throw new InvalidOperationException(MatchFragmentNames604.Invalid);
            ulong* data = Locate(work.Word, tuple ? 1 : 2);
            int count = 2;
            if (tuple)
            {
                ulong header = *data;
                if (header == (ulong)BoxedFloat556Names.Header)
                {
                    data = Locate(work.Word, BoxedFloat556Names.HeaderWords + BoxedFloat556Names.PayloadWords);
                    double number = BitConverter.Int64BitsToDouble(unchecked((long)data[BoxedFloat556Names.HeaderWords]));
                    if (!double.IsFinite(number)) throw new InvalidOperationException(MatchFragmentNames604.Invalid);
                    decoded.Add(work.Word, new FloatTerm(number));
                    active.Remove(work.Word);
                    continue;
                }
                ulong subtag = header & MatchFragmentNames604.AtomMask;
                if (subtag == (ulong)BoxedInteger559Names.PositiveSubtag || subtag == (ulong)BoxedInteger559Names.NegativeSubtag)
                {
                    ulong digits = header >> BoxedInteger559Names.HeaderArityShift;
                    if (digits == 0 || digits > BoxedInteger559Names.MaximumDigits)
                        throw new InvalidOperationException(MatchFragmentNames604.Invalid);
                    int length = checked((int)digits);
                    data = Locate(work.Word, checked(length + BoxedInteger559Names.HeaderWords));
                    if (data[length] == 0) throw new InvalidOperationException(MatchFragmentNames604.Invalid);
                    var digitBytes = new byte[checked(length * BoxedInteger559Names.DigitBytes)];
                    for (int index = 0; index < length; index++)
                        BinaryPrimitives.WriteUInt64LittleEndian(
                            digitBytes.AsSpan(index * BoxedInteger559Names.DigitBytes, BoxedInteger559Names.DigitBytes),
                            data[index + BoxedInteger559Names.HeaderWords]
                        );
                    BigInteger number = new(digitBytes, isUnsigned: true, isBigEndian: false);
                    if (subtag == (ulong)BoxedInteger559Names.NegativeSubtag) number = -number;
                    if (number >= MatchFragmentNames604.MinimumSmall && number <= MatchFragmentNames604.MaximumSmall)
                        throw new InvalidOperationException(MatchFragmentNames604.Invalid);
                    decoded.Add(work.Word, new Integer(number));
                    active.Remove(work.Word);
                    continue;
                }
                if ((header & MatchFragmentNames604.AtomMask) != 0 || (header >> MatchFragmentNames604.ArityShift) > int.MaxValue - 1)
                    throw new InvalidOperationException(MatchFragmentNames604.Invalid);
                count = (int)(header >> MatchFragmentNames604.ArityShift);
                data = Locate(work.Word, checked(count + 1)) + 1;
            }
            var words = new ulong[count];
            for (int index = 0; index < count; index++) words[index] = data[index];
            pending.Push((work.Word, words, tuple));
            for (int index = words.Length - 1; index >= 0; index--)
                pending.Push((words[index], null, false));
        }

        return decoded[root];
    }
}
