using Erlang;
using System.Reflection;

internal static class TermOrderProbeChecks
{
    internal static Term Build(string family, int depth, int leaf)
    {
        Term value = Term.I(leaf);
        for (int i = 0; i < depth; i++)
        {
            value = family switch
            {
                TermOrderProbeNames.Tuple => Term.Tuple(value),
                TermOrderProbeNames.ListHead => new Cons(value, Nil.Value),
                TermOrderProbeNames.MapValue => new MapTerm([new(Term.A(TermOrderProbeNames.Leaf), value)]),
                TermOrderProbeNames.FunctionEnvironment => new FunctionTerm(
                    new LocalFunctionIdentity(Term.A(TermOrderProbeNames.Module), Term.A(TermOrderProbeNames.Function), 0, 0, new BitString(new byte[16])),
                    0, [value], (_, _) => ValueTask.FromResult<Term>(Nil.Value)),
                TermOrderProbeNames.NativeRecord => new NativeRecordTerm(Term.A(TermOrderProbeNames.Module), Term.A(TermOrderProbeNames.Record), true,
                    [new(Term.A(TermOrderProbeNames.Field), value)]),
                _ => throw new ArgumentException(nameof(family))
            };
        }
        return value;
    }

    internal static void Shallow()
    {
        foreach (string family in new[] { TermOrderProbeNames.Tuple, TermOrderProbeNames.ListHead, TermOrderProbeNames.MapValue,
            TermOrderProbeNames.FunctionEnvironment, TermOrderProbeNames.NativeRecord })
        {
            Term a = Build(family, 6, 0), b = Build(family, 6, 0), c = Build(family, 6, 1);
            if (!a.Equals(b) || a.CompareTo(c) >= 0 || a.GetHashCode() != BaselineTermHash.Hash(a) || b.GetHashCode() != BaselineTermHash.Hash(b))
                throw new InvalidOperationException(family);
        }
        Term integer = Term.I(1), floating = new FloatTerm(1), positiveZero = new FloatTerm(0.0), negativeZero = new FloatTerm(-0.0);
        if (integer.Equals(floating) || !integer.NumericEquals(floating) || !positiveZero.NumericEquals(negativeZero) || positiveZero.Equals(negativeZero))
            throw new InvalidOperationException(nameof(Shallow));
        var keyFirst = new MapTerm([new(Term.I(1), Term.I(9)), new(Term.I(2), Term.I(0))]);
        var keyLater = new MapTerm([new(Term.I(1), Term.I(0)), new(Term.I(3), Term.I(0))]);
        if (keyFirst.CompareTo(keyLater) >= 0)
            throw new InvalidOperationException(nameof(MapTerm));
        var numericA = new MapTerm([new(Term.I(1), Term.I(1))]);
        var numericB = new MapTerm([new(Term.I(1), new FloatTerm(1))]);
        if (!numericA.NumericEquals(numericB) || numericA.Equals(numericB))
            throw new InvalidOperationException(nameof(MapTerm));
        var scalar = new Term[] { integer, floating, positiveZero, negativeZero, Term.A(TermOrderProbeNames.Leaf),
            Nil.Value, new BitString([0x80], 1), new Pid(TermOrderProbeNames.BeforeNode, 1),
            Term.List(Term.I(1),Term.I(2)), Cons.From([Term.I(1)], Term.I(3)),
            new FunctionTerm(new ExternalFunctionIdentity(Term.A(TermOrderProbeNames.Module),Term.A(TermOrderProbeNames.Function),0),(_,_)=>ValueTask.FromResult<Term>(Nil.Value)) };
        foreach (var term in scalar)
            if (term.GetHashCode() != BaselineTermHash.Hash(term))
                throw new InvalidOperationException(nameof(BaselineTermHash));
    }

    internal static void Snapshot()
    {
        var assembly = typeof(Term).Assembly;
        var identityType = assembly.GetType(TermOrderProbeNames.IdentityType, true)!;
        var contextType = assembly.GetType(TermOrderProbeNames.ContextType, true)!;
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        var identity = Activator.CreateInstance(identityType, flags, null, [TermOrderProbeNames.BeforeNode, (uint)1], null)!;
        var local = (Pid)Activator.CreateInstance(typeof(Pid), flags, null, [identity, (ulong)1], null)!;
        var external = new Pid(TermOrderProbeNames.BeforeNode, 1, 1);
        var compare = typeof(TermOrder).GetMethod(TermOrderProbeNames.CompareSnapshot, BindingFlags.NonPublic | BindingFlags.Static)!;
        object?[] parameters = [local, external, true, Activator.CreateInstance(contextType)];
        if ((int)compare.Invoke(null, parameters)! != 0)
            throw new InvalidOperationException(TermOrderProbeNames.Snapshot);
        identityType.GetMethod(TermOrderProbeNames.Publish, flags)!.Invoke(identity, [TermOrderProbeNames.AfterNode, (uint)2]);
        if ((int)compare.Invoke(null, parameters)! != 0 || local.CompareTo(external) == 0)
            throw new InvalidOperationException(TermOrderProbeNames.Snapshot);
    }
}
