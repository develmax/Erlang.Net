using DistributionPacketContrasts;

namespace DistributionMonitorPayloadObservations;

internal static class MonitorPayloadModuleSource
{
    internal static string Runtime(string output, string root) =>
        "V={erlang:system_info(otp_release),erlang:system_info(version),erlang:system_info(emu_flavor),code:root_dir(),erlang:system_info(wordsize),erlang:system_info(system_architecture),erlang:system_info(build_type)},ok=file:write_file(" +
        PacketErlangString.Quote(Path.Combine(output, PacketNames.Runtime)) +
        ",term_to_binary(V)),{\"29\",\"17.1\",jit,R,_Wordsize,_Architecture,_BuildType}=V,true=(filename:absname(R)=:=filename:absname(" +
        PacketErlangString.Quote(root) + ")),halt(0).";

    internal static string Disassembly(string output) =>
        "D=beam_disasm:file(" + PacketErlangString.Quote(Path.Combine(output, MonitorPayloadNames.Module + PacketNames.BinarySuffix)) +
        "),ok=file:write_file(" + PacketErlangString.Quote(Path.Combine(output, MonitorPayloadNames.Module + PacketNames.Disassembly)) +
        ",iolist_to_binary(io_lib:format(\"~tp~n\",[D]))),halt(0).";

    internal static string Evaluate(string directory, string scenario) =>
        "try ok=" + MonitorPayloadNames.Module + ":run(" + PacketErlangString.Quote(directory) + "," + scenario +
        "),{ok,<<\"complete\">>}=file:read_file(" + PacketErlangString.Quote(Path.Combine(directory, PacketNames.Stage)) +
        "),halt(0) catch C:R:S -> ok=file:write_file(" +
        PacketErlangString.Quote(Path.Combine(directory, PacketNames.Exception)) +
        ",term_to_binary({C,R,S})),halt(1) end.";
}
