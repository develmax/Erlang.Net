namespace Erlang.BeamBuild;

internal static class BeamApplicationNames
{
    internal const string Invalid = "Invalid or unsupported application resource request or input tree";
    internal const string Schema = "application-owned-input-v1";
    internal const string LogicalSeparator = "/";
    internal const char LogicalSeparatorCharacter = '/';
    internal const string VersionSeparator = "-";
    internal const string VersionPattern = @"\A[A-Za-z0-9][A-Za-z0-9._+-]{0,127}\z";
    internal const string LibraryDirectory = "lib";
    internal const string EbinDirectory = "ebin";
    internal const string PrivDirectory = "priv";
    internal const string ConfigurationDirectory = "config";
    internal const string SourceTree = "source";
    internal const string BeamTree = "beams";
    internal const string AppExtension = ".app";
    internal const string AppSourceExtension = ".app.src";
    internal const string ManifestFile = "application-resources.json";
    internal const string DescriptorKind = "Descriptor";
    internal const string ModuleKind = "Module";
    internal const string PrivKind = "Priv";
    internal const string ConfigurationKind = "Configuration";
    internal const int MaximumFiles = 256;
    internal const int MaximumRequestBytes = 1024 * 1024;
    internal const int MaximumFileBytes = 16 * 1024 * 1024;
    internal const long MaximumAggregateBytes = 128L * 1024 * 1024;
    internal const int ReadBufferBytes = 64 * 1024;
    internal static readonly HashSet<string> ReservedDeviceNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
        "COM¹", "COM²", "COM³", "LPT¹", "LPT²", "LPT³"
    };
}
