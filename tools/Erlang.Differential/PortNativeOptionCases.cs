namespace Erlang.Differential;

public static class PortNativeOptionCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            "erlang-port-native-option-" + name,
            "-module(port_native_option). -export([run/0]). run()->" + body + ",done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        const string executable = "{spawn_executable,\"tools/Erlang.PortProbe/bin/Debug/net10.0/Erlang.PortProbe\"}";
        const string shell = "{spawn,\"dotnet tools/Erlang.PortProbe/bin/Debug/net10.0/Erlang.PortProbe.dll\"}";
        foreach (string options in new[] { "in,out", "out,in", "in,in,out", "overlapped_io", "overlapped_io,in,out" })
            Add(
                "echo-" + cases.Count,
                "P=open_port(" + shell + ",[binary," + options + "]),true=erlang:port_command(P,<<1>>),receive {P,{data,<<1>>}}->ok after 5000->error(no_echo) end,true=erlang:port_close(P)"
            );
        foreach (string settings in new[] { "stderr_to_stdout", "stderr_to_stdout,overlapped_io", "nouse_stdio,use_stdio,stderr_to_stdout" })
            Add(
                "shared-streams-" + cases.Count,
                "P=open_port(" + executable + ",[binary,{packet,1},{args,[\"streams\"]}," + settings + "]),receive {P,{data,<<11>>}}->ok after 5000->error(no_stdout_first) end,receive {P,{data,<<12>>}}->ok after 5000->error(no_stderr_first) end,receive {P,{data,<<13>>}}->ok after 5000->error(no_stdout_second) end,receive {P,{data,<<14>>}}->ok after 5000->error(no_stderr_second) end"
            );
        Add(
            "shell-shared-streams",
            "P=open_port({spawn,\"dotnet tools/Erlang.PortProbe/bin/Debug/net10.0/Erlang.PortProbe.dll streams\"},[binary,{packet,1},stderr_to_stdout]),receive {P,{data,<<11>>}}->ok after 5000->error(no_stdout_first) end,receive {P,{data,<<12>>}}->ok after 5000->error(no_stderr_first) end,receive {P,{data,<<13>>}}->ok after 5000->error(no_stdout_second) end,receive {P,{data,<<14>>}}->ok after 5000->error(no_stderr_second) end"
        );
        foreach (string value in new[] { "custom-name", "custom name", "" })
            Add(
                "arg0-" + cases.Count,
                "P=open_port(" + executable + ",[binary,{packet,1},{arg0,\"" + value + "\"},{args,[\"argv0\"]}]),receive {P,{data,B}}->B=list_to_binary(\"" + value + "\") after 5000->error(no_arg0) end"
            );
        Add(
            "arg0-preserved-by-args",
            "P=open_port(" + executable + ",[binary,{packet,1},{args,[]},{arg0,\"first\"},{args,[\"argv0\"]},{arg0,\"last\"}]),receive {P,{data,<<108,97,115,116>>}}->ok after 5000->error(wrong_arg0) end"
        );
        foreach (string value in new[] { "42", "[0]", "[1|tail]" })
            Add(
                "invalid-arg0-" + cases.Count,
                "try open_port(" + executable + ",[{arg0," + value + "}]) of _->error(accepted) catch error:badarg->ok end"
            );
        Add("arg0-rejects-shell", "try open_port(" + shell + ",[{arg0,\"custom\"}]) of _->error(accepted) catch error:badarg->ok end");
        foreach (string settings in new[] { "nouse_stdio,stderr_to_stdout", "stderr_to_stdout,nouse_stdio" })
            Add(
                "stdio-conflict-" + cases.Count,
                "try open_port(" + shell + ",[" + settings + "]) of _->error(accepted) catch error:badarg->ok end"
            );
        foreach (string settings in new[] { "nouse_stdio", "use_stdio,nouse_stdio", "nouse_stdio,in,out" })
            Add(
                "auxiliary-" + cases.Count,
                "P=open_port(" + executable + ",[binary,{packet,1},{args,[\"auxiliary\"]}," + settings + "]),true=erlang:port_command(P,<<1>>),receive {P,{data,<<1>>}}->ok after 5000->error(no_aux_echo) end,true=erlang:port_close(P)"
            );
        Add(
            "input-only-eof",
            "P=open_port(" + executable + ",[binary,in,{packet,1},{args,[\"input-eof\"]}]),receive {P,{data,<<17>>}}->ok after 5000->error(stdin_not_closed) end"
        );
        Add(
            "input-only-output-fails",
            "process_flag(trap_exit,true),P=open_port({spawn,\"dotnet tools/Erlang.PortProbe/bin/Debug/net10.0/Erlang.PortProbe.dll slow-echo\"},[binary,in,{packet,4}]),receive {P,{data,<<1>>}}->ok after 5000->error(no_ready) end,R=monitor(port,P),true=erlang:port_command(P,<<2>>),receive {'EXIT',P,ebadf}->ok after 5000->error(no_badfd_exit) end,receive {'DOWN',R,port,P,ebadf}->ok after 5000->error(no_badfd_down) end"
        );
        Add(
            "output-only-no-data",
            "P=open_port({spawn,\"dotnet tools/Erlang.PortProbe/bin/Debug/net10.0/Erlang.PortProbe.dll emit\"},[binary,out,eof]),receive {P,{data,_}}->error(unexpected_data);{P,eof}->error(unexpected_eof) after 200->ok end,{connected,S}=erlang:port_info(P,connected),S=self(),true=erlang:port_close(P)"
        );

        return cases;
    }
}
