namespace Erlang.Differential;

public static class ErlangProcessQueryCases
{
    private static CompiledModuleCase Case(string name, string body) => new(
        "erlang-process-query-" + name,
        "-module(oracle_process_query). -export([run/0]). run()->" + body + ",done.",
        Term.Tuple(Term.A("ok"), Term.A("done"))
    );

    private static string Error(string call) => "try erlang:" + call + " of _->unexpected catch error:badarg->ok end";

    public static string? FixtureName(string function, int arity)
    {
        string name = "erlang-process-query-export-" + function + "-" + arity;

        return Modules.Any(fixture => fixture.Name == name) ? name : null;
    }

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();
        cases.Add(Case(
            "node-guard-qualified",
            "N=erlang:node(),case self() of P when erlang:node(P)=:=N,erlang:node()=:=N->ok;_->error(unexpected) end"
        ));
        cases.Add(Case("node-guard-unqualified", "N=node(),case self() of P when node(P)=:=N,node()=:=N->ok;_->error(unexpected) end"));
        cases.Add(Case("node-guard-failure", "case bad of X when node(X)=:=node()->error(unexpected);_->ok end"));
        cases.Add(Case("export-node-0", "'nonode@nohost'=erlang:node()"));
        cases.Add(Case("export-node-1", "N=erlang:node(),N=erlang:node(self()),N=erlang:node(erlang:make_ref())"));
        cases.Add(Case(
            "export-processes-0",
            "true=lists:member(self(),erlang:processes()),true=lists:all(fun(X)->is_pid(X) end,erlang:processes())"
        ));
        cases.Add(Case(
            "export-registered-0",
            "register(oracle_query_parent,self()),true=lists:member(oracle_query_parent,erlang:registered()),true=lists:all(fun(X)->is_atom(X) end,erlang:registered()),unregister(oracle_query_parent),false=lists:member(oracle_query_parent,erlang:registered())"
        ));
        cases.Add(Case("export-is_process_alive-1", "true=erlang:is_process_alive(self())"));
        cases.Add(Case("export-yield-0", "true=erlang:yield()"));
        cases.Add(Case(
            "export-bump_reductions-1",
            "true=erlang:bump_reductions(0),true=erlang:bump_reductions(1),true=erlang:bump_reductions(4000),true=erlang:bump_reductions(576460752303423487)"
        ));
        cases.Add(Case(
            "live-dead-child",
            "{P,M}=spawn_monitor(fun()->receive stop->ok end end),true=erlang:is_process_alive(P),true=lists:member(P,erlang:processes()),P!stop,receive {'DOWN',M,process,P,normal}->ok after 1000->error(missing) end,false=erlang:is_process_alive(P),false=lists:member(P,erlang:processes())"
        ));
        cases.Add(Case(
            "registered-child-cleanup",
            "Parent=self(),{P,M}=spawn_monitor(fun()->register(oracle_query_child,self()),Parent!registered,receive stop->ok end end),receive registered->ok after 1000->error(missing) end,true=lists:member(oracle_query_child,erlang:registered()),P!stop,receive {'DOWN',M,process,P,normal}->ok after 1000->error(missing) end,false=lists:member(oracle_query_child,erlang:registered())"
        ));
        cases.Add(Case(
            "node-child-identity",
            "Parent=self(),N=erlang:node(),P=spawn(fun()->Parent!{node,erlang:node(),erlang:node(self())} end),N=erlang:node(P),receive {node,N,N}->ok after 1000->error(missing) end"
        ));
        cases.Add(Case(
            "yield-progress",
            "Parent=self(),spawn(fun()->Parent!ready end),true=erlang:yield(),receive ready->ok after 1000->error(missing) end"
        ));
        cases.Add(Case(
            "bump-progress",
            "Parent=self(),spawn(fun()->Parent!ready end),true=erlang:bump_reductions(4000),receive ready->ok after 1000->error(missing) end"
        ));
        foreach (string value in new[] { "bad", "1", "1.0", "[]", "{}", "#{}", "fun()->ok end" })
            cases.Add(Case("node-type-error-" + cases.Count, Error("node(" + value + ")")));
        foreach (string value in new[] { "bad", "1", "[]", "erlang:make_ref()", "fun()->ok end" })
            cases.Add(Case("alive-type-error-" + cases.Count, Error("is_process_alive(" + value + ")")));
        foreach (string value in new[] { "-1", "576460752303423488", "100000000000000000000000000000000", "bad", "1.0", "[]", "{}" })
            cases.Add(Case("bump-range-error-" + cases.Count, Error("bump_reductions(" + value + ")")));

        return cases;
    }
}
