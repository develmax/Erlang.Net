namespace Erlang.Differential;

public static class ApplyFrameCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body, Term expected) => cases.Add(new(
            ApplyFrameCaseNames.Prefix + name,
            ApplyFrameCaseNames.ModulePrefix + body + "." + ApplyFrameCaseNames.Helpers,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), expected)
        ));

        Term Failure(
            string reason,
            string module,
            string function,
            Term arguments
        ) => Term.Tuple(
            Term.A(ApplyFrameCaseNames.Error),
            Term.A(reason),
            Term.Tuple(Term.A(module), Term.A(function), arguments)
        );

        Term badHead = Failure(
            ApplyFrameCaseNames.BadArgument,
            ApplyFrameCaseNames.Erlang,
            ApplyFrameCaseNames.Head,
            Term.List(Term.A(ApplyFrameCaseNames.NonList))
        );
        Add(
            "badarg-direct-and-dynamic",
            "D=try erlang:hd(not_a_list) catch C:R:S->project(C,R,S) end,A=oracle_apply_frame:invoke(erlang,hd,[not_a_list]),{D,A}",
            Term.Tuple(badHead, badHead)
        );
        Term undefined = Failure(
            ApplyFrameCaseNames.Undefined,
            ApplyFrameCaseNames.MissingModule,
            ApplyFrameCaseNames.MissingFunction,
            Term.List(Term.I(17))
        );
        Add(
            "undef-direct-and-dynamic",
            "D=try missing_apply_frame_module:absent(17) catch C:R:S->project(C,R,S) end,A=oracle_apply_frame:invoke(missing_apply_frame_module,absent,[17]),{D,A}",
            Term.Tuple(undefined, undefined)
        );
        Add(
            "invalid-module-keeps-apply-frame",
            "oracle_apply_frame:invoke(0,hd,[])",
            Failure(
                ApplyFrameCaseNames.BadArgument,
                ApplyFrameCaseNames.Erlang,
                ApplyFrameCaseNames.Apply,
                Term.List(Term.I(0), Term.A(ApplyFrameCaseNames.Head), Nil.Value)
            )
        );
        Add(
            "invalid-function-keeps-apply-frame",
            "oracle_apply_frame:invoke(erlang,0,[])",
            Failure(
                ApplyFrameCaseNames.BadArgument,
                ApplyFrameCaseNames.Erlang,
                ApplyFrameCaseNames.Apply,
                Term.List(Term.A(ApplyFrameCaseNames.Erlang), Term.I(0), Nil.Value)
            )
        );
        Add(
            "improper-arguments-keeps-apply-frame",
            "oracle_apply_frame:invoke(erlang,hd,[1|2])",
            Failure(
                ApplyFrameCaseNames.BadArgument,
                ApplyFrameCaseNames.Erlang,
                ApplyFrameCaseNames.Apply,
                Term.List(Term.A(ApplyFrameCaseNames.Erlang), Term.A(ApplyFrameCaseNames.Head), new Cons(Term.I(1), Term.I(2)))
            )
        );
        Term trace = Term.List(Term.Tuple(
            Term.A(ApplyFrameCaseNames.TraceModule),
            Term.A(ApplyFrameCaseNames.TraceFunction),
            Term.List(Term.I(17)),
            Nil.Value
        ));

        Term Raised(string exceptionClass, Term stack) => Term.Tuple(Term.A(exceptionClass), Term.A(ApplyFrameCaseNames.Reason), stack);
        Add(
            "explicit-raise-preserves-all-classes-and-empty-stack",
            "T=[{synthetic_module,synthetic_function,[17],[]}],{oracle_apply_frame:invoke(erlang,raise,[error,reason,T]),oracle_apply_frame:invoke(erlang,raise,[throw,reason,T]),oracle_apply_frame:invoke(erlang,raise,[exit,reason,T]),oracle_apply_frame:invoke(erlang,raise,[error,reason,[]]),oracle_apply_frame:invoke(erlang,raise,[throw,reason,[]]),oracle_apply_frame:invoke(erlang,raise,[exit,reason,[]])}",
            Term.Tuple(
                Raised(ApplyFrameCaseNames.Error, trace),
                Raised(ApplyFrameCaseNames.Throw, trace),
                Raised(ApplyFrameCaseNames.Exit, trace),
                Raised(ApplyFrameCaseNames.Error, Nil.Value),
                Raised(ApplyFrameCaseNames.Throw, Nil.Value),
                Raised(ApplyFrameCaseNames.Exit, Nil.Value)
            )
        );

        return cases;
    }
}
