namespace ApplyOptimizerContrasts;

internal static class ApplyModuleSource
{
    internal static string Runtime(string output, string root) => "V={erlang:system_info(otp_release),erlang:system_info(version),erlang:system_info(emu_flavor),code:root_dir()},ok=file:write_file(" + ApplyExporter.Quote(Path.Combine(output, ApplyArtifactNames.RuntimeEtf)) + ",term_to_binary(V)),{\"29\",\"17.1\",jit," + ApplyExporter.Quote(root) + "}=V,halt(0).";

    internal static string Disassembly(string output, string module) => "D=beam_disasm:file(" + ApplyExporter.Quote(Path.Combine(output, module + ApplyArtifactNames.BinarySuffix)) + "),ok=file:write_file(" + ApplyExporter.Quote(Path.Combine(output, module + ApplyArtifactNames.DisassemblySuffix)) + ",iolist_to_binary(io_lib:format(\"~tp~n\",[D]))),halt(0).";

    internal static string Evaluation(string name, string capturedInput, string directory) => ApplyNames.MainModule + ":run(" + ApplyExporter.Quote(name) + "," + ApplyExporter.Quote(capturedInput) + "," + ApplyExporter.Quote(directory) + ").";
}
