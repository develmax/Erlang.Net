namespace Erlang.Differential;

public static class ErlangRaiseCases
{
    private static CompiledModuleCase Case(string name, string body) => new(
        "erlang-raise-" + name,
        "-module(oracle_erlang_raise). -export([run/0]). run()->" + body + ",done.",
        Term.Tuple(Term.A("ok"), Term.A("done"))
    );

    public static string? FixtureName(string function, int arity) => function == "raise" && arity == 3 ? "erlang-raise-export-raise-3" : null;

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();
        cases.Add(Case(
            "uncaught-error-monitor",
            "T=[{m,f,0,[]}],{P,M}=spawn_monitor(fun()->erlang:raise(error,reason,T) end),receive {'DOWN',M,process,P,{reason,T}}->ok after 1000->error(missing) end"
        ));
        cases.Add(Case(
            "uncaught-throw-monitor",
            "T=[{m,f,0,[]}],{P,M}=spawn_monitor(fun()->erlang:raise(throw,reason,T) end),receive {'DOWN',M,process,P,{{nocatch,reason},T}}->ok after 1000->error(missing) end"
        ));
        cases.Add(Case(
            "uncaught-exit-monitor",
            "T=[{m,f,0,[]}],{P,M}=spawn_monitor(fun()->erlang:raise(exit,reason,T) end),receive {'DOWN',M,process,P,reason}->ok after 1000->error(missing) end"
        ));
        cases.Add(Case("export-raise-3", "try erlang:raise(error,reason,[]) of _->error(unexpected) catch error:reason:S->[]=S end"));
        foreach (string exceptionClass in new[] { "error", "exit", "throw" })
        {
            cases.Add(Case(
                "class-" + exceptionClass,
                "T=[{m,f,0,[]}],try erlang:raise(" + exceptionClass + ",{reason,42},T) of _->error(unexpected) catch " + exceptionClass + ":{reason,42}:S->T=S end"
            ));
            cases.Add(Case(
                "old-frame-" + exceptionClass,
                "try erlang:raise(" + exceptionClass + ",reason,[{m,f,0}]) of _->error(unexpected) catch " + exceptionClass + ":reason:S->[{m,f,0,[]}]=S end"
            ));
        }
        foreach (string arguments in new[] { "0", "-1", "1000000000000000000000000000000000000000", "bad", "[]", "[1,2]", "[1|bad]", "{}", "1.0" })
            cases.Add(Case(
                "opaque-frame-arguments-" + cases.Count,
                "A=" + arguments + ",try erlang:raise(error,reason,[{m,f,A}]) of _->error(unexpected) catch error:reason:S->[{m,f,A,[]}]=S end"
            ));
        foreach (string location in new[] { "[]", "[bad]", "[bad|tail]", "[{line,42}]", "[{error_info,#{module=>custom}}]" })
            cases.Add(Case(
                "opaque-location-" + cases.Count,
                "T=[{m,f,0," + location + "}],try erlang:raise(error,reason,T) of _->error(unexpected) catch error:reason:S->T=S end"
            ));
        foreach (string exceptionClass in new[] { "bad", "true", "[]", "1", "1.0", "{error}" })
            cases.Add(Case("invalid-class-" + cases.Count, "badarg=erlang:raise(" + exceptionClass + ",reason,[])"));
        foreach (string stack in new[] { "bad", "{}", "[bad]", "[{m}]", "[{m,f}]", "[{1,f,0}]", "[{m,1,0}]", "[{m,f,0,bad}]", "[{m,f,0,{},extra}]", "[{m,f,0}|bad]" })
            cases.Add(Case("invalid-stack-" + cases.Count, "badarg=erlang:raise(error,reason," + stack + ")"));
        cases.Add(Case(
            "fun-two-frame",
            "F=fun()->ok end,try erlang:raise(error,reason,[{F,[]}]) of _->error(unexpected) catch error:reason:S->[{F,[],[]}]=S end"
        ));
        cases.Add(Case(
            "fun-three-frame",
            "F=fun()->ok end,T=[{F,bad,[bad|tail]}],try erlang:raise(error,reason,T) of _->error(unexpected) catch error:reason:S->T=S end"
        ));
        cases.Add(Case("fun-invalid-location", "F=fun()->ok end,badarg=erlang:raise(error,reason,[{F,[],bad}])"));
        cases.Add(Case(
            "default-depth",
            "T=lists:duplicate(20,{m,f,0,[]}),try erlang:raise(error,reason,T) of _->error(unexpected) catch error:reason:S->8=length(S),true=lists:all(fun(X)->X=:={m,f,0,[]} end,S) end"
        ));
        cases.Add(Case("invalid-after-depth", "T=lists:append(lists:duplicate(20,{m,f,0,[]}),[bad]),badarg=erlang:raise(error,reason,T)"));
        cases.Add(Case(
            "nested-rethrow",
            "T=[{m,f,[42],[]}],try try erlang:raise(error,first,T) catch error:first:S->erlang:raise(throw,second,S) end catch throw:second:Outer->T=Outer end"
        ));

        return cases;
    }
}
