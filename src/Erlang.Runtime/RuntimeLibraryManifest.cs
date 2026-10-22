using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Erlang;

public static class RuntimeLibraryManifest
{
    public const int CurrentVersion = 1;
    public const string FileName = "erlang-libraries.json";

    public static JsonSerializerOptions SerializationOptions => new()
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        PropertyNameCaseInsensitive = false,
        RespectRequiredConstructorParameters = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter<RuntimeModuleBackend>(allowIntegerValues: false) }
    };

    public static RuntimeLibrarySelection Load(string manifestPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestPath);
        string absolute = Path.GetFullPath(manifestPath);
        string directory = Path.GetDirectoryName(absolute)!;
        byte[] bytes = File.ReadAllBytes(absolute);
        using JsonDocument json = JsonDocument.Parse(bytes);
        RejectDuplicateProperties(json.RootElement);
        RuntimeLibraryManifestDocument document = json.Deserialize<RuntimeLibraryManifestDocument>(SerializationOptions)
            ?? throw new InvalidDataException(LibraryManifestNames.Invalid);
        if (document.Version != CurrentVersion || document.Modules is null)
            throw new InvalidDataException(LibraryManifestNames.Invalid);
        var choices = new List<RuntimeModuleSelection>();
        foreach (RuntimeLibraryManifestModule row in document.Modules)
        {
            if (row is null || row.Dependencies is null)
                throw new InvalidDataException(LibraryManifestNames.Invalid);
            if (row.Backend == RuntimeModuleBackend.Managed)
            {
                if (row.Image is not null || row.Sha256 is not null || row.Dependencies.Length != 0)
                    throw new InvalidDataException(LibraryManifestNames.Invalid);
                choices.Add(RuntimeModuleSelection.Managed(row.Module));
                continue;
            }
            if (row.Backend != RuntimeModuleBackend.Beam || string.IsNullOrWhiteSpace(row.Image)
                || row.Sha256 is null || row.Sha256.Length != SHA256.HashSizeInBytes * 2
                || !row.Sha256.All(char.IsAsciiHexDigit))
                throw new InvalidDataException(LibraryManifestNames.Invalid);
            if (Path.IsPathRooted(row.Image) || Path.GetExtension(row.Image) != LibraryManifestNames.BeamExtension)
                throw new InvalidDataException(LibraryManifestNames.RelativeImage);
            string imagePath = Path.GetFullPath(row.Image, directory);
            string relative = Path.GetRelativePath(directory, imagePath);
            if (relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(part => part == LibraryManifestNames.ParentDirectory))
                throw new InvalidDataException(LibraryManifestNames.RelativeImage);
            byte[] image = File.ReadAllBytes(imagePath);
            if (!Convert.ToHexString(SHA256.HashData(image)).Equals(row.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException(LibraryManifestNames.HashMismatch);
            choices.Add(RuntimeModuleSelection.Beam(row.Module, image, row.Dependencies));
        }

        return new RuntimeLibrarySelection(choices);
    }

    private static void RejectDuplicateProperties(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (!names.Add(property.Name))
                    throw new InvalidDataException(LibraryManifestNames.Invalid);
                RejectDuplicateProperties(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (JsonElement item in element.EnumerateArray())
                RejectDuplicateProperties(item);
    }
}
