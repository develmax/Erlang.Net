using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Erlang;
using static InitProgressNames617;

internal sealed class InitProgressAdmission617 : IDisposable
{
    private readonly JsonDocument document;
    private readonly string path, expectedHash;
    internal JsonElement Plan => document.RootElement;
    internal string RouteValue => Plan.GetProperty(Route).GetString()!;
    internal string Head => Plan.GetProperty(SubjectHead).GetString()!;
    internal JsonElement[] Native => Plan.GetProperty(Images).EnumerateArray().ToArray();

    internal InitProgressAdmission617(string admission, string sha)
    {
        path = System.IO.Path.GetFullPath(admission);
        expectedHash = sha;
        Require(Hash(path) == expectedHash);
        document = JsonDocument.Parse(System.IO.File.ReadAllBytes(path));
        try
        {
            Validate();
        }
        catch
        {
            document.Dispose();
            throw;
        }
    }

    private void Validate()
    {
        Require(Plan.GetProperty(Status).GetString() == Admission && Plan.GetProperty(PinField).GetString() == Pin);
        Require(Head.Length == 40 && Head.All(char.IsAsciiHexDigit) && Head == Head.ToLowerInvariant());
        Require(RouteValue is Ordinary or Reviewed);
        Verify();
        using var original = JsonDocument.Parse(System.IO.File.ReadAllBytes(File(OriginalCompletion)));
        JsonElement source = original.RootElement;
        Require(source.GetProperty(Passed).GetBoolean() && source.GetProperty(OriginalOnly).GetBoolean()
            && !source.GetProperty(ManagedExecuted).GetBoolean() && source.GetProperty(WholeNine).GetBoolean()
            && source.GetProperty(ConstructorMilliseconds).GetInt32() == ConstructorBound
            && source.GetProperty(VmSeconds).GetInt32() == (int)InvocationDeadline.TotalSeconds
            && source.GetProperty(Checks).EnumerateObject().All(p => p.Value.GetBoolean()));
        Require(Native.Length == NativeCount && Native.Select(p => p.GetProperty(Id).GetString()).ToHashSet().SetEquals([AckId, FirstId, SecondId]));
        foreach (JsonElement image in Native)
        {
            string library = image.GetProperty(JsonPath).GetString()!;
            Require(Hash(library) == image.GetProperty(Sha).GetString() && library.EndsWith(LinuxSuffix, StringComparison.Ordinal));
            string receipt = image.GetProperty(BuildReceipt).GetString()!;
            Require(receipt == File(OriginalCompletion) && Hash(receipt) == image.GetProperty(BuildReceiptSha).GetString());
            JsonElement genuine = source.GetProperty(NativeImages).EnumerateArray().Single(p => p.GetProperty(Id).GetString() == image.GetProperty(Id).GetString());
            Require(genuine.GetProperty(Library).GetProperty(Sha).GetString() == Hash(library));
        }
        Require(source.GetProperty(WholeRawResult).GetProperty(Sha).GetString() == Hash(File(OriginalObserved))
            && source.GetProperty(WholeCleanup).GetProperty(Sha).GetString() == Hash(File(OriginalCleanup))
            && source.GetProperty(SchedulerProfile).GetProperty(Sha).GetString() == Hash(File(OriginalProfile)));
        foreach (var item in new[] { (AckBeam, AckBeamFile), (Beam1, FirstBeamFile), (Beam2, SecondBeamFile), (ObserverBeam, ObserverBeamFile) })
            Require(source.GetProperty(Beams).EnumerateArray().Single(p => System.IO.Path.GetFileName(p.GetProperty(JsonPath).GetString()) == item.Item2)
                .GetProperty(Sha).GetString() == Hash(File(item.Item1)));
        using var build = JsonDocument.Parse(System.IO.File.ReadAllBytes(File(OriginalBuild)));
        Require(build.RootElement.GetProperty(Status).GetString() == OriginalBuildStatus && build.RootElement.GetProperty(Commit).GetString() == Pin);
        using var bridge = JsonDocument.Parse(System.IO.File.ReadAllBytes(File(BridgeReceipt)));
        Require(bridge.RootElement.GetProperty(Status).GetString() == BridgeBuildStatus
            && bridge.RootElement.GetProperty(PinField).GetString() == Pin
            && bridge.RootElement.GetProperty(SubjectHead).GetString() == Head
            && bridge.RootElement.GetProperty(BridgeHash).GetString() == Hash(File(Bridge)));

    }

