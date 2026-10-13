namespace Erlang.Differential;

public static class SystemProfileCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    public static string? FixtureName(string function, int arity) => function == SystemProfileNames.Function ? arity switch
    {
        0 => "erlang-system-profile-query",
        2 => "erlang-system-profile-timestamp-only",
        _ => null
    } : null;

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            "erlang-system-profile-" + name,
            "-module(system_profile_contract). -export([run/0]). run()->" + body + ",done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        Add("query", "undefined=erlang:system_profile()");
        Add("clear-undefined", "undefined=erlang:system_profile(undefined,anything),undefined=erlang:system_profile()");
        Add(
            "empty-options-clear",
            "S=self(),undefined=erlang:system_profile(S,[timestamp]),{S,[]}=erlang:system_profile(wrong,[]),undefined=erlang:system_profile()"
        );
        Add(
            "undefined-discards-improper-options",
            "S=self(),undefined=erlang:system_profile(S,[timestamp]),{S,[]}=erlang:system_profile(undefined,[wrong|tail]),undefined=erlang:system_profile()"
        );
        Add(
            "timestamp-only",
            "S=self(),undefined=erlang:system_profile(S,[timestamp]),{S,[]}=erlang:system_profile(),{S,[]}=erlang:system_profile(undefined,[])"
        );
        foreach (string clock in new[] { "monotonic_timestamp", "strict_monotonic_timestamp" })
            Add(
                clock,
                "S=self(),undefined=erlang:system_profile(S,[" + clock + "]),{S,[]}=erlang:system_profile(),{S,[]}=erlang:system_profile(undefined,[])"
            );
        Add(
            "timestamp-duplicates",
            "S=self(),undefined=erlang:system_profile(S,[timestamp,monotonic_timestamp,strict_monotonic_timestamp,timestamp]),{S,[]}=erlang:system_profile(),{S,[]}=erlang:system_profile(undefined,[])"
        );
        Add(
            "replace-receiver",
            "S=self(),P=spawn(fun()->receive stop->ok end end),undefined=erlang:system_profile(S,[timestamp]),{S,[]}=erlang:system_profile(P,[monotonic_timestamp]),{P,[]}=erlang:system_profile(),{P,[]}=erlang:system_profile(undefined,[]),P!stop"
        );
        Add(
            "owner-death",
            "P=spawn(fun()->receive stop->ok end end),R=monitor(process,P),undefined=erlang:system_profile(P,[timestamp]),P!stop,receive {'DOWN',R,process,P,normal}->ok after 5000->error(no_down) end,undefined=erlang:system_profile()"
        );
        Add(
            "dead-receiver",
            "P=spawn(fun()->ok end),R=monitor(process,P),receive {'DOWN',R,process,P,normal}->ok end,try erlang:system_profile(P,[timestamp]) of _->error(accepted) catch error:badarg->ok end,undefined=erlang:system_profile()"
        );
        foreach (string target in new[] { "wrong", "0", "[]", "{name,node()}", "make_ref()" })
            Add(
                "invalid-receiver-" + cases.Count,
                "try erlang:system_profile(" + target + ",[timestamp]) of _->error(accepted) catch error:badarg->ok end,undefined=erlang:system_profile()"
            );
        foreach (string options in new[] { "wrong", "0", "[wrong]", "[{timestamp,true}]", "[timestamp|tail]", "[timestamp,wrong]", "[1]" })
            Add(
                "invalid-options-preserve-" + cases.Count,
                "S=self(),undefined=erlang:system_profile(S,[timestamp]),try erlang:system_profile(S," + options + ") of _->error(accepted) catch error:badarg->ok end,{S,[]}=erlang:system_profile(),{S,[]}=erlang:system_profile(undefined,[])"
            );

        return cases;
    }
}
