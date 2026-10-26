namespace Erlang.Differential;

public static class GarbageCollectionResultCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            GarbageCollectionResultCaseNames.Prefix + name,
            GarbageCollectionResultCaseNames.ModulePrefix + body + ",done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        Add(
            "integers",
            "R=make_ref(),N=erlang:system_info(schedulers),lists:foreach(fun(I)->self()!{R,{I,7,11}} end,lists:seq(1,N)),E={7*N,11*N,0},E=erlang:gather_gc_info_result(R)"
        );
        Add(
            "selective-order-exact-count",
            "R=make_ref(),Other=make_ref(),self()!marker,self()!{Other,{ignored,8,9}},self()!{R,{ignored,1}},lists:foreach(fun(I)->self()!{R,{I,2,3}} end,lists:seq(1,erlang:system_info(schedulers))),self()!{R,{extra,99,99}},N=erlang:system_info(schedulers),E={2*N,3*N,0},E=erlang:gather_gc_info_result(R),marker=receive X1->X1 end,{Other,{ignored,8,9}}=receive X2->X2 end,{R,{ignored,1}}=receive X3->X3 end,{R,{extra,99,99}}=receive X4->X4 end"
        );
        Add(
            "floating-sums",
            "R=make_ref(),N=erlang:system_info(schedulers),lists:foreach(fun(I)->self()!{R,{I,0.5,1.25}} end,lists:seq(1,N)),E={N*0.5,N*1.25,0},E=erlang:gather_gc_info_result(R)"
        );
        Add(
            "bignum-signed-sums",
            "R=make_ref(),N=erlang:system_info(schedulers),B=1 bsl 100,lists:foreach(fun(I)->self()!{R,{I,B,-B}} end,lists:seq(1,N)),E={B*N,-B*N,0},E=erlang:gather_gc_info_result(R)"
        );
        Add(
            "invalid-reference-function-clause",
            "lists:foreach(fun(V)->ok=try erlang:gather_gc_info_result(V) of _->unexpected catch error:function_clause->ok end end,[0,ok,{},[],self(),<<>>,fun()->ok end])"
        );
        Add(
            "invalid-collections-consumed",
            "R=make_ref(),self()!{R,{ignored,invalid_number,1}},self()!{R,{keep,2,3}},ok=try erlang:gather_gc_info_result(R) of _->unexpected catch error:badarith->ok end,{R,{keep,2,3}}=receive V->V after 0->missing end"
        );
        Add(
            "invalid-reclaimed-consumed",
            "R=make_ref(),self()!{R,{ignored,1,invalid_number}},self()!{R,{keep,2,3}},ok=try erlang:gather_gc_info_result(R) of _->unexpected catch error:badarith->ok end,{R,{keep,2,3}}=receive V->V after 0->missing end"
        );
        Add(
            "floating-overflow-domain",
            "R=make_ref(),N=erlang:system_info(schedulers),lists:foreach(fun(I)->self()!{R,{I,1.0e308,0}} end,lists:seq(1,N)),case N of 1->{1.0e308,0,0}=erlang:gather_gc_info_result(R);_->ok=try erlang:gather_gc_info_result(R) of _->unexpected catch error:badarith->ok end end"
        );

        return cases;
    }
}
