namespace Erlang.Differential;

public static class PortCases
{
    public static string? FixtureName(string function, int arity) => function switch
    {
        PortNames.Open or PortNames.Ports or PortNames.Close => "erlang-port-lifecycle",
        PortNames.Command => arity == 2 ? "erlang-port-binary-echo" : "erlang-port-command-flags",
        PortNames.Connect => "erlang-port-reconnect",
        PortNames.GetData or PortNames.SetData => "erlang-port-data",
        PortNames.Info => "erlang-port-info",
        PortNames.Call or PortNames.Control => "erlang-port-driver-callbacks",
        _ => null
    };

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();
        const string echo = "P=open_port({spawn,\"dotnet tools/Erlang.PortProbe/bin/Debug/net10.0/Erlang.PortProbe.dll\"},[binary])";

        void Add(string name, string body) => cases.Add(new(
            "erlang-port-" + name,
            "-module(port_contract). -export([run/0]). run()->" + body + ",done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        Add(
            "lifecycle",
            "process_flag(trap_exit,true)," + echo + ",true=is_port(P),true=lists:member(P,erlang:ports()),{links,L}=process_info(self(),links),true=lists:member(P,L),true=erlang:port_close(P),receive {'EXIT',P,normal}->ok after 5000->error(no_exit) end,undefined=erlang:port_info(P),false=lists:member(P,erlang:ports())"
        );
        Add(
            "binary-echo",
            echo + ",true=erlang:port_command(P,[<<1,2>>,3,[4]]),receive {P,{data,<<1,2,3,4>>}}->ok after 5000->error(no_data) end,true=erlang:port_close(P)"
        );
        Add(
            "command-flags",
            echo + ",true=erlang:port_command(P,<<5>>,[nosuspend]),receive {P,{data,<<5>>}}->ok after 5000->error(no_data) end,try erlang:port_command(P,<<>>,[wrong]) of _->error(accepted) catch error:badarg->ok end,try erlang:port_command(P,<<>>,[force]) of _->error(accepted) catch error:notsup->ok end,true=erlang:port_close(P)"
        );
        Add(
            "data",
            echo + ",undefined=erlang:port_get_data(P),D=#{payload=>[1,<<2>>,self()]},true=erlang:port_set_data(P,D),D=erlang:port_get_data(P),true=erlang:port_close(P),try erlang:port_get_data(P) of _->error(accepted) catch error:badarg->ok end"
        );
        Add(
            "info",
            echo + ",{connected,S}=erlang:port_info(P,connected),S=self(),{links,[S]}=erlang:port_info(P,links),{input,0}=erlang:port_info(P,input),{output,0}=erlang:port_info(P,output),{os_pid,N}=erlang:port_info(P,os_pid),true=is_integer(N),true=N>0,[]=erlang:port_info(P,registered_name),I=erlang:port_info(P),[name,links,id,connected,input,output,os_pid]=[K||{K,_}<-I],true=erlang:port_close(P)"
        );
        Add(
            "driver-callbacks",
            echo + ",try erlang:port_call(P,payload) of _->error(accepted) catch error:badarg->ok end,try erlang:port_call(P,0,payload) of _->error(accepted) catch error:badarg->ok end,try erlang:port_control(P,0,<<>>) of _->error(accepted) catch error:badarg->ok end,true=erlang:port_close(P)"
        );
        Add(
            "registered",
            echo + ",true=register(port_contract_name,P),P=whereis(port_contract_name),{registered_name,port_contract_name}=erlang:port_info(P,registered_name),true=erlang:port_set_data(port_contract_name,value),value=erlang:port_get_data(port_contract_name),true=erlang:port_close(port_contract_name),undefined=whereis(port_contract_name)"
        );
        Add(
            "reconnect",
            echo + ",Root=self(),C=spawn(fun()->receive {port,Q}->true=erlang:port_command(Q,<<8>>),receive {Q,{data,<<8>>}}->Root!observed after 5000->Root!missing end,receive stop->ok end end end),true=erlang:port_connect(P,C),{connected,C}=erlang:port_info(P,connected),{links,L}=erlang:port_info(P,links),true=lists:member(Root,L),true=lists:member(C,L),C!{port,P},receive observed->ok after 5000->error(no_reconnect) end,true=erlang:port_close(P),C!stop"
        );
        foreach (int width in new[] { 1, 2, 4 })
            Add(
                "packet-" + width,
                "P=open_port({spawn,\"dotnet tools/Erlang.PortProbe/bin/Debug/net10.0/Erlang.PortProbe.dll\"},[binary,{packet," + width + "}]),true=erlang:port_command(P,<<1,2,3>>),receive {P,{data,<<1,2,3>>}}->ok after 5000->error(no_packet) end,true=erlang:port_close(P)"
            );
        Add(
            "list-echo",
            "P=open_port({spawn,\"dotnet tools/Erlang.PortProbe/bin/Debug/net10.0/Erlang.PortProbe.dll\"},[]),true=erlang:port_command(P,[1,2,3]),receive {P,{data,[1,2,3]}}->ok after 5000->error(no_list) end,true=erlang:port_close(P)"
        );
        Add(
            "exit-status",
            "process_flag(trap_exit,true),P=open_port({spawn,\"dotnet tools/Erlang.PortProbe/bin/Debug/net10.0/Erlang.PortProbe.dll emit\"},[binary,exit_status]),receive {P,{data,<<\"port-probe\">>}}->ok after 5000->error(no_output) end,receive {P,{exit_status,7}}->ok after 5000->error(no_status) end,receive {'EXIT',P,normal}->ok after 5000->error(no_exit) end"
        );
        Add(
            "eof",
            "P=open_port({spawn,\"dotnet tools/Erlang.PortProbe/bin/Debug/net10.0/Erlang.PortProbe.dll emit\"},[binary,eof,exit_status]),receive {P,{data,<<\"port-probe\">>}}->ok after 5000->error(no_output) end,receive {P,eof}->ok after 5000->error(no_eof) end,receive {P,{exit_status,7}}->ok after 5000->error(no_status) end,true=erlang:port_close(P)"
        );
        foreach (string settings in new[] { "atom", "[wrong]", "[{packet,0}]", "[{packet,3}]", "[{packet,5}]", "[binary|wrong]", "[{args,atom}]", "[{env,[wrong]}]" })
            Add(
                "bad-options-" + cases.Count,
                "try open_port({spawn,\"unused\"}," + settings + ") of _->error(accepted) catch error:badarg->ok end"
            );
        Add(
            "closed-errors",
            echo + ",true=erlang:port_close(P),undefined=erlang:port_info(P,wrong),try erlang:port_close(P) of _->error(accepted) catch error:badarg->ok end,try erlang:port_command(P,<<>>) of _->error(accepted) catch error:badarg->ok end,try erlang:port_set_data(P,term) of _->error(accepted) catch error:badarg->ok end"
        );
        Add(
            "protocol-command",
            echo + ",P!{self(),{command,<<9>>}},receive {P,{data,<<9>>}}->ok after 5000->error(no_protocol) end,P!{self(),close},receive {P,closed}->ok after 5000->error(no_closed) end"
        );
        Add(
            "protocol-badsig",
            "process_flag(trap_exit,true)," + echo + ",P!wrong,receive {'EXIT',P,badsig}->ok after 5000->error(no_badsig) end,{connected,S}=erlang:port_info(P,connected),S=self(),true=erlang:port_close(P)"
        );
        Add(
            "unlink-link",
            echo + ",true=unlink(P),{links,[]}=erlang:port_info(P,links),true=link(P),{links,[S]}=erlang:port_info(P,links),S=self(),true=erlang:port_close(P)"
        );
        Add(
            "name-uniqueness",
            echo + ",true=register(port_contract_name,P),try register(port_contract_name,self()) of _->error(accepted) catch error:badarg->ok end,try register(port_other_name,P) of _->error(accepted) catch error:badarg->ok end,true=unregister(port_contract_name),true=erlang:port_close(P)"
        );

        Add(
            "protocol-info-barrier",
            echo + ",P!{self(),{command,<<9>>}},{output,1}=erlang:port_info(P,output),true=erlang:port_close(P)"
        );

        return cases;
    }
}
