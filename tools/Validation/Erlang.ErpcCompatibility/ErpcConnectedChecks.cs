using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text.Json;

internal static class ErpcConnectedChecks
{
    internal static async Task<int> Run(string repository, string directory)
    {
        string assemblyPath = Path.Combine(repository, "tests/Erlang.Tests/bin/Debug/net10.0/Erlang.Tests.dll");
        AssemblyLoadContext.Default.Resolving += (context, name) =>
        {
            string candidate = Path.Combine(Path.GetDirectoryName(assemblyPath)!, name.Name + ".dll");

            return File.Exists(candidate) ? context.LoadFromAssemblyPath(candidate) : null;
        };
        Assembly tests = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
        (string Type, string Method)[] cases =
        [
            ("ConnectedRpcLocalHostChecks", "LocalCallerAndExceptions"),
            ("ConnectedRpcTimeoutHostChecks", "QueuedDownBeforeZeroWait"),
            ("ConnectedRpcTimeoutHostChecks", "TimeoutLosesToAdmittedDown"),
            ("ConnectedRpcLabelHostChecks", "VisibleLabelDeathAndNodeRetirement"),
            ("ConnectedSpawnCleanupHostChecks", "AbandonBeforeReply"),
            ("ConnectedSpawnHostChecks", "RealRequestReplyAndDeath"),
            ("ConnectedSpawnMonitorOptionHostChecks", "DemonitorAfterSuccess"),
            ("ConnectedSpawnRetirementHostChecks", "StaleReplyCannotRebind")
        ];
        var failures = new List<object>();
        foreach (var row in cases)
        {
            Console.WriteLine(row.Type + "." + row.Method);
            try
            {
                MethodInfo method = tests.GetType("Erlang.Tests." + row.Type, true)!.GetMethod(row.Method)!;
                await ((Task)method.Invoke(null, null)!).WaitAsync(TimeSpan.FromSeconds(45));
            }
            catch (Exception error)
            {
                failures.Add(new { row.Type, row.Method, Error = error.ToString() });
            }
        }
        File.WriteAllText(
            Path.Combine(directory, "connected.json"),
            JsonSerializer.Serialize(
                new
                {
                    Cases = cases.Length,
                    Passed = cases.Length - failures.Count,
                    Failures = failures,
                    FrozenTestAssembly = assemblyPath,
                    FrozenTestAssemblySha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assemblyPath)))
                },
                new JsonSerializerOptions { WriteIndented = true }
            )
        );
        Console.WriteLine($"Connected: {cases.Length - failures.Count}/{cases.Length}");

        return failures.Count == 0 ? 0 : 1;
    }
}
