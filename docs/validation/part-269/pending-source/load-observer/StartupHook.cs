using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

internal static class StartupHook
{
    private static readonly object gate = new();

    public static void Initialize()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            Record(assembly);
        AppDomain.CurrentDomain.AssemblyLoad += (_, arguments) => Record(arguments.LoadedAssembly);
    }

    private static void Record(Assembly assembly)
    {
        string? name = assembly.GetName().Name;
        if (name is not (LoadObserverNames.Tests or LoadObserverNames.Runtime or LoadObserverNames.Runner))
            return;
        string? output = Environment.GetEnvironmentVariable(LoadObserverNames.OutputEnvironment);
        if (output is null)
            return;
        string path = assembly.Location;
        using var stream = File.OpenRead(path);
        string sha = Convert.ToHexString(SHA256.HashData(stream));
        string row = JsonSerializer.Serialize(new
        {
            Name = name,
            ProcessId = Environment.ProcessId,
            FullName = assembly.FullName,
            Location = path,
            Mvid = assembly.ManifestModule.ModuleVersionId,
            Sha256 = sha
        });
        lock (gate)
            File.AppendAllText(output, row + Environment.NewLine);
    }
}
