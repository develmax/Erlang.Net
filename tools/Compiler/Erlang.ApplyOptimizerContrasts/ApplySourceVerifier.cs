using System.Security.Cryptography;
using System.Text.Json;
namespace ApplyOptimizerContrasts;

internal static class ApplySourceVerifier
{
    internal static async Task<string[]> Verify(JsonElement declarations)
    {
        string[] names = declarations.GetProperty(ApplyObservationProtocol.NamesField).EnumerateArray().Select(row => row.GetString()!).ToArray();
        if (!names.SequenceEqual(ApplyScenarios.All.Select(row => row.Name)) || names.Distinct(StringComparer.Ordinal).Count() != ApplyNames.ScenarioCount)
            throw new InvalidDataException(ApplyDiagnostics.Declarations);
        JsonElement[] bindings = declarations.GetProperty(ApplyObservationProtocol.ScenarioBindings).EnumerateArray().ToArray();
        if (bindings.Length != ApplyNames.ScenarioCount)
            throw new InvalidDataException(ApplyDiagnostics.Declarations);
        for (int index = 0; index < bindings.Length; index++)
        {
            ApplyScenarioBinding expected = ApplyScenarios.All[index];
            JsonElement actual = bindings[index];
            if (actual.GetProperty(ApplyObservationProtocol.NameField).GetString() != expected.Name || actual.GetProperty(ApplyObservationProtocol.ModuleField).GetString() != expected.Module || actual.GetProperty(ApplyObservationProtocol.NativeFunctionField).GetString() != expected.NativeFunctionName)
                throw new InvalidDataException(ApplyDiagnostics.Declarations);
        }
        JsonElement[] sources = declarations.GetProperty(ApplyObservationProtocol.SourcesField).EnumerateArray().ToArray();
        string[] expectedSources = [ApplyNames.MainSource, ApplyNames.LocalSource];
        if (sources.Length != expectedSources.Length)
            throw new InvalidDataException(ApplyDiagnostics.Source);
        for (int index = 0; index < sources.Length; index++)
        {
            string? name = sources[index].GetProperty(ApplyObservationProtocol.PathField).GetString();
            if (name != expectedSources[index])
                throw new InvalidDataException(ApplyDiagnostics.Source);
            byte[] bytes = await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, name));
            if (Convert.ToHexString(SHA256.HashData(bytes)) != sources[index].GetProperty(ApplyObservationProtocol.HashField).GetString())
                throw new InvalidDataException(ApplyDiagnostics.Source);
        }

        return names;
    }
}
