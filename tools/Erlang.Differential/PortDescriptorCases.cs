namespace Erlang.Differential;

public static class PortDescriptorCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            "erlang-port-fd-" + name,
            "-module(port_descriptor). -export([run/0]). run()->" + body + ",done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        foreach (string specification in new[] { "{fd,1}", "{fd,1,2,3}", "{fd,wrong,1}", "{fd,0,wrong}", "{fd,0.0,1}", "{fd,0,1.0}", "{fd,18446744073709551616,1}" })
            Add(
                "invalid-specification-" + cases.Count,
                "try open_port(" + specification + ",[]) of _->error(accepted) catch error:badarg->ok end"
            );
        foreach (string setting in new[] { "{args,[]}", "{arg0,\"custom\"}" })
            Add(
                "spawn-only-argument-" + cases.Count,
                "try open_port({fd,0,1},[out," + setting + "]) of _->error(accepted) catch error:badarg->ok end"
            );
        foreach (string settings in new[] { "out", "out,binary", "out,exit_status", "out,nouse_stdio", "out,overlapped_io" })
            if (settings.Contains(PortNames.ExitStatus, StringComparison.Ordinal))
                Add("metadata-" + cases.Count, "try open_port({fd,0,1},[out,exit_status]) of _->error(accepted) catch error:badarg->ok end");
            else
                Add(
                    "metadata-" + cases.Count,
                    "P=open_port({fd,0,1},[" + settings + "]),{name,\"0/1\"}=erlang:port_info(P,name),{os_pid,undefined}=erlang:port_info(P,os_pid),{output,0}=erlang:port_info(P,output),true=erlang:port_command(P,<<>>),true=erlang:port_close(P),undefined=erlang:port_info(P)"
                );

        return cases;
    }
}
