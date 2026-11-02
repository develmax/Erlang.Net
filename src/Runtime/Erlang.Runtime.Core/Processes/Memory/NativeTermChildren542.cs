namespace Erlang;

// A two-edge Cons snapshot avoids a temporary Term[2]. Other term families
// keep the exact existing eagerly materialized children array and ordering.
internal readonly struct NativeTermChildren542
{
    private readonly Term? head;
    private readonly Term? tail;
    private readonly Term[]? other;
    private readonly int count;

    internal NativeTermChildren542(Term head, Term tail)
    {
        this.head = head;
        this.tail = tail;
        other = null;
        count = 2;
    }

    internal NativeTermChildren542(Term[] other)
    {
        head = null;
        tail = null;
        this.other = other;
        count = other.Length;
    }

    public Enumerator GetEnumerator() => new(
        head,
        tail,
        other,
        count
    );

    public struct Enumerator
    {
        private readonly Term? head;
        private readonly Term? tail;
        private readonly Term[]? other;
        private readonly int count;
        private int index;

        internal Enumerator(
            Term? head,
            Term? tail,
            Term[]? other,
            int count
        )
        {
            this.head = head;
            this.tail = tail;
            this.other = other;
            this.count = count;
            index = -1;
        }

        public bool MoveNext() => ++index < count;

        public Term Current => other is not null ? other[index] : index == 0 ? head! : tail!;
    }
}
