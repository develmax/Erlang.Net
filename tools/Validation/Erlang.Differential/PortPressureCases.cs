namespace Erlang.Differential;

public static class PortPressureCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            "erlang-port-pressure-" + name,
            "-module(port_pressure). -export([run/0]). run()->" + body + ",done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        const string probe = "{spawn,\"dotnet tools/Native/Erlang.PortProbe/bin/Debug/net10.0/Erlang.PortProbe.dll\"}";
        foreach (string setting in new[] { "{busy_limits_port,{1,1}}", "{busy_limits_port,{8192,4096}}", "{busy_limits_port,disabled}", "{parallelism,true}", "{parallelism,false}" })
            Add(
                "setting-" + cases.Count,
                "P=open_port(" + probe + ",[binary," + setting + "]),true=erlang:port_command(P,<<1>>),receive {P,{data,<<1>>}}->ok after 5000->error(no_echo) end,{queue_size,0}=erlang:port_info(P,queue_size),true=erlang:port_close(P)"
            );
        foreach (string value in new[] { "{0,1}", "{1,0}", "{-1,1}", "{1,-1}", "{1.0,2}", "{1,2.0}", "{1}", "enabled", "{18446744073709551615,18446744073709551615}", "{1,18446744073709551616}" })
            Add(
                "invalid-watermark-" + cases.Count,
                "try open_port(" + probe + ",[{busy_limits_port," + value + "}]) of _->error(accepted) catch error:badarg->ok end"
            );
        Add(
            "invalid-parallelism",
            "try open_port(" + probe + ",[{parallelism,enabled}]) of _->error(accepted) catch error:badarg->ok end"
        );
        foreach (string parallelism in new[] { "true", "false" })
            Add(
                "suspend-resume-" + parallelism,
                "P=open_port({spawn,\"dotnet tools/Native/Erlang.PortProbe/bin/Debug/net10.0/Erlang.PortProbe.dll slow-echo\"},[binary,{packet,4},{busy_limits_port,{1,256}},{parallelism," + parallelism + "}]),receive {P,{data,<<1>>}}->ok after 5000->error(no_ready) end,B = <<0:16777216>>,true=erlang:port_command(P,B),false=erlang:port_command(P,<<2>>,[nosuspend]),{queue_size,Q}=erlang:port_info(P,queue_size),true=Q>0,true=erlang:port_command(P,<<3>>),receive {P,{data,B}}->ok after 10000->error(no_large_echo) end,receive {P,{data,<<3>>}}->ok after 5000->error(no_resumed_echo) end,{queue_size,0}=erlang:port_info(P,queue_size),true=erlang:port_close(P)"
            );
        Add(
            "default-line",
            "P=open_port({spawn,\"dotnet tools/Native/Erlang.PortProbe/bin/Debug/net10.0/Erlang.PortProbe.dll lines\"},[binary,line,eof]),receive {P,{data,{eol,<<97,98,99>>}}}->ok after 5000->error(no_line) end,true=erlang:port_close(P)"
        );

        return cases;
    }
}