    internal string File(string key)
    {
        JsonElement item = Plan.GetProperty(Artifacts).GetProperty(key);
        string file = item.GetProperty(JsonPath).GetString()!;
        Require(System.IO.Path.IsPathFullyQualified(file) && Hash(file) == item.GetProperty(Sha).GetString()
            && Plan.GetProperty(Inputs).EnumerateArray().Any(row => row.GetProperty(JsonPath).GetString() == file
                && row.GetProperty(Sha).GetString() == item.GetProperty(Sha).GetString()));

        return file;
    }

    internal string NativePath(string id) => Native.Single(p => p.GetProperty(Id).GetString() == id).GetProperty(JsonPath).GetString()!;

    internal void Verify()
    {
        Require(Hash(path) == expectedHash);
        JsonElement[] inputs = Plan.GetProperty(Inputs).EnumerateArray().ToArray();
        Require(inputs.Length != 0 && inputs.Select(item => item.GetProperty(JsonPath).GetString()).Distinct(StringComparer.Ordinal).Count() == inputs.Length);
        foreach (JsonElement item in inputs)
            Require(Hash(item.GetProperty(JsonPath).GetString()!) == item.GetProperty(Sha).GetString());
        VerifyRuntime();
    }

    private void VerifyRuntime()
    {
        using var runtime = JsonDocument.Parse(System.IO.File.ReadAllBytes(File(RuntimeManifest)));
        JsonElement manifest = runtime.RootElement;
        Require(manifest.GetProperty(Status).GetString() == RuntimeStatus
            && manifest.GetProperty(HeadField).GetString() == Head && manifest.GetProperty(BuildExit).GetInt32() == 0);
        foreach (string key in new[] { BuildEvidence, SourceFreeze })
        {
            JsonElement proof = manifest.GetProperty(key);
            Require(Hash(proof.GetProperty(JsonPath).GetString()!) == proof.GetProperty(Sha).GetString());
        }
        JsonElement frozen = manifest.GetProperty(SourceFreeze);
        using var source = JsonDocument.Parse(System.IO.File.ReadAllBytes(frozen.GetProperty(JsonPath).GetString()!));
        Require(source.RootElement.GetProperty(SubjectHead).GetString() == Head
            && source.RootElement.GetProperty(Rows).GetArrayLength() != 0);
        string repository = System.IO.Path.GetFullPath(Plan.GetProperty(Repository).GetString()!);
        foreach (JsonElement row in source.RootElement.GetProperty(Rows).EnumerateArray())
        {
            string file = System.IO.Path.GetFullPath(System.IO.Path.Combine(repository, row.GetProperty(JsonPath).GetString()!));
            string relative = System.IO.Path.GetRelativePath(repository, file);
            Require(!System.IO.Path.IsPathRooted(relative) && relative.Split(System.IO.Path.DirectorySeparatorChar).All(part => part != ParentSegment)
                && Hash(file) == row.GetProperty(Sha).GetString());
        }
        JsonElement[] files = manifest.GetProperty(Files).EnumerateArray().ToArray();
        Require(files.Length != 0);
        foreach (JsonElement row in files) Require(Hash(row.GetProperty(JsonPath).GetString()!) == row.GetProperty(Sha).GetString());
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name!.StartsWith(ErlangPrefix, StringComparison.Ordinal)))
        {
            string location = assembly.Location;
            Require(location.Length != 0 && files.Any(row => System.IO.Path.GetFileName(row.GetProperty(JsonPath).GetString()) == System.IO.Path.GetFileName(location)
                && row.GetProperty(Sha).GetString() == Hash(location)));
        }
        Require(files.Any(row => System.IO.Path.GetFileName(row.GetProperty(JsonPath).GetString()) == RuntimeDll
            && row.GetProperty(Sha).GetString() == Hash(typeof(ProcessRuntime).Assembly.Location)));
        JsonElement host = manifest.GetProperty(RuntimeHost);
        Require(Environment.ProcessPath is { } actualHost && Hash(actualHost) == host.GetProperty(Sha).GetString()
            && Environment.Version.ToString() == host.GetProperty(FrameworkVersion).GetString());
    }

    internal static string Hash(string file) => Convert.ToHexString(SHA256.HashData(System.IO.File.ReadAllBytes(file)));

    internal static void Require(bool value)
    {
        if (!value) throw new InvalidDataException(Failure);
    }

    public void Dispose() => document.Dispose();
}
