using Erlang;

namespace Erlang.Tests;

internal static class TermOrderBaselineHash
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
        MapTerm => 8,
        Nil => 9,
        Cons => 10,
        BitString => 11,
        _ => throw new NotSupportedException()
    };

    public static int Hash(Term t)
    {
        var h = new HashCode();
        h.Add(Rank(t));
        switch (t)
        {
            case Integer x:
                h.Add(0);
                h.Add(x.Value);
                break;
            case FloatTerm x:
                h.Add(1);
                h.Add(BitConverter.DoubleToInt64Bits(x.Value));
                break;
            case Atom x:
                h.Add(x.Name, StringComparer.Ordinal);
                break;
            case Pid x:
                h.Add(x.Id);
                break;
            case ReferenceTerm x:
                h.Add(x.FullId);
                break;
            case PortTerm x:
                h.Add(x.Id);
                break;
            case FunctionTerm x:
                if (x.ExternalIdentity is { } identity)
                {
                    h.Add(Hash(identity.Module));
                    h.Add(Hash(identity.Function));
                    h.Add(identity.Arity);
                }
                else if (x.LocalIdentity is { } local)
                {
                    h.Add(Hash(local.Module));
                    h.Add(local.Index);
                    h.Add(Hash(local.Unique));
                    h.Add(x.Arity);
                    foreach (var value in x.Environment)
                        h.Add(Hash(value));
                }
                else
                {
                    h.Add((long)typeof(FunctionTerm).GetProperty(TermOrderHostNames.IdentityProperty, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(x)!);
                }
                break;
            case TupleTerm x:
                foreach (var item in x.Items)
                    h.Add(Hash(item));
                break;
            case NativeRecordTerm x:
                h.Add(Hash(x.Module));
                h.Add(Hash(x.Name));
                h.Add(x.IsExported);
                foreach (var field in x.FieldNames)
                    h.Add(Hash(field));
                foreach (var value in x.Values)
                    h.Add(Hash(value));
                break;
            case MapTerm x:
                int pairs = 0;
                foreach (var item in x.Entries)
                    pairs = unchecked(pairs + HashCode.Combine(Hash(item.Key), Hash(item.Value)));
                h.Add(x.Entries.Count);
                h.Add(pairs);
                break;
            case Cons:
                while (t is Cons c)
                {
                    h.Add(Hash(c.Head));
                    t = c.Tail;
                }
                h.Add(Hash(t));
                break;
            case BitString x:
                h.Add(x.BitLength);
                foreach (byte item in x.ToArray())
                    h.Add(item);
                break;
        }

        return h.ToHashCode();
    }
}
