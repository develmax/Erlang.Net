using System.Numerics;

namespace Erlang;

internal sealed record RuntimeMicrostateSnapshot(string Type, int Identity, BigInteger[] Counters)
{
    internal Term ToTerm() => new MapTerm([
        new(Term.A(RuntimeMicrostateNames.Type), Term.A(Type)),
        new(Term.A(RuntimeMicrostateNames.Identity), Term.I(Identity)),
        new(
            Term.A(RuntimeMicrostateNames.Counters),
            new MapTerm(RuntimeMicrostateNames.States.Select((name, index) =>
            new KeyValuePair<Term, Term>(Term.A(name), new Integer(Counters[index]))))
        )
    ]);
}
