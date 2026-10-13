namespace Erlang.Differential;

public static class PortMessageCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            "erlang-port-message-pressure-" + name,
            "-module(port_message_pressure). -export([run/0]). run()->" + body + ",done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        const string probe = "{spawn,\"dotnet tools/Erlang.PortProbe/bin/Debug/net10.0/Erlang.PortProbe.dll\"}";
        foreach (string value in new[] { "{1,1}", "{8192,4096}", "disabled" })
            Add(
                "option-" + cases.Count,
                "P=open_port(" + probe + ",[binary,{busy_limits_msgq," + value + "}]),M={self(),{command,<<1>>}},M=erlang:send(P,M),receive {P,{data,<<1>>}}->ok after 5000->error(no_echo) end,true=erlang:port_close(P)"
            );
        foreach (string value in new[] { "{0,1}", "{1,0}", "{-1,1}", "{1,-1}", "{1.0,2}", "{1,2.0}", "{1}", "enabled", "{1,18446744073709551615}" })
            Add(
                "invalid-option-" + cases.Count,
                "try open_port(" + probe + ",[{busy_limits_msgq," + value + "}]) of _->error(accepted) catch error:badarg->ok end"
            );
        foreach (string decline in new[] { "nosuspend=erlang:send(P,M,[nosuspend])", "false=erlang:send_nosuspend(P,M)", "false=erlang:send_nosuspend(P,M,[])" })
            Add(
                "decline-" + cases.Count,
                "P=open_port({spawn,\"dotnet tools/Erlang.PortProbe/bin/Debug/net10.0/Erlang.PortProbe.dll slow-echo\"},[binary,{packet,4},{busy_limits_port,{1,256}},{busy_limits_msgq,{1,256}},{parallelism,true}]),receive {P,{data,<<1>>}}->ok after 5000->error(no_ready) end,B = <<0:16777216>>,true=erlang:port_command(P,B),M={self(),{command,<<2>>}}," + decline + ",N={self(),{command,<<3>>}},N=P!N,receive {P,{data,B}}->ok after 10000->error(no_large_echo) end,receive {P,{data,<<3>>}}->ok after 5000->error(no_resumed_echo) end,receive {P,{data,<<2>>}}->error(declined_sent) after 0->ok end,true=erlang:port_close(P)"
            );
        foreach (string send in new[] { "M=P!M", "M=erlang:send(P,M)", "ok=erlang:send(P,M,[])" })
            Add(
                "large-message-" + cases.Count,
                "P=open_port(" + probe + ",[binary,{packet,4},{busy_limits_msgq,{1,1}},{parallelism,true}]),B = <<0:65536>>,M={self(),{command,B}}," + send + ",receive {P,{data,B}}->ok after 5000->error(no_echo) end,true=erlang:port_close(P)"
            );

        return cases;
    }
}
