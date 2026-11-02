using System.Collections;

namespace Erlang;

// The actual global tuple payload. There is no CLR child array and no Term
// GCHandle in its native header or following safety word. Even Count consults
// that real header; the safety word contributes no tuple/process payload.
internal sealed class RuntimeEmptyTupleStorage803(RuntimeEmptyTupleDomain803 domain) : IOwnedTermSequence506
{
    internal RuntimeEmptyTupleDomain803 Domain803 => domain;
    public object Owner => domain;
    public long Identity => RuntimeEmptyTuple803Names.Identity;
    public int Count => domain.ReadArity803(this);
    public Term this[int index]
    {
        get
        {
            _ = domain.ReadArity803(this);
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    public IEnumerator<Term> GetEnumerator()
    {
        using RuntimeEmptyTupleRead803 read = domain.EnterRead803(this);
        domain.RequireHeader803();
        yield break;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
