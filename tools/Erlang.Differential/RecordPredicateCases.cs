namespace Erlang.Differential;

public static class RecordPredicateCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    public static string? FixtureName(string module, string function, int arity) => module == ErlangModuleNames.Module && function == RecordPredicateNames.IsRecord ? arity switch
    {
        2 => "erlang-record-predicate-weak-two",
        3 => "erlang-record-predicate-sized-three",
        _ => null
    } : null;

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(
            string name,
            string body,
            string declarations = "",
            string helpers = ""
        ) => cases.Add(new(
            "erlang-record-predicate-" + name,
            "-module(record_predicate_contract). -export([run/0]). " + declarations + " run()->" + body + ",done. id(X)->X. " + helpers,
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        Add(
            "weak-two",
            "true=apply(erlang,is_record,[{r},r]),true=apply(erlang,is_record,[{r,one,two},r]),false=apply(erlang,is_record,[{},r]),false=apply(erlang,is_record,[{other},r])"
        );
        Add(
            "sized-three",
            "true=apply(erlang,is_record,[{r,one},r,2]),false=apply(erlang,is_record,[{r,one},r,3]),false=apply(erlang,is_record,[{other,one},r,2])"
        );
        Add(
            "declared-local",
            "true=is_record(#r{},r),true=is_record({r,one,two},r),false=is_record({r},r),false=is_record({r,one,two,three},r)",
            "-record(r,{first,second})."
        );
        Add(
            "declared-remote",
            "true=erlang:is_record(#r{},r),false=erlang:is_record({r},r),false=erlang:is_record({other,one,two},r)",
            "-record(r,{first,second})."
        );
        Add("empty-declaration", "true=is_record({r},r),false=is_record({},r),false=is_record({r,one},r)", "-record(r,{}).");
        Add(
            "literal-evaluates-once",
            "true=is_record(begin self()!once,{r,one} end,r),receive once->ok after 1000->error(timeout) end,receive once->error(duplicate) after 0->ok end",
            "-record(r,{field})."
        );
        Add(
            "dynamic-two",
            "Name=id(r),true=is_record({r},Name),true=erlang:is_record({r,one,two},Name),false=is_record({other},Name)"
        );
        Add("external-constructed-two", "F=erlang:make_fun(erlang,is_record,2),true=F({r},r),false=F({other},r)");
        Add("external-constructed-three", "F=erlang:make_fun(erlang,is_record,3),true=F({r,one},r,2),false=F({r},r,2)");
        Add(
            "guard-declared",
            "yes=check({r,one}),no=check({r}),no=check({other,one}),no=check(42)",
            "-record(r,{field}).",
            "check(X) when is_record(X,r)->yes;check(_)->no."
        );
        Add(
            "guard-remote",
            "yes=check({r,one}),no=check({r}),no=check([])",
            "-record(r,{field}).",
            "check(X) when erlang:is_record(X,r)->yes;check(_)->no."
        );
        Add(
            "guard-literal-three",
            "yes=check({r,one}),no=check({r}),no=check({other,one}),no=check(42)",
            "",
            "check(X) when is_record(X,r,2)->yes;check(_)->no."
        );
        Add(
            "guard-literal-three-remote",
            "yes=check({r,one}),no=check({r})",
            "",
            "check(X) when erlang:is_record(X,r,2)->yes;check(_)->no."
        );
        Add(
            "guard-literal-zero",
            "no=check({r}),no=check(42)",
            "",
            "check(X) when is_record(X,r,0)->yes;check(_)->no."
        );
        Add(
            "guard-literal-module",
            "no=check({r}),no=check(42)",
            "",
            "check(X) when is_record(X,r,record_predicate_contract)->yes;check(_)->no."
        );
        Add(
            "guard-disjunction",
            "yes=check({r,one}),yes=check(fallback),no=check({r})",
            "-record(r,{field}).",
            "check(X) when is_record(X,r);X=:=fallback->yes;check(_)->no."
        );
        Add("guard-comprehension", "[{r,one}]=[X||X<-[{r},{r,one},{other,one}],is_record(X,r)]", "-record(r,{field}).");
        Add(
            "positive-evaluation-order",
            "true=erlang:is_record(begin self()!first,{r,one} end,begin self()!second,r end,begin self()!third,2 end),first=receive A->A end,second=receive B->B end,third=receive C->C end"
        );
        foreach (string size in new[] { "0", "-1", "-576460752303423489" })
            Add(
                "literal-suppresses-arguments-" + size,
                "false=erlang:is_record(error(should_not_execute),error(should_not_execute)," + size + "),false=is_record(error(should_not_execute),42," + size + ")"
            );
        int index = 0;
        foreach (string value in new[] { "[]", "42", "r", "1.0", "#{r=>one}", "<<1>>", "self()", "make_ref()", "fun()->ok end" })
            Add("non-tuple-" + index++, "false=apply(erlang,is_record,[" + value + ",r]),false=apply(erlang,is_record,[" + value + ",r,2])");
        index = 0;
        foreach (string tag in new[] { "42", "[]", "1.0", "{r}" })
        {
            Add("bad-tag-two-" + index, "ok=try apply(erlang,is_record,[{r}," + tag + "]) of _->unexpected catch error:badarg->ok end");
            Add("bad-tag-three-" + index++, "ok=try apply(erlang,is_record,[{r}," + tag + ",1]) of _->unexpected catch error:badarg->ok end");
        }
        index = 0;
        foreach (string size in new[] { "[]", "1.0", "{2}", "576460752303423488", "-576460752303423489" })
            Add(
                "bad-dynamic-size-" + index++,
                "ok=try apply(erlang,is_record,[{r},r," + size + "]) of _->unexpected catch error:badarg->ok end"
            );
        index = 0;
        foreach (string size in new[] { "0", "-1", "576460752303423487", "-576460752303423488" })
            Add("valid-dynamic-size-false-" + index++, "false=apply(erlang,is_record,[{r},r," + size + "])");
        Add(
            "native-module-name-ordinary-terms",
            "false=apply(erlang,is_record,[{r,one},record_predicate_contract,r]),false=apply(erlang,is_record,[{r},r,record_predicate_contract])"
        );

        return cases;
    }
}
