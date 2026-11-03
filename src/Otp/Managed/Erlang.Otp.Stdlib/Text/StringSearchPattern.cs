using System.Collections;

namespace Erlang;

internal sealed class StringSearchPattern : IReadOnlyList<Term>
{
    private readonly Term[] values;

    internal StringSearchPattern(Term original)
    {
        Original = original;
        values = original is Cons or Nil ? Cons.Items(original).ToArray() : [];
    }

    internal Term Original { get; }
    internal Term? FullCompiled { get; set; }
    internal Term? InitialCompiled { get; set; }
    internal bool HasHead => Original is Cons;
    public int Count => values.Length;
    public Term this[int index] => values[index];

    public IEnumerator<Term> GetEnumerator() => ((IEnumerable<Term>)values).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
