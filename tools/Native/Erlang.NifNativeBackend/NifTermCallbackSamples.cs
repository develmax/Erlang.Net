using System.Numerics;

namespace Erlang.NifNativeBackend;

internal static class NifTermCallbackSamples
{
    internal static Term[] Values(ProcessContext context)
    {
        Atom module = Term.A(NifTermCallbackNames.Module);
        FunctionTerm closure = new(
            new LocalFunctionIdentity(
                module,
                Term.A(NifTermCallbackNames.Closure),
                1,
                2,
                new BitString(new byte[16])
            ),
            0,
            [],
            NeverInvoke
        );
        FunctionTerm exported = new(
            new ExternalFunctionIdentity(Term.A(ErlangModuleNames.Module), Term.A(ErlangModuleNames.Self), 0),
            NeverInvoke
        );

        return [
            Nil.Value,
            Term.List(Term.I(1), Term.I(2)),
            new Cons(Term.I(1), Term.A(NifTermCallbackNames.TailAtom)),
            Term.A(NifTermCallbackNames.Atom),
            Term.I(42),
            new Integer(BigInteger.One << 100),
            new FloatTerm(1.5),
            Term.Tuple(Term.A(NifTermCallbackNames.HeadAtom), Term.I(1)),
            Term.Tuple(),
            new BitString(new byte[] { 1, 2 }),
            new BitString(new byte[] { 1, 2 }),
            new BitString(new byte[] { 128 }, 1),
            closure,
            exported,
            context.Self,
            new Pid(NifTermCallbackNames.ExternalNode, 17, 7),
            context.Runtime.RehydratePort(new PortTerm(context.Self.Node, 1, context.Self.Creation)),
            new PortTerm(NifTermCallbackNames.ExternalNode, 23, 7),
            context.Runtime.MakeReference(),
            new ReferenceTerm(NifTermCallbackNames.ExternalNode, 31, 7),
            new MapTerm([new(Term.A(NifTermCallbackNames.HeadAtom), Term.I(1))]),
            Term.Tuple(Term.A(NifTermCallbackNames.Record), Term.A(NifTermCallbackNames.Payload))
        ];
    }

    // Independent fixture category matrix in original C predicate order.
    internal static readonly int[][] TruePredicates = [
        [6, 7], [7], [7], [0], [9], [9], [9], [8], [8], [1], [1], [],
        [3], [3], [4], [4], [5], [5], [2], [2], [10], [8]
    ];

    internal static Term Expected(int index, Term value)
    {
        Term flags = Term.Tuple(Enumerable.Range(0, 11).Select(column =>
            (Term)Term.I(TruePredicates[index].Contains(column) ? 1 : 0)).ToArray());
        Term cell = value is Cons cons
            ? Term.Tuple(Term.I(1), cons.Head, cons.Tail)
            : Term.Tuple(Term.I(0), Term.I(NifTermCallbackNames.SentinelHead), Term.I(NifTermCallbackNames.SentinelTail));

        return Term.Tuple(flags, cell);
    }

    private static ValueTask<Term> NeverInvoke(ITermExecutionContext context, IReadOnlyList<Term> arguments)
        => throw new InvalidOperationException(NifTermCallbackNames.Broken);
}
