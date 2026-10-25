using DistributionPacketContrasts;

namespace DistributionPayloadExitObservations;

internal static class PayloadExitModuleSource
{
    internal static string Runtime(string output, string root) =>
        "V={erlang:system_info(otp_release),erlang:system_info(version),erlang:system_info(emu_flavor),code:root_dir(),erlang:system_info(wordsize),erlang:system_info(system_architecture),erlang:system_info(build_type)},ok=file:write_file(" +
        PacketErlangString.Quote(Path.Combine(output, PacketNames.Runtime)) +
        ",term_to_binary(V)),{\"29\",\"17.1\",jit,R,_Wordsize,_Architecture,_BuildType}=V,true=(filename:absname(R)=:=filename:absname(" +
        PacketErlangString.Quote(root) + ")),halt(0).";

    internal static string Disassembly(string output) =>
        "D=beam_disasm:file(" + PacketErlangString.Quote(Path.Combine(output, PayloadExitNames.Module + PacketNames.BinarySuffix)) +
        "),ok=file:write_file(" + PacketErlangString.Quote(Path.Combine(output, PayloadExitNames.Module + PacketNames.Disassembly)) +
        ",iolist_to_binary(io_lib:format(\"~tp~n\",[D]))),halt(0).";

    internal static string Evaluate(string directory, string scenario) =>
        "try ok=" + PayloadExitNames.Module + ":run(" + scenario + "," + PacketErlangString.Quote(directory) +
        "),{ok,S}=file:read_file(" + PacketErlangString.Quote(Path.Combine(directory, PayloadExitNames.Stage)) +
        "),{complete," + scenario + "}=binary_to_term(S),{ok,O}=file:read_file(" +
        PacketErlangString.Quote(Path.Combine(directory, PayloadExitNames.Outer)) +
        "),{result,ok}=binary_to_term(O),halt(0) catch C:R:Stack -> ok=file:write_file(" +
        PacketErlangString.Quote(Path.Combine(directory, PacketNames.Exception)) +
        ",term_to_binary({C,R,Stack})),halt(1) end.";
}
