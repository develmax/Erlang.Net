namespace NodeIdentityContrasts;

internal static class NodeModuleSource
{
    internal static string Quote(string text) => NodeErlangString.Quote(text);

    internal static string Runtime(string output, string root) =>
        "V={erlang:system_info(otp_release),erlang:system_info(version),erlang:system_info(emu_flavor),code:root_dir()},ok=file:write_file(" +
        Quote(Path.Combine(output, NodeNames.Runtime)) +
        ",term_to_binary(V)),{\"29\",\"17.1\",jit,R}=V,true=(filename:absname(R)=:=filename:absname(" +
        Quote(root) + ")),halt(0).";

    internal static string Disassembly(string output, string module) =>
        "D=beam_disasm:file(" + Quote(Path.Combine(output, module + NodeNames.BinarySuffix)) +
        "),ok=file:write_file(" + Quote(Path.Combine(output, module + NodeNames.Disassembly)) +
        ",iolist_to_binary(io_lib:format(\"~tp~n\",[D]))),halt(0).";

    internal static string Evaluate(string scenario, string directory) =>
        NodeNames.Module + ":run(" + Quote(scenario) + "," + Quote(directory) + ").";
}
