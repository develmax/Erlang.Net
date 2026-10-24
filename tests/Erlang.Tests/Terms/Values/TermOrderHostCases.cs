using Erlang;
using System.Reflection;

namespace Erlang.Tests;

internal static class TermOrderHostCases
{
    internal static Term Build(string family, int depth, int leaf)
    {
        Term value = Term.I(leaf);
        for (int i = 0; i < depth; i++)
        {
            value = family switch
            {
                TermOrderHostNames.Tuple => Term.Tuple(value),
                TermOrderHostNames.ListHead => new Cons(value, Nil.Value),
                TermOrderHostNames.MapValue => new MapTerm([new(Term.A(TermOrderHostNames.Leaf), value)]),
                TermOrderHostNames.FunctionEnvironment => new FunctionTerm(
                    new LocalFunctionIdentity(
                        Term.A(TermOrderHostNames.Module),
                        Term.A(TermOrderHostNames.Function),
                        0,
                        0,
                        new BitString(new byte[16])
                    ),
                    0,
                    [value],
                    (_, _) => ValueTask.FromResult<Term>(Nil.Value)
                ),
                TermOrderHostNames.NativeRecord => new NativeRecordTerm(
                    Term.A(TermOrderHostNames.Module),
                    Term.A(TermOrderHostNames.Record),
                    true,
                    [new(Term.A(TermOrderHostNames.Field), value)]
                ),
                _ => throw new ArgumentException(nameof(family))
            };
        }

        return value;
    }

    internal static void Shallow()
    {
        foreach (string family in new[] { TermOrderHostNames.Tuple, TermOrderHostNames.ListHead, TermOrderHostNames.MapValue,
            TermOrderHostNames.FunctionEnvironment, TermOrderHostNames.NativeRecord })
        {
            Term a = Build(family, 6, 0), b = Build(family, 6, 0), c = Build(family, 6, 1);
            if (!a.Equals(b) || a.CompareTo(c) >= 0 || a.GetHashCode() != TermOrderBaselineHash.Hash(a) || b.GetHashCode() != TermOrderBaselineHash.Hash(b))
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
        var scalar = new Term[] { integer, floating, positiveZero, negativeZero, Term.A(TermOrderHostNames.Leaf),
            Nil.Value, new FunctionTerm(0, (_,_) => ValueTask.FromResult<Term>(Nil.Value)), new BitString([0x80], 1), new Pid(TermOrderHostNames.BeforeNode, 1),
            Term.List(Term.I(1),Term.I(2)), Cons.From([Term.I(1)], Term.I(3)),
            new FunctionTerm(
                new ExternalFunctionIdentity(Term.A(TermOrderHostNames.Module),Term.A(TermOrderHostNames.Function),0),
                (_,_)=>ValueTask.FromResult<Term>(Nil.Value)
            ) };
        foreach (var term in scalar)
            if (term.GetHashCode() != TermOrderBaselineHash.Hash(term))
                throw new InvalidOperationException(nameof(TermOrderBaselineHash));
    }

    internal static void Snapshot()
    {
        var assembly = typeof(Term).Assembly;
        var identityType = assembly.GetType(TermOrderHostNames.IdentityType, true)!;
        var contextType = assembly.GetType(TermOrderHostNames.ContextType, true)!;
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        var identity = Activator.CreateInstance(
            identityType,
            flags,
            null,
            [TermOrderHostNames.BeforeNode, (uint)1],
            null
        )!;
        var local = (Pid)Activator.CreateInstance(
            typeof(Pid),
            flags,
            null,
            [identity, (ulong)1],
            null
        )!;
        var external = new Pid(TermOrderHostNames.BeforeNode, 1, 1);
        var compare = typeof(TermOrder).GetMethod(TermOrderHostNames.CompareSnapshot, BindingFlags.NonPublic | BindingFlags.Static)!;
        object?[] parameters = [local, external, true, Activator.CreateInstance(contextType)];
        if ((int)compare.Invoke(null, parameters)! != 0)
            throw new InvalidOperationException(TermOrderHostNames.Snapshot);
        identityType.GetMethod(TermOrderHostNames.Publish, flags)!.Invoke(identity, [TermOrderHostNames.AfterNode, (uint)2]);
        if ((int)compare.Invoke(null, parameters)! != 0 || local.CompareTo(external) == 0)
            throw new InvalidOperationException(TermOrderHostNames.Snapshot);
    }
}
