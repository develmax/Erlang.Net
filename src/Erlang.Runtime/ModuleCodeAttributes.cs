using System.Numerics;

namespace Erlang;

public static class ModuleCodeAttributes
{
    public static Term Values(Term value) => value is Cons or Nil ? value : Term.List(value);

    internal static Term WithDefaultVersion(Term? attributes, BitString fingerprint)
    {
        attributes ??= Nil.Value;
        if (Cons.Items(attributes).Any(item => item is TupleTerm { Items.Count: 2 } tuple && tuple.Items[0] is Atom { Name: ModuleInformationKeys.VersionAttribute }))
            return attributes;
        Term version = Term.Tuple(
            Term.A(ModuleInformationKeys.VersionAttribute),
            Term.List(new Integer(new BigInteger(fingerprint.ToArray(), isUnsigned: true, isBigEndian: true)))
        );

        return new Cons(version, attributes);
    }
}
