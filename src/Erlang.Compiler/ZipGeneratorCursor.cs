// Modified: iterative source state for OTP-29.1.1 erl_eval zip generators.
namespace Erlang.Compiler;

internal sealed class ZipGeneratorCursor
{
    private Term remaining;
    private readonly BitString? bits;
    private readonly byte[]? bytes;
    private readonly IReadOnlyList<KeyValuePair<Term, Term>>? entries;
    private Term? compiledIterator;
    private int position;
    public ComprehensionGenerator Generator { get; }

    public ZipGeneratorCursor(ComprehensionGenerator generator, Term source)
    {
        Generator = generator;
        remaining = source;
        if (generator.Binary && source is BitString input)
        {
            bits = input;
            bytes = input.ToArray();
        }
        if (generator.Map)
        {
            try
            {
                entries = MapIteration.Entries(source);
                compiledIterator = source is TupleTerm or Atom ? source : IteratorRemainder();
            }
            catch (ErlangException)
            {
                throw new ErlangException(Term.Tuple(Term.A(ComprehensionErrorReasons.BadGenerator), source));
            }
        }
    }

    public bool Read(
        Pattern pattern,
        Dictionary<string, Term> scope,
        Dictionary<string, Term> bindings,
        ProcessContext context,
        out bool matched,
        out int consumed
    )
    {
        matched = false;
        consumed = 0;
        if (Generator.Binary)
        {
            if (bits is null)
                return false;

            return ((BitPattern)pattern).ReadGenerator(
                bits,
                bytes!,
                position,
                bindings,
                context,
                scope,
                out consumed,
                out matched
            );
        }
        Term item;
        if (entries is not null)
        {
            if (position == entries.Count)
                return false;
            var pair = entries[position];
            item = Term.Tuple(pair.Key, pair.Value);
        }
        else if (remaining is Cons cell)
            item = cell.Head;
        else
            return false;
        consumed = 1;
        matched = pattern.Match(
            item,
            bindings,
            context,
            scope
        );

        return true;
    }

    public bool IsDone(bool complete) => entries is not null ? position == entries.Count : Generator.Binary ? bits is not null && !complete : remaining is Nil;

    public bool HasStrictBinaryTail => Generator.Strict && bits is not null && position != bits.BitLength;

    public bool CompiledTail => entries is not null ? position == entries.Count : Generator.Binary ? bits is not null && (!Generator.Strict || position == bits.BitLength) : remaining is Nil;

    public bool NormalizedEnd(bool complete) => entries is not null ? position == entries.Count : bits is not null ? position == bits.BitLength || !complete : remaining is Nil or BitString { BitLength: 0 } or MapTerm { Entries.Count: 0 };

    public void Advance(int consumed)
    {
        if (Generator.Binary)
        {
            position += consumed;
        }
        else if (entries is not null)
        {
            position++;
            compiledIterator = ((TupleTerm)compiledIterator!).Items[ComprehensionIteratorLayout.ContinuationIndex];
            if (compiledIterator is Cons)
                compiledIterator = IteratorRemainder();
        }
        else
            remaining = ((Cons)remaining).Tail;
    }

    public Term Remainder(bool normalized, bool complete)
    {
        if (entries is not null)
            return normalized ? new MapTerm(entries.Skip(position)) : compiledIterator!;
        if (bits is null)
            return remaining;
        if (normalized && !complete)
            return new BitString([]);
        int count = bits.BitLength - position;
        var rest = new byte[(int)(((long)count + BitStorageLayout.ByteRoundingOffset) / BitStorageLayout.BitsPerByte)];
        for (int bit = 0; bit < count; bit++)
            if (((bytes![(position + bit) / BitStorageLayout.BitsPerByte] >> (BitStorageLayout.MostSignificantBitIndex - (position + bit) % BitStorageLayout.BitsPerByte)) & 1) != 0)
                rest[bit / BitStorageLayout.BitsPerByte] |= (byte)(1 << (BitStorageLayout.MostSignificantBitIndex - bit % BitStorageLayout.BitsPerByte));

        return new BitString(rest, count);
    }

    private Term IteratorRemainder()
    {
        Term rest = Term.A(ComprehensionIteratorAtoms.End);
        for (int index = entries!.Count - 1; index >= position; index--)
            rest = Term.Tuple(entries[index].Key, entries[index].Value, rest);

        return rest;
    }
}
