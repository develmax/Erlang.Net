using System.Security.Cryptography;
using System.Text.Json;
using Erlang.Compiler;
using Erlang.Differential;

namespace Erlang.NifNativeBackend;

internal static class NifNativeUnixRawPathDriver
{
    internal static async Task Run(string[] arguments)
    {
        NifNativeHarness.Require(OperatingSystem.IsLinux() && arguments.Length == 3);
        string admissionPath = Path.GetFullPath(arguments[0]);
        NifNativeHarness.Require(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(admissionPath))) == arguments[1]);
        using var admission = JsonDocument.Parse(File.ReadAllBytes(admissionPath));
        JsonElement plan = admission.RootElement;
        NifNativeHarness.Require(Text(plan, "Status") == NifNativeUnixRawPathNames.Admission);
        string source = Text(plan, "SourcePath");
        Verify(source, Text(plan, "SourceSha256"));
        Verify(Text(plan, "BuildReceiptPath"), Text(plan, "BuildReceiptSha256"));
        Verify(Text(plan, "NativeSourcePath"), Text(plan, "NativeSourceSha256"));
        Verify(Text(plan, "BridgePath"), Text(plan, "BridgeSha256"));
        var imagePath = NifNativeLibraryPath.FromUnixAbsolute(Convert.FromHexString(Text(plan, "ImageRawPathHex")));
        using (var imageFile = NifNativeUnixFile.OpenRead(imagePath))
            NifNativeHarness.Require(Convert.ToHexString(SHA256.HashData(imageFile)) == Text(plan, "ImageSha256"));
        byte[] stem = Convert.FromHexString(Text(plan, "RequestedStemHex"));
        string route = Text(plan, "Route");
        NifNativeHarness.Require(route is NifNativeUnixRawPathNames.Ordinary or NifNativeUnixRawPathNames.Reviewed or NifNativeUnixRawPathNames.Collision);
        var capability = new NifNativeReviewedBasicAbi(
            imagePath.DisplayPath,
            Text(plan, "ImageSha256"),
            Text(plan, "BuildReceiptSha256"),
            Text(plan, "BridgePath"),
            Text(plan, "BridgeSha256"),
            imagePath.UnixNullTerminated()[..^1]
        );
        string output = Path.GetFullPath(arguments[2]);
        NifNativeHarness.Require(!Directory.Exists(output));
        Directory.CreateDirectory(output);
        using var compiled = GeneratedModuleCompiler.Compile(File.ReadAllText(source));
        await using var runtime = new ProcessRuntime();
        compiled.Definition.Register(runtime.Modules);
        if (route != NifNativeUnixRawPathNames.Ordinary)
            runtime.Modules.InstallReviewedNativeExecutionPolicy(_ => capability);
        ModuleCodeImage image = runtime.Modules.CurrentCodeImage(NifNativeUnixRawPathNames.Module)
            ?? throw new InvalidOperationException(NifNativeHarnessNames.Broken);
        Term? returned = null;
        bool refused = false;
        try
        {
            await NifNativeHarness.Context(
                runtime,
                async context =>
            {
                try
                {
                    returned = await Execution.InvokeAsync(
                        compiled.Definition,
                        NifNativeUnixRawPathNames.Run,
                        context,
                        [new BitString(stem)]
                    );
                }
                catch (UnauthorizedAccessException) when (route == NifNativeUnixRawPathNames.Collision)
                {
                    refused = true;
                }
            }
            );
        }
        finally
        {
            await image.NifBindings.RetireAsync().WaitAsync(NifNativeHarnessNames.Deadline);
        }
        if (route == NifNativeUnixRawPathNames.Collision)
            NifNativeHarness.Require(refused && returned is null);
        else
        {
            NifNativeHarness.Require(returned is not null);
            File.WriteAllBytes(Path.Combine(output, NifNativeUnixRawPathNames.ResultFile), ExternalTermFormat.Encode(returned!));
        }
        File.WriteAllText(
            Path.Combine(output, NifNativeUnixRawPathNames.CompletionFile),
            JsonSerializer.Serialize(new
            {
                Status = refused ? NifNativeUnixRawPathNames.CollisionRefused : NifNativeUnixRawPathNames.Complete,
                Route = route,
                AdmissionSha256 = arguments[1],
                NativeImageRetired = true,
                OriginalComparisonExecuted = false
            })
        );
    }

    private static string Text(JsonElement plan, string key) => plan.GetProperty(key).GetString()
        ?? throw new InvalidOperationException(NifNativeHarnessNames.Broken);

    private static void Verify(string path, string hash)
    {
        NifNativeHarness.Require(Path.IsPathFullyQualified(path));
        NifNativeHarness.Require(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))) == hash);
    }
}
