internal static class FileCompatibilityFiles
{
    internal const string Provenance = "provenance.json";
    internal const string OriginalCorpus = "original.etf";
    internal const string RuntimeGatesCorpus = "runtime-gates.etf";
    internal const string Exports = "exports.etf";
    internal const string DeviceBeam = "file_port_device.beam";
    internal const string ObserverBeam = "file_port_cases.beam";
    internal const string FixturePayload = "filesystem/payload";
    internal const string FoundationDirectory = "src/Otp/Original/Foundation";
    internal const string BeamSearchPattern = "*.beam";
    internal const string StdlibCategory = "Stdlib";
    internal const string KernelCategory = "Kernel";
    internal const string ErtsCategory = "Erts";
    internal const string DeviceSource = "tools/Validation/Erlang.FileCompatibility/file_port_device.erl";
    internal const string ObserverSource = "tools/Validation/Erlang.FileCompatibility/file_port_cases.erl";
    internal const string OriginalFileSource = "src/Otp/Original/Foundation/Erlang.Otp.Kernel.Beam/Otp/src/file.erl";
    internal const string OriginalFileBeam = "src/Otp/Original/Foundation/Erlang.Otp.Kernel.Beam/Otp/ebin/file.beam";

    internal static readonly string[] RequiredInputs = [ObserverSource, DeviceSource, OriginalFileSource, OriginalFileBeam];
    internal static readonly string[] RequiredReports = [OriginalCorpus, RuntimeGatesCorpus, Exports, ObserverBeam, DeviceBeam];

    internal static string OriginalImage(string repository, string category, string module)
        => Path.Combine(
            repository,
            $"src/Otp/Original/Foundation/Erlang.Otp.{category}.Beam/Otp/{(category == StdlibCategory ? string.Empty : "ebin/")}{module}.beam"
        );

    internal static string Report(string backend, string corpus, int? row)
        => backend + (corpus == OriginalCorpus ? "-report" : "-runtime-gates")
            + (row is null ? string.Empty : "-" + row.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)) + ".json";
}
