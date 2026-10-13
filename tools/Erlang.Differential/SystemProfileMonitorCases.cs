namespace Erlang.Differential;

public static class SystemProfileMonitorCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } =
    [
        new(
            "erlang-system-profile-monitor-independent-controls",
            "-module(profile_monitor_joint). -export([run/0]). run()->S=self(),O=[{long_message_queue,{1,100}}],undefined=erlang:system_monitor(S,O),undefined=erlang:system_profile(S,[timestamp]),{S,[]}=erlang:system_profile(undefined,anything),{S,O}=erlang:system_monitor(),undefined=erlang:system_profile(S,[monotonic_timestamp]),{S,O}=erlang:system_monitor(undefined),{S,[]}=erlang:system_profile(),{S,[]}=erlang:system_profile(undefined,[]),done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "erlang-system-profile-monitor-common-owner-death",
            "-module(profile_monitor_death_joint). -export([run/0]). run()->P=spawn(fun()->receive stop->ok end end),R=monitor(process,P),undefined=erlang:system_monitor(P,[{long_message_queue,{100,200}}]),undefined=erlang:system_profile(P,[strict_monotonic_timestamp]),P!stop,receive {'DOWN',R,process,P,normal}->ok after 5000->error(no_down) end,undefined=erlang:system_monitor(),undefined=erlang:system_profile(),done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        )
    ];
}
