using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text.Json;

if (args.Length != 3)
    return 2;
string output = Path.GetFullPath(args[2]);
if (Directory.Exists(output))
    return 2;
Directory.CreateDirectory(output);
var rows = new List<object>();
using var proof = JsonDocument.Parse("{}");
foreach (var sample in new[] { (Kind: "old", Dll: args[0]), (Kind: "new", Dll: args[1]) })
{
    var context = new AssemblyLoadContext(sample.Kind, isCollectible: true);
    try
    {
        var assembly = context.LoadFromAssemblyPath(Path.GetFullPath(sample.Dll));
        var exporter = assembly.GetType("ApplyOptimizerContrasts.ApplyExporter", true)!;
        var report = exporter.GetMethod("Report", BindingFlags.NonPublic | BindingFlags.Static)!;
        var observation = assembly.GetType("ApplyOptimizerContrasts.ApplyObservation", true)!;
        object observations = Activator.CreateInstance(typeof(List<>).MakeGenericType(observation))!;
        string directory = Path.Combine(output, sample.Kind);
        Directory.CreateDirectory(directory);
        byte[] payload = [0, 255, 195, 40];
        await File.WriteAllBytesAsync(Path.Combine(directory, "payload.bin"), payload);
        await (Task)report.Invoke(null, [directory, proof.RootElement, Array.Empty<byte>(), observations, false])!;
        byte[] first = await File.ReadAllBytesAsync(Path.Combine(directory, "apply-optimizer.json"));
        if (sample.Kind == "new")
            await File.WriteAllBytesAsync(Path.Combine(directory, "apply-optimizer.json.tmp"), [0, 18, 255]);
        await (Task)report.Invoke(null, [directory, proof.RootElement, Array.Empty<byte>(), observations, true])!;
        byte[] final = await File.ReadAllBytesAsync(Path.Combine(directory, "apply-optimizer.json"));
        using var document = JsonDocument.Parse(final);
        JsonElement[] files = document.RootElement.GetProperty("Files").EnumerateArray().ToArray();
        bool selfPresent = files.Any(file => file.GetProperty("Name").GetString() == "apply-optimizer.json");
        bool selfMismatch = selfPresent && files.Single(file => file.GetProperty("Name").GetString() == "apply-optimizer.json")
            .GetProperty("Sha256").GetString() != Convert.ToHexString(SHA256.HashData(final));
        bool payloadExact = files.Single(file => file.GetProperty("Name").GetString() == "payload.bin")
            .GetProperty("Sha256").GetString() == Convert.ToHexString(SHA256.HashData(payload));
        bool temporaryPresent = files.Any(file => file.GetProperty("Name").GetString() == "apply-optimizer.json.tmp");
        if (!payloadExact || temporaryPresent || (sample.Kind == "old" ? !selfMismatch : selfPresent))
            throw new InvalidDataException("Inventory control failed");
        if (sample.Kind == "new")
        {
            string caseDirectory = Path.Combine(directory, "case");
            Directory.CreateDirectory(caseDirectory);
            await File.WriteAllBytesAsync(Path.Combine(caseDirectory, "apply-optimizer.json"), payload);
            var inventory = exporter.GetMethod("Files", BindingFlags.NonPublic | BindingFlags.Static)!;
            var task = (Task)inventory.Invoke(null, [caseDirectory, null])!;
            await task;
            object result = task.GetType().GetProperty("Result")!.GetValue(task)!;
            string serialized = JsonSerializer.Serialize(result);
            using var caseFiles = JsonDocument.Parse(serialized);
            if (caseFiles.RootElement.GetArrayLength() != 1 ||
                caseFiles.RootElement[0].GetProperty("Name").GetString() != "apply-optimizer.json")
                throw new InvalidDataException("Case inventory was changed");
        }
        rows.Add(new
        {
            sample.Kind,
            DllSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(sample.Dll))),
            assembly.ManifestModule.ModuleVersionId,
            SelfPresent = selfPresent,
            SelfMismatch = selfMismatch,
            TemporaryPresent = temporaryPresent,
            PayloadExact = payloadExact,
            FirstReportSha256 = Convert.ToHexString(SHA256.HashData(first)),
            FinalReportSha256 = Convert.ToHexString(SHA256.HashData(final))
        });
    }
    finally
    {
        context.Unload();
    }
}
await File.WriteAllTextAsync(
    Path.Combine(output, "control.json"),
    JsonSerializer.Serialize(new { Status = "PASS", NativeExecuted = false, Rows = rows }, new JsonSerializerOptions { WriteIndented = true })
);
Console.WriteLine("Old self-inventory mismatch reproduced; new root-only exclusion and case inventory pass.");

return 0;
