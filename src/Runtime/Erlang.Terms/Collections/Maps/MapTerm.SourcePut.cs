namespace Erlang;

public sealed partial class MapTerm
{
    // The native single-key put searches exact identity before inserting at
    // the first non-less key. Distinct local code identities can compare zero.
    internal MapTerm SourcePut(Term key, Term value) => new(TryGet(key, out _)
        ? ArithmeticKeyEntries.Append(new(key, value))
        : ArithmeticKeyEntries.Prepend(new(key, value)));
}
