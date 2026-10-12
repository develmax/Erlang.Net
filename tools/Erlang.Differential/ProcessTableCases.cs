namespace Erlang.Differential;

public static class ProcessTableCases
{
    public static string? FixtureName(string function, int arity) => (function, arity) switch
    {
        (ProcessTableContractNames.Iterator, 0) => ProcessTableContractNames.InitialFixture,
        (ProcessTableContractNames.Next, 1) => ProcessTableContractNames.WalkFixture,
        _ => null
    };

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            "erlang-process-table-" + name,
            "-module(process_table_contract). -export([run/0]). run()->" + body + ",done. collect(I,A)->case erlang:processes_next(I) of none->A;{P,N}->collect(N,[P|A]) end.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));

        Add("initial", "{0,[]}=erlang:processes_iterator()");
        Add(
            "walk",
            "L=collect(erlang:processes_iterator(),[]),true=lists:member(self(),L),true=lists:all(fun(P)->is_pid(P) end,L),true=(length(L)=:=length(lists:usort(L)))"
        );
        Add(
            "independent",
            "I1=erlang:processes_iterator(),I2=erlang:processes_iterator(),true=lists:member(self(),collect(I1,[])),true=lists:member(self(),collect(I2,[]))"
        );
        Add("end", "none=erlang:processes_next({1048576,[]})");
        Add("empty-windows", "L=collect({1048575,[]},[]),true=lists:all(fun(P)->is_pid(P) end,L)");
        Add("buffered", "{not_a_pid,{arbitrary,[self_pid]}}=erlang:processes_next({arbitrary,[not_a_pid,self_pid]})");
        Add("improper-buffer", "{head,{-1,tail}}=erlang:processes_next({-1,[head|tail]})");
        Add("buffered-large-cursor", "{head,{576460752303423488,[]}}=erlang:processes_next({576460752303423488,[head]})");
        Add("buffered-float-cursor", "{head,{1.5,[]}}=erlang:processes_next({1.5,[head]})");
        Add("buffered-reference-cursor", "R=make_ref(),{head,{R,[]}}=erlang:processes_next({R,[head]})");
        Add(
            "live-child",
            "{P,R}=spawn_monitor(fun()->receive finish->ok end end),L=collect(erlang:processes_iterator(),[]),true=lists:member(P,L),true=lists:member(self(),L),P!finish,receive {'DOWN',R,process,P,normal}->ok after 1000->error(missing) end"
        );
        Add(
            "dead-buffer",
            "{P,R}=spawn_monitor(fun()->ok end),receive {'DOWN',R,process,P,normal}->ok after 1000->error(missing) end,{P,{ignored,[]}}=erlang:processes_next({ignored,[P]})"
        );
        int index = 0;
        foreach (string value in new[] { "none", "[]", "0", "{}", "{0}", "{0,[],extra}", "{0,none}", "{0,<<>>}", "{-1,[]}", "{1048577,[]}", "{576460752303423488,[]}", "{1.5,[]}", "{make_ref(),[]}", "{self(),[]}", "{[],[]}", "{none,[]}" })
            Add("bad-type-" + index++, "ok=try erlang:processes_next(" + value + ") of _->unexpected catch error:badarg->ok end");

        return cases;
    }
}
