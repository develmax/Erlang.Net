using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class BeamOnLoadResourceHashChecks
{
    internal static void Run()
    {
        Assembly assembly = typeof(BeamOnLoadResourceHashChecks).Assembly;
        string[] resources = assembly.GetManifestResourceNames();
        string manifestName = resources.Single(name => name.EndsWith(BeamOnLoadHashNames.ManifestSuffix, StringComparison.Ordinal));
        using Stream manifestStream = assembly.GetManifestResourceStream(manifestName)!;
        using JsonDocument manifest = JsonDocument.Parse(manifestStream);
        JsonElement files = manifest.RootElement.GetProperty(BeamOnLoadHashNames.Files);
        string[] originals = resources.Where(name => name.Contains(BeamOnLoadHashNames.ResourcePrefix, StringComparison.Ordinal)).ToArray();
        if (files.GetArrayLength() != BeamOnLoadHashNames.OriginalResourceCount || originals.Length != BeamOnLoadHashNames.OriginalResourceCount)
            throw new InvalidOperationException(BeamOnLoadHashNames.CountFailure);
        var checkedNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (JsonElement file in files.EnumerateArray())
        {
            string path = file.GetProperty(BeamOnLoadHashNames.Path).GetString()!;
            string expected = file.GetProperty(BeamOnLoadHashNames.Sha256).GetString()!;
            string[] parts = path.Split(BeamOnLoadHashNames.PathSeparator);
            string suffix = BeamOnLoadHashNames.ResourcePrefix + string.Join(
                BeamOnLoadHashNames.NamespaceSeparator,
                parts[..^1].Select(part => part.Replace(BeamOnLoadHashNames.FolderHyphen, BeamOnLoadHashNames.NamespaceUnderscore))
            ) + BeamOnLoadHashNames.NamespaceSeparator + parts[^1];
            string name = originals.Single(resource => resource.EndsWith(suffix, StringComparison.Ordinal));
            using Stream stream = assembly.GetManifestResourceStream(name)!;
            string actual = Convert.ToHexString(SHA256.HashData(stream));
            if (!checkedNames.Add(name) || !StringComparer.Ordinal.Equals(actual, expected))
                throw new InvalidOperationException(BeamOnLoadHashNames.HashFailure + path);
        }
        if (checkedNames.Count != originals.Length)
            throw new InvalidOperationException(BeamOnLoadHashNames.CountFailure);
    }
}
