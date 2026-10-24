using Erlang;
using System.Globalization;

namespace Erlang.Tests;

internal static class IoPrintableOriginal404Terms
{
    internal static object Typed(Term term) => term switch
    {
        Nil => new { Kind = nameof(Nil) },
        Atom atom => new { Kind = nameof(Atom), Name = atom.Name },
        Integer integer => new { Kind = nameof(Integer), Decimal = integer.Value.ToString(CultureInfo.InvariantCulture) },
        FloatTerm number => new { Kind = nameof(FloatTerm), Bits = BitConverter.DoubleToInt64Bits(number.Value).ToString(IoPrintableOriginal404Names.FloatBitsFormat, CultureInfo.InvariantCulture) },
        BitString bits => new { Kind = nameof(BitString), bits.BitLength, BytesHex = Convert.ToHexString(bits.ToArray()) },
        TupleTerm tuple => new { Kind = nameof(TupleTerm), Items = tuple.Items.Select(Typed).ToArray() },
        MapTerm map => new { Kind = nameof(MapTerm), Entries = map.Entries.Select(pair => new { Key = Typed(pair.Key), Value = Typed(pair.Value) }).ToArray() },
        Cons => TypedList(term),
        Pid pid => new { Kind = nameof(Pid), pid.Node, Id = pid.Id.ToString(CultureInfo.InvariantCulture), pid.Creation },
        PortTerm port => new { Kind = nameof(PortTerm), port.Node, Id = port.Id.ToString(CultureInfo.InvariantCulture), port.Creation },
        ReferenceTerm reference => new { Kind = nameof(ReferenceTerm), reference.Node, reference.Creation, Words = reference.Words.ToArray() },
        _ => new { Kind = term.GetType().FullName, StructuredFieldsUnavailable = true, CompleteEtfBase64 = Convert.ToBase64String(ExternalTermFormat.Encode(term)) }
    };

    private static object TypedList(Term term)
    {
        var items = new List<object>();
        while (term is Cons cons)
        {
            items.Add(Typed(cons.Head));
            term = cons.Tail;
        }

        return new { Kind = nameof(Cons), Items = items, Tail = Typed(term) };
    }

    internal static Term[] List(Term term, out bool proper)
    {
        var items = new List<Term>();
        while (term is Cons cons)
        {
            items.Add(cons.Head);
            term = cons.Tail;
        }
        proper = term is Nil;

        return items.ToArray();
    }
}
