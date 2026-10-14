namespace DistributionPacketContrasts;

internal static class PacketModuleSource
{
    internal static string Runtime(string output, string root) =>
        "V={erlang:system_info(otp_release),erlang:system_info(version),erlang:system_info(emu_flavor),code:root_dir()},ok=file:write_file(" +
        PacketErlangString.Quote(Path.Combine(output, PacketNames.Runtime)) +
        ",term_to_binary(V)),{\"29\",\"17.1\",jit,R}=V,true=(filename:absname(R)=:=filename:absname(" +
        PacketErlangString.Quote(root) + ")),halt(0).";

    internal static string Disassembly(string output) =>
        "D=beam_disasm:file(" + PacketErlangString.Quote(Path.Combine(output, PacketNames.Module + PacketNames.BinarySuffix)) +
        "),ok=file:write_file(" + PacketErlangString.Quote(Path.Combine(output, PacketNames.Module + PacketNames.Disassembly)) +
        ",iolist_to_binary(io_lib:format(\"~tp~n\",[D]))),halt(0).";

    internal static string Evaluate(string directory) =>
        "try ok=" + PacketNames.Module + ":run(" + PacketErlangString.Quote(directory) +
        "),halt(0) catch C:R:S -> ok=file:write_file(" +
        PacketErlangString.Quote(Path.Combine(directory, PacketNames.Exception)) +
        ",term_to_binary({C,R,S})),halt(1) end.";
}
