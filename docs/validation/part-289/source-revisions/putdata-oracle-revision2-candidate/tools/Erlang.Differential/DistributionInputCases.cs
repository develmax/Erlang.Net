namespace Erlang.Differential;

public static class DistributionInputCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(
            string name,
            string dataSource,
            Term data,
            bool dynamicApply = false,
            bool referenceHandle = false
        )
        {
            string handleSource = referenceHandle ? "make_ref()" : DistributionInputCaseNames.InvalidHandle;
            string call = dynamicApply ? DistributionInputCaseNames.DynamicCall : DistributionInputCaseNames.DirectCall;
            string projection = referenceHandle ? DistributionInputCaseNames.ReferenceProjection : DistributionInputCaseNames.ExactProjection;
            string source = DistributionInputCaseNames.ModulePrefix
                + "oracle_dist_input:invoke(erlang,dist_ctrl_put_data," + handleSource + "," + dataSource + "). "
                + (dynamicApply ? "invoke(M,F,H,D)->try " : "invoke(_M,_F,H,D)->try ") + call
                + " of V->{unexpected_success,V} catch C:R:Stack->[{M1,F1,Args,_Annotations}|_]=Stack,"
                + projection + " end.";
            Term handle = Term.A(referenceHandle ? DistributionInputCaseNames.OrdinaryReference : DistributionInputCaseNames.InvalidHandle);
            Term expected = Term.Tuple(
                Term.A(OracleOutcomeTags.Success),
                Term.Tuple(
                    Term.A(DistributionInputCaseNames.ErrorClass),
                    Term.A(DistributionInputCaseNames.BadArgument),
                    Term.Tuple(
                        Term.A(DistributionInputCaseNames.Module),
                        Term.A(DistributionInputCaseNames.Function),
                        Term.List(handle, data)
                    )
                )
            );
            cases.Add(new(DistributionInputCaseNames.Prefix + name, source, expected));
        }

        Add("atom-nil-direct", "[]", Nil.Value);
        Add("atom-empty-binary-apply", "<<>>", new BitString([]), dynamicApply: true);
        Add(
            "atom-nested-iolist-direct",
            "[1,[2,<<3,4>>],[]]",
            Term.List(Term.I(1), Term.List(Term.I(2), new BitString([3, 4])), Nil.Value)
        );
        Add(
            "atom-binary-tail-iolist-apply",
            "[1|<<2,3>>]",
            new Cons(Term.I(1), new BitString([2, 3])),
            dynamicApply: true
        );
        Add("atom-out-of-range-iolist-direct", "[256]", Term.List(Term.I(256)));
        Add("atom-improper-iolist-apply", "[1|2]", new Cons(Term.I(1), Term.I(2)), dynamicApply: true);
        Add("atom-integer-direct", "17", Term.I(17));
        Add("atom-nonbinary-bitstring-apply", "<<1:1>>", new BitString([128], 1), dynamicApply: true);
        Add("ordinary-reference-nil-direct", "[]", Nil.Value, referenceHandle: true);
        Add(
            "ordinary-reference-iolist-apply",
            "[1,<<2>>]",
            Term.List(Term.I(1), new BitString([2])),
            dynamicApply: true,
            referenceHandle: true
        );

        return cases;
    }
}
