namespace Erlang.Differential;

public static class PortPacketBoundaryCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            "erlang-port-packet-boundary-" + name,
            "-module(port_packet_boundary). -export([run/0]). run()->" + body + ",done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        foreach ((int width, int maximum) in new[] { (1, 255), (2, 65535) })
        {
            string open = "P=open_port({spawn,\"dotnet tools/Native/Erlang.PortProbe/bin/Debug/net10.0/Erlang.PortProbe.dll\"},[binary,{packet," + width + "}])";
            Add(
                "maximum-" + width,
                open + ",B = <<0:" + maximum * 8 + ">>,true=erlang:port_command(P,B),receive {P,{data,B}}->ok after 5000->error(no_maximum_echo) end,true=erlang:port_close(P)"
            );
            foreach (bool options in new[] { false, true })
                Add(
                    "too-large-" + width + (options ? "-three" : "-two"),
                    "process_flag(trap_exit,true)," + open + ",R=monitor(port,P),true=erlang:port_command(P,<<0:" + (maximum + 1) * 8 + ">>" + (options ? ",[]" : string.Empty) + "),receive {'EXIT',P,einval}->ok after 5000->error(no_invalid_exit) end,receive {'DOWN',R,port,P,einval}->ok after 5000->error(no_invalid_down) end,undefined=erlang:port_info(P)"
                );
            Add(
                "protocol-too-large-" + width,
                "process_flag(trap_exit,true)," + open + ",R=monitor(port,P),P!{self(),{command,<<0:" + (maximum + 1) * 8 + ">>}},receive {'EXIT',P,einval}->ok after 5000->error(no_invalid_exit) end,receive {'DOWN',R,port,P,einval}->ok after 5000->error(no_invalid_down) end"
            );
            Add(
                "empty-" + width,
                open + ",true=erlang:port_command(P,<<>>),receive {P,{data,<<>>}}->ok after 5000->error(no_empty_packet) end,true=erlang:port_close(P)"
            );
            Add(
                "invalid-iodata-preserves-" + width,
                open + ",try erlang:port_command(P,[256]) of _->error(accepted) catch error:badarg->ok end,{connected,S}=erlang:port_info(P,connected),S=self(),true=erlang:port_command(P,<<1>>),receive {P,{data,<<1>>}}->ok after 5000->error(no_valid_packet) end,true=erlang:port_close(P)"
            );
        }

        return cases;
    }
}
